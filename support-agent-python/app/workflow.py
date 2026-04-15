from __future__ import annotations

from typing import Any

from agent_framework import Agent, Executor, WorkflowContext, handler
from pydantic import ValidationError
from typing_extensions import Never

from app.models import (
    ActionTaken,
    ArtifactType,
    CustomerMessageDraft,
    DraftedResponseContext,
    EscalationContext,
    IntakeAssessment,
    IntakeContext,
    MessageMode,
    OperationalActionContext,
    ParsedSupportRequest,
    PolicyContext,
    PolicyRoute,
    Sentiment,
    SimulatedArtifact,
    SupportRequestResult,
    WorkflowTraceStep,
    WorkflowTraceStepKind,
)
from app.policy import (
    SupportPolicyRules,
    build_escalation_next_steps,
    build_escalation_queue,
    build_escalation_sla,
    build_escalation_summary,
)
from app.storage import persist_if_ticket_artifact


KEY_DRAFT = "support_request.draft"


class NormalizeRequestExecutor(Executor):
    def __init__(self, policy_rules: SupportPolicyRules) -> None:
        self._policy_rules = policy_rules
        super().__init__(id="normalize_request")

    @handler
    async def normalize(self, message: str, ctx: WorkflowContext[ParsedSupportRequest]) -> None:
        await _emit_trace(self.id, ctx, "Normalize", "Parsing pasted customer message.", WorkflowTraceStepKind.Detail)
        parsed = self._policy_rules.parse_request(message)
        await ctx.send_message(parsed)


class IntakeClassifierExecutor(Executor):
    def __init__(self, agent: Agent) -> None:
        self._agent = agent
        self._session = None
        super().__init__(id="intake_classifier")

    @handler
    async def classify(self, message: ParsedSupportRequest, ctx: WorkflowContext[IntakeContext]) -> None:
        await _emit_trace(self.id, ctx, "Intake", "Preparing classifier agent.", WorkflowTraceStepKind.Detail)
        self._session = self._session or self._agent.create_session()

        prompt = f"""
Classify this customer support request.

Subject: {message.subject or "(none)"}
From: {message.sender or "(unknown)"}
Detected signals: {_format_list(message.detected_signals)}

Request:
{message.body}
""".strip()

        response = await self._agent.run(
            prompt,
            session=self._session,
            options={"response_format": IntakeAssessment},
        )
        await _emit_trace(self.id, ctx, "Intake", "Classifier agent returned a response.", WorkflowTraceStepKind.Detail)
        intake = _coerce_response_value(response, IntakeAssessment)

        intake_context = IntakeContext(request=message, intake=intake)
        await ctx.add_event(_data_event(self.id, intake_context))
        await ctx.send_message(intake_context)


class PolicyGateExecutor(Executor):
    def __init__(self) -> None:
        super().__init__(id="policy_gate")

    @handler
    async def apply_policy(self, message: IntakeContext, ctx: WorkflowContext[PolicyContext]) -> None:
        decision = SupportPolicyRules.evaluate_policy(message.request, message.intake)
        policy_context = PolicyContext(
            request=message.request,
            intake=message.intake,
            policy=decision,
        )

        await _emit_trace(
            self.id,
            ctx,
            "Policy",
            f"Route={decision.route.value}; Action={decision.action_taken.value}; Next={decision.recommended_next_action or '(none)'}",
        )
        await ctx.add_event(_data_event(self.id, policy_context))
        await ctx.send_message(policy_context)


class DraftCustomerMessageExecutor(Executor):
    def __init__(self, agent: Agent, policy_rules: SupportPolicyRules) -> None:
        self._agent = agent
        self._policy_rules = policy_rules
        self._session = None
        super().__init__(id="draft_customer_message")

    @handler
    async def handle_policy(self, message: PolicyContext, ctx: WorkflowContext[DraftedResponseContext]) -> None:
        await _emit_trace(
            self.id,
            ctx,
            "Draft",
            f"Drafting {message.policy.message_mode.value} response.",
            WorkflowTraceStepKind.Detail,
        )

        prompt = f"""
Draft a customer support message.

Mode: {message.policy.message_mode.value}
Intent: {message.intake.primary_intent.value}
Sentiment: {message.intake.sentiment.value}
Urgency: {message.intake.urgency.value}
Tone guidance: {_build_tone_guidance(message.intake.sentiment)}
Summary: {message.intake.summary}
Missing information: {_format_list(message.intake.missing_information)}
Applied handbook facts: {_format_list(message.policy.applied_policies)}
Recommended next action: {message.policy.recommended_next_action or "(none)"}

Relevant handbook context:
{self._policy_rules.build_handbook_excerpt(message.policy)}

Customer request:
{message.request.body}

Draft requirements:
- Acknowledge the customer briefly before the answer.
- Follow the tone guidance exactly and match the empathy to the customer's sentiment.
- If Mode is ClarificationEmail, ask only for the missing details needed to continue.
- If Mode is Reply, answer directly and clearly.
- Keep the message concise.
""".strip()

        draft = await self._run_draft(prompt, message.policy.message_mode)
        await self._send_drafted(ctx, message, draft, None)

    @handler
    async def handle_operational(
        self,
        message: OperationalActionContext,
        ctx: WorkflowContext[DraftedResponseContext],
    ) -> None:
        await _emit_trace(
            self.id,
            ctx,
            "Draft",
            f"Drafting {message.policy_context.policy.message_mode.value} response using artifact {message.artifact.artifact_type.value}.",
            WorkflowTraceStepKind.Detail,
        )

        policy_context = message.policy_context
        prompt = f"""
Draft an acknowledgement for a support operation.

Mode: {policy_context.policy.message_mode.value}
Intent: {policy_context.intake.primary_intent.value}
Sentiment: {policy_context.intake.sentiment.value}
Tone guidance: {_build_tone_guidance(policy_context.intake.sentiment)}
Summary: {policy_context.intake.summary}
Applied handbook facts: {_format_list(policy_context.policy.applied_policies)}
Recommended next action: {policy_context.policy.recommended_next_action or "(none)"}

Simulated artifact preview:
{message.artifact.payload}

Customer request:
{policy_context.request.body}

Draft requirements:
- Acknowledge the customer's request.
- Follow the tone guidance exactly and show appropriate empathy without becoming long or dramatic.
- Confirm the next step without promising unsupported exceptions.
- Keep it short and human.
""".strip()

        draft = await self._run_draft(prompt, MessageMode.OperationalAcknowledgement)
        await self._send_drafted(ctx, policy_context, draft, message.artifact)

    @handler
    async def handle_escalation(
        self,
        message: EscalationContext,
        ctx: WorkflowContext[DraftedResponseContext],
    ) -> None:
        await _emit_trace(self.id, ctx, "Draft", "Drafting escalation acknowledgement response.", WorkflowTraceStepKind.Detail)
        policy_context = message.policy_context
        prompt = f"""
Draft an escalation acknowledgement for a customer.

Mode: EscalationAcknowledgement
Intent: {policy_context.intake.primary_intent.value}
Sentiment: {policy_context.intake.sentiment.value}
Tone guidance: {_build_tone_guidance(policy_context.intake.sentiment)}
Summary: {policy_context.intake.summary}
Queue: {message.queue}
SLA: {message.sla}
Next steps: {_format_list(message.next_steps)}
Applied handbook facts: {_format_list(policy_context.policy.applied_policies)}

Escalation artifact:
{message.artifact.payload}

Customer request:
{policy_context.request.body}

Draft requirements:
- Acknowledge the concern.
- Follow the tone guidance exactly and sound calm, respectful, and human.
- Confirm that the case is being escalated.
- Do not argue or over-explain.
- Mention the expected response timing in plain language.
""".strip()

        draft = await self._run_draft(prompt, MessageMode.EscalationAcknowledgement)
        await self._send_drafted(ctx, policy_context, draft, message.artifact)

    async def _run_draft(self, prompt: str, mode: MessageMode) -> CustomerMessageDraft:
        self._session = self._session or self._agent.create_session()
        response = await self._agent.run(
            prompt,
            session=self._session,
            options={"response_format": CustomerMessageDraft},
        )
        draft = _coerce_response_value(response, CustomerMessageDraft)
        draft.mode = mode
        draft.body = draft.body.strip()
        return draft

    async def _send_drafted(
        self,
        ctx: WorkflowContext[DraftedResponseContext],
        policy_context: PolicyContext,
        draft: CustomerMessageDraft,
        artifact: SimulatedArtifact | None,
    ) -> None:
        await _emit_trace(
            self.id,
            ctx,
            "Draft",
            f"Mode={draft.mode.value}; Subject={draft.subject_line or '(none)'}",
        )
        ctx.set_state(KEY_DRAFT, draft)
        await ctx.add_event(_data_event(self.id, draft))
        await ctx.send_message(DraftedResponseContext(policy_context=policy_context, draft=draft, artifact=artifact))


class OperationalActionExecutor(Executor):
    def __init__(self) -> None:
        super().__init__(id="operational_action")

    @handler
    async def handle_policy(self, message: PolicyContext, ctx: WorkflowContext[OperationalActionContext]) -> None:
        await _emit_trace(
            self.id,
            ctx,
            "Artifact",
            f"Preparing {message.policy.action_taken.value} artifact.",
            WorkflowTraceStepKind.Detail,
        )

        if message.policy.action_taken == ActionTaken.RefundTicketCreated:
            artifact = _build_refund_artifact(message)
        elif message.policy.action_taken == ActionTaken.CancellationTicketCreated:
            artifact = _build_cancellation_artifact(message)
        else:
            raise RuntimeError(f"Unsupported action for operational handling: {message.policy.action_taken.value}.")

        persist_if_ticket_artifact(artifact)
        if "ticket_id" in artifact.metadata:
            await _emit_trace(
                self.id,
                ctx,
                "Storage",
                f"Persisted ticket artifact {artifact.metadata['ticket_id']}.",
                WorkflowTraceStepKind.Detail,
            )

        await _emit_artifact(self.id, ctx, artifact)
        await ctx.send_message(
            OperationalActionContext(
                policy_context=message,
                artifact=artifact,
                operator_note=message.policy.recommended_next_action,
            )
        )

    @handler
    async def handle_draft(self, message: DraftedResponseContext, ctx: WorkflowContext[OperationalActionContext]) -> None:
        await _emit_trace(
            self.id,
            ctx,
            "Artifact",
            "Preparing deterministic clarification email artifact.",
            WorkflowTraceStepKind.Detail,
        )

        artifact = SimulatedArtifact(
            artifact_type=ArtifactType.ClarificationEmail,
            display_title="Clarification Email Preview",
            payload=f"Subject: {message.draft.subject_line or 'More details needed for your support request'}\n\n{message.draft.body}",
            metadata={
                "route": "clarification",
                "recipient": message.policy_context.request.sender or "unknown",
            },
        )

        await _emit_artifact(self.id, ctx, artifact)
        await ctx.send_message(
            OperationalActionContext(
                policy_context=message.policy_context,
                artifact=artifact,
                operator_note=message.policy_context.policy.recommended_next_action,
            )
        )


class PrepareEscalationArtifactExecutor(Executor):
    def __init__(self) -> None:
        super().__init__(id="prepare_escalation")

    @handler
    async def prepare(self, message: PolicyContext, ctx: WorkflowContext[EscalationContext]) -> None:
        await _emit_trace(
            self.id,
            ctx,
            "Artifact",
            "Preparing escalation handoff artifact.",
            WorkflowTraceStepKind.Detail,
        )

        queue = build_escalation_queue(message.request, message.intake)
        sla = build_escalation_sla(message.intake.urgency)
        next_steps = build_escalation_next_steps(message.request, message.intake)
        next_steps_text = "\n- ".join(next_steps)
        summary = build_escalation_summary(message)

        artifact = SimulatedArtifact(
            artifact_type=ArtifactType.EscalationHandoff,
            display_title="Escalation Handoff Preview",
            payload=f"""Tag: ESCALATE
Queue: {queue}
SLA: {sla}
Summary: {summary}

Next steps:
- {next_steps_text}
""",
            metadata={"queue": queue, "sla": sla},
        )

        persist_if_ticket_artifact(artifact)
        if "ticket_id" in artifact.metadata:
            await _emit_trace(
                self.id,
                ctx,
                "Storage",
                f"Persisted ticket artifact {artifact.metadata['ticket_id']}.",
                WorkflowTraceStepKind.Detail,
            )

        escalation_context = EscalationContext(
            policy_context=message,
            artifact=artifact,
            queue=queue,
            sla=sla,
            next_steps=next_steps,
        )
        await _emit_artifact(self.id, ctx, artifact)
        await ctx.add_event(_data_event(self.id, escalation_context))
        await ctx.send_message(escalation_context)


class AssembleSupportResultExecutor(Executor):
    def __init__(self) -> None:
        super().__init__(id="assemble_result")

    @handler
    async def handle_drafted(
        self,
        message: DraftedResponseContext,
        ctx: WorkflowContext[Never, SupportRequestResult],
    ) -> None:
        await _emit_trace(self.id, ctx, "Assemble", "Building final support result.", WorkflowTraceStepKind.Detail)
        result = _build_result(message.policy_context, message.draft.body)
        await ctx.yield_output(result)

    @handler
    async def handle_operational(
        self,
        message: OperationalActionContext,
        ctx: WorkflowContext[Never, SupportRequestResult],
    ) -> None:
        await _emit_trace(
            self.id,
            ctx,
            "Assemble",
            "Building final support result with prepared artifact.",
            WorkflowTraceStepKind.Detail,
        )
        draft = ctx.get_state(KEY_DRAFT)
        response_text = draft.body if isinstance(draft, CustomerMessageDraft) else message.artifact.payload
        result = _build_result(message.policy_context, response_text)
        await ctx.yield_output(result)


def is_reply_or_clarification(message: PolicyContext) -> bool:
    return message.policy.route in {PolicyRoute.Reply, PolicyRoute.Clarification}


def is_refund_or_cancellation(message: PolicyContext) -> bool:
    return message.policy.route == PolicyRoute.RefundOrCancellation


def is_escalation(message: PolicyContext) -> bool:
    return message.policy.route == PolicyRoute.Escalation


def is_clarification_draft(message: DraftedResponseContext) -> bool:
    return message.policy_context.policy.route == PolicyRoute.Clarification


def is_not_clarification_draft(message: DraftedResponseContext) -> bool:
    return message.policy_context.policy.route != PolicyRoute.Clarification


def is_refund_or_cancellation_action(message: OperationalActionContext) -> bool:
    return message.policy_context.policy.route == PolicyRoute.RefundOrCancellation


def is_clarification_action(message: OperationalActionContext) -> bool:
    return message.policy_context.policy.route == PolicyRoute.Clarification


def _build_result(policy_context: PolicyContext, customer_facing_response: str) -> SupportRequestResult:
    reasoning = [
        *policy_context.policy.reasoning_steps,
        f"Applied policies: {'; '.join(policy_context.policy.applied_policies)}",
    ]

    return SupportRequestResult(
        intent=policy_context.intake.primary_intent,
        sentiment=policy_context.intake.sentiment,
        urgency=policy_context.intake.urgency,
        reasoning=reasoning,
        action_taken=policy_context.policy.action_taken,
        customer_facing_response=customer_facing_response.strip(),
        recommended_next_action=policy_context.policy.recommended_next_action,
    )


def _build_refund_artifact(message: PolicyContext) -> SimulatedArtifact:
    return SimulatedArtifact(
        artifact_type=ArtifactType.RefundTicket,
        display_title="Refund Review Ticket Preview",
        payload=f"""Create refund review ticket.

Customer: {message.request.sender or "unknown"}
Summary: {message.intake.summary}
Handbook basis: {'; '.join(message.policy.applied_policies)}
Recommended next action: {message.policy.recommended_next_action}
""",
        metadata={
            "route": "refund_or_cancellation",
            "action": ActionTaken.RefundTicketCreated.name,
        },
    )


def _build_cancellation_artifact(message: PolicyContext) -> SimulatedArtifact:
    return SimulatedArtifact(
        artifact_type=ArtifactType.CancellationTicket,
        display_title="Cancellation Task Preview",
        payload=f"""Create cancellation task.

Customer: {message.request.sender or "unknown"}
Summary: {message.intake.summary}
Handbook basis: {'; '.join(message.policy.applied_policies)}
Recommended next action: {message.policy.recommended_next_action}
""",
        metadata={
            "route": "refund_or_cancellation",
            "action": ActionTaken.CancellationTicketCreated.name,
        },
    )


def _build_tone_guidance(sentiment: Sentiment) -> str:
    if sentiment == Sentiment.Frustrated:
        return "Acknowledge the frustration explicitly with one short empathetic sentence before moving to the next step."
    if sentiment == Sentiment.Angry:
        return "Start with a short apology and a calm acknowledgement of the frustration, then move straight to the next step without sounding defensive."
    if sentiment == Sentiment.Confused:
        return "Sound reassuring and clear, acknowledging the confusion before explaining the next step."
    return "Keep the acknowledgement warm, brief, and professional."


def _format_list(items: list[str]) -> str:
    values = [item for item in items if item and item.strip()]
    return "; ".join(values) if values else "(none)"


def _coerce_response_value(response: Any, model_type: type[IntakeAssessment] | type[CustomerMessageDraft]) -> Any:
    value = getattr(response, "value", None)
    if isinstance(value, model_type):
        return value

    text = getattr(response, "text", "")
    try:
        return model_type.model_validate_json(text)
    except ValidationError as exc:
        raise RuntimeError(f"Model response did not match {model_type.__name__}: {exc}") from exc


async def _emit_trace(
    executor_id: str,
    ctx: WorkflowContext[Any, Any],
    stage: str,
    detail: str,
    kind: WorkflowTraceStepKind = WorkflowTraceStepKind.Info,
) -> None:
    await ctx.add_event(_data_event(executor_id, WorkflowTraceStep(stage=stage, detail=detail, kind=kind)))


async def _emit_artifact(executor_id: str, ctx: WorkflowContext[Any, Any], artifact: SimulatedArtifact) -> None:
    await ctx.add_event(_data_event(executor_id, artifact))
    await _emit_trace(
        executor_id,
        ctx,
        "Artifact",
        f"Type={artifact.artifact_type.value}; Title={artifact.display_title}"
        + (f"; Ticket={artifact.metadata['ticket_id']}" if "ticket_id" in artifact.metadata else ""),
    )


def _data_event(executor_id: str, data: Any):
    from agent_framework import WorkflowEvent

    return WorkflowEvent.emit(executor_id, data)
