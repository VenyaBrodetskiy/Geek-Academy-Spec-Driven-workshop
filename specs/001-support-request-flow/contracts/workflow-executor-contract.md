# Workflow Executor Contract

## Purpose

Define the typed contracts passed between workflow executors so implementation does not depend on hidden coupling through shared state.

## Core Rule

- Each edge must pass a typed message object.
- Shared workflow state is allowed for logging, final rendering, and debugging, but not as the primary means of transferring required data between adjacent executors.

## Executor I/O Matrix

| From Executor | To Executor | Edge Payload | Required Fields |
|---|---|---|---|
| Console input | `NormalizeRequestExecutor` | `string` | Full pasted request text |
| `NormalizeRequestExecutor` | `IntakeClassifierExecutor` | `ParsedSupportRequest` | `RawText`, `NormalizedText`, `Body` |
| `IntakeClassifierExecutor` | `PolicyGateExecutor` | `IntakeContext` | `Request`, `Intake` |
| `PolicyGateExecutor` | `DraftCustomerMessageExecutor` | `PolicyContext` | `Request`, `Intake`, `Policy.Route`, `Policy.MessageMode` |
| `PolicyGateExecutor` | `OperationalActionExecutor` | `PolicyContext` | `Policy.ArtifactMode`, `ActionTaken` |
| `PolicyGateExecutor` | `PrepareEscalationArtifactExecutor` | `PolicyContext` | `Policy.Route=Escalation`, escalation reasoning |
| `DraftCustomerMessageExecutor` | `OperationalActionExecutor` | `DraftedResponseContext` | Draft plus policy context when clarification draft must also generate local email artifact |
| `OperationalActionExecutor` | `DraftCustomerMessageExecutor` | `OperationalActionContext` | Artifact plus policy context when operational acknowledgement still needs drafting |
| `PrepareEscalationArtifactExecutor` | `DraftCustomerMessageExecutor` | `EscalationContext` | Queue, SLA, next steps, artifact |
| `DraftCustomerMessageExecutor` | `AssembleSupportResultExecutor` | `DraftedResponseContext` | Final customer draft and optional artifact |
| `OperationalActionExecutor` | `AssembleSupportResultExecutor` | `OperationalActionContext` | Artifact when no extra draft step is required |

## Contract Invariants

1. `IntakeContext` always contains both normalized request data and AI intake output.
2. `PolicyContext` always contains a resolved route and `ActionTaken`.
3. `DraftedResponseContext` always contains customer-facing text ready for rendering.
4. `OperationalActionContext` always contains a concrete local artifact, never just a string note.
5. `AssembleSupportResultExecutor` must be able to build `SupportRequestResult` from its input payload plus optional shared-state reads for diagnostics only.

## Implementation Pattern

Recommended implementation uses the same approach as multi-input executors in the reference workflow project:

1. Use a normal typed `Executor<TIn, TOut>` where one input shape is enough.
2. Use a route-based executor with multiple typed handlers when the same business executor should accept multiple branch-specific input contracts.
3. Normalize branch-specific contracts back into a single downstream contract as early as possible.

## Preferred Normalization Point

- `DraftCustomerMessageExecutor` is the preferred normalization point for customer-facing branches.
- After drafting, downstream flow should use `DraftedResponseContext` whenever practical.