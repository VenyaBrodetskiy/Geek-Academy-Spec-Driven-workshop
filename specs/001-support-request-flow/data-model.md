# Data Model: Support Request Processing Flow

## Entity: ParsedSupportRequest

- Purpose: Normalized representation of the raw pasted support message.
- Fields:
  - `RawText`
  - `NormalizedText`
  - `Sender` (optional)
  - `Subject` (optional)
  - `Body`
  - `DetectedSignals` (keywords, pricing mentions, legal terms, cancellation phrases, etc.)

## Entity: IntakeAssessment

- Purpose: Structured AI interpretation of the request before handbook policy is applied.
- Fields:
  - `PrimaryIntent`
  - `SecondaryIntentCues`
  - `Sentiment`
  - `Urgency`
  - `MissingInformation`
  - `EscalationSignals`
  - `Summary`
  - `ConfidenceNotes`
- Validation rules:
  - `PrimaryIntent` must map to the allowed `Intent` values in the spec.
  - `Sentiment` and `Urgency` must map to allowed enum sets.
  - `MissingInformation` can be empty but not null.

## Entity: PolicyDecision

- Purpose: Deterministic handbook-grounded route and action decision.
- Fields:
  - `Route` (`Reply`, `Clarification`, `RefundOrCancellation`, `Escalation`)
  - `ActionTaken`
  - `RecommendedNextAction`
  - `AppliedPolicies`
  - `ReasoningSteps`
  - `MessageMode` (`Reply`, `ClarificationEmail`, `EscalationAcknowledgement`)
  - `ArtifactMode` (optional; `ClarificationEmail`, `RefundTicket`, `CancellationTicket`, `EscalationHandoff`)
- Validation rules:
  - Route and `ActionTaken` must remain consistent with the selected workflow branch.
  - `AppliedPolicies` must reference handbook-backed rules only.

## Entity: CustomerMessageDraft

- Purpose: AI-generated customer-facing text for the chosen route.
- Fields:
  - `Mode`
  - `SubjectLine` (optional)
  - `Body`
  - `ToneChecks`
- Validation rules:
  - Must be short, human, and policy-consistent.
  - Must not invent company rules or unsupported features.

## Entity: SimulatedArtifact

- Purpose: Local representation of an outbound operational action.
- Fields:
  - `ArtifactType`
  - `DisplayTitle`
  - `Payload`
  - `Metadata`
- Examples:
  - Clarification email preview
  - Refund review ticket preview
  - Cancellation task preview
  - Escalation handoff preview tagged `ESCALATE`

## Entity: SupportRequestResult

- Purpose: Final canonical per-request output contract already present in the C# skeleton.
- Fields:
  - `Intent`
  - `Sentiment`
  - `Urgency`
  - `Reasoning`
  - `ActionTaken`
  - `CustomerFacingResponse`
  - `RecommendedNextAction`
- Validation rules:
  - All non-nullable fields must be populated for every run.
  - `RecommendedNextAction` may be null only when no further internal follow-up is needed.

## Workflow State Aggregates

- `WorkflowRequestState`: stores `ParsedSupportRequest` and handbook snapshot reference.
- `WorkflowAssessmentState`: stores `IntakeAssessment`.
- `WorkflowDecisionState`: stores `PolicyDecision`.
- `WorkflowArtifactState`: stores optional `CustomerMessageDraft` and `SimulatedArtifact`.

## Message Context Contracts

## Entity: IntakeContext

- Purpose: Message contract passed from normalization into policy evaluation.
- Fields:
  - `Request` (`ParsedSupportRequest`)
  - `Intake` (`IntakeAssessment`)

## Entity: PolicyContext

- Purpose: Message contract passed out of policy gating and into route branches.
- Fields:
  - `Request` (`ParsedSupportRequest`)
  - `Intake` (`IntakeAssessment`)
  - `Policy` (`PolicyDecision`)

## Entity: OperationalActionContext

- Purpose: Message contract passed after a local clarification, refund, or cancellation artifact is produced.
- Fields:
  - `PolicyContext`
  - `Artifact` (`SimulatedArtifact`)
  - `OperatorNote` (optional)

## Entity: EscalationContext

- Purpose: Message contract passed after escalation preparation.
- Fields:
  - `PolicyContext`
  - `Artifact` (`SimulatedArtifact`)
  - `Queue`
  - `Sla`
  - `NextSteps`

## Entity: DraftedResponseContext

- Purpose: Common message contract consumed by the final assembler after any customer-facing draft is created.
- Fields:
  - `PolicyContext`
  - `Draft` (`CustomerMessageDraft`)
  - `Artifact` (`SimulatedArtifact`, optional)

## State Transitions

1. `Raw input` -> `ParsedSupportRequest`
2. `ParsedSupportRequest` + AI classification -> `IntakeContext`
3. `IntakeContext` + deterministic routing -> `PolicyContext`
4. `PolicyContext` -> branch-specific context (`DraftedResponseContext`, `OperationalActionContext`, or `EscalationContext`)
5. Branch context + final draft/artifact merge -> `SupportRequestResult`