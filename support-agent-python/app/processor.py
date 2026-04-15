from collections.abc import Callable

from agent_framework import WorkflowBuilder

from app.agent import (
    create_chat_client,
    create_intake_classifier_agent,
    create_message_drafting_agent,
)
from app.models import (
    SimulatedArtifact,
    SupportProcessingOutcome,
    SupportRequestResult,
    WorkflowTraceStep,
    WorkflowTraceStepKind,
)
from app.policy import SupportPolicyRules
from app.workflow import (
    AssembleSupportResultExecutor,
    DraftCustomerMessageExecutor,
    IntakeClassifierExecutor,
    NormalizeRequestExecutor,
    OperationalActionExecutor,
    PolicyGateExecutor,
    PrepareEscalationArtifactExecutor,
    is_clarification_action,
    is_clarification_draft,
    is_escalation,
    is_not_clarification_draft,
    is_refund_or_cancellation,
    is_refund_or_cancellation_action,
    is_reply_or_clarification,
)


class SupportRequestProcessor:
    def __init__(self) -> None:
        chat_client = create_chat_client()
        self._policy_rules = SupportPolicyRules(SupportPolicyRules.load_handbook())
        self._intake_agent = create_intake_classifier_agent(chat_client)
        self._drafting_agent = create_message_drafting_agent(chat_client)

    async def process(
        self,
        customer_message: str,
        on_trace_step: Callable[[WorkflowTraceStep], None] | None = None,
    ) -> SupportProcessingOutcome:
        workflow = self._build_workflow()

        result: SupportRequestResult | None = None
        artifact: SimulatedArtifact | None = None
        trace: list[WorkflowTraceStep] = []

        stream = workflow.run(customer_message, stream=True)
        async for event in stream:
            if event.type == "data":
                data = event.data
                if isinstance(data, WorkflowTraceStep):
                    trace.append(data)
                    if on_trace_step is not None:
                        on_trace_step(data)
                elif isinstance(data, SimulatedArtifact):
                    artifact = data
            elif event.type == "output" and isinstance(event.data, SupportRequestResult):
                result = event.data
                _add_trace(trace, WorkflowTraceStep("Complete", "SupportRequestResult produced."), on_trace_step)
            elif event.type == "failed":
                detail = event.details.message if event.details is not None else "Workflow execution failed."
                _add_trace(
                    trace,
                    WorkflowTraceStep("Error", detail, WorkflowTraceStepKind.Error),
                    on_trace_step,
                )

        if result is None:
            raise RuntimeError("Workflow completed without producing a SupportRequestResult.")

        return SupportProcessingOutcome(result=result, artifact=artifact, trace=trace)

    def _build_workflow(self):
        normalize = NormalizeRequestExecutor(self._policy_rules)
        intake = IntakeClassifierExecutor(self._intake_agent)
        policy_gate = PolicyGateExecutor()
        draft = DraftCustomerMessageExecutor(self._drafting_agent, self._policy_rules)
        action = OperationalActionExecutor()
        escalation = PrepareEscalationArtifactExecutor()
        assemble = AssembleSupportResultExecutor()

        return (
            WorkflowBuilder(start_executor=normalize, output_executors=[assemble])
            .add_edge(normalize, intake)
            .add_edge(intake, policy_gate)
            .add_edge(policy_gate, draft, is_reply_or_clarification)
            .add_edge(policy_gate, action, is_refund_or_cancellation)
            .add_edge(policy_gate, escalation, is_escalation)
            .add_edge(draft, action, is_clarification_draft)
            .add_edge(draft, assemble, is_not_clarification_draft)
            .add_edge(action, draft, is_refund_or_cancellation_action)
            .add_edge(action, assemble, is_clarification_action)
            .add_edge(escalation, draft)
            .build()
        )


def _add_trace(
    trace: list[WorkflowTraceStep],
    step: WorkflowTraceStep,
    on_trace_step: Callable[[WorkflowTraceStep], None] | None,
) -> None:
    trace.append(step)
    if on_trace_step is not None:
        on_trace_step(step)
