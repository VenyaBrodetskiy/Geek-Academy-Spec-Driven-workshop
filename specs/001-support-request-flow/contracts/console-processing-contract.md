# Console Processing Contract

## Purpose

Define the operator-facing contract for the C# console prototype and the workflow-level outputs that must exist even when no external systems are called.

## Input Contract

- The operator pastes one customer message into the console.
- Input ends when the operator enters a line containing only `---`.
- The pasted message may include free-form email-like headers such as `From:` and `Subject:` but the system must also handle plain text.

## Core Output Contract

Each successful run must produce one `SupportRequestResult` with:

- `Intent`
- `Sentiment`
- `Urgency`
- `Reasoning`
- `ActionTaken`
- `CustomerFacingResponse`
- `RecommendedNextAction` when applicable

## Route Semantics

### Direct Reply Route

- Trigger: handbook-safe question, explanation, or complaint that does not need clarification or escalation.
- Required outputs:
  - Final `SupportRequestResult`
  - Customer-facing reply text

### Clarification Route

- Trigger: missing critical details prevent a reliable response.
- Required outputs:
  - Final `SupportRequestResult`
  - Clarification email draft text
  - Simulated clarification-email artifact shown in the run output

### Refund or Cancellation Operational Route

- Trigger: request requires a simulated internal action such as refund review or cancellation handling.
- Required outputs:
  - Final `SupportRequestResult`
  - Customer-facing reply text
  - Simulated operational artifact shown in the run output

### Escalation Route

- Trigger: manager request, legal or chargeback language, high-value billing dispute, repeated unresolved issue, or other escalation rule.
- Required outputs:
  - Final `SupportRequestResult`
  - Customer-facing escalation acknowledgement
  - Simulated escalation artifact shown in the run output

## Contract Constraints

- No external email, ticketing, or billing APIs are called in the MVP.
- Simulated artifacts must be clear and human-readable in the console output.
- Policy-sensitive statements must be handbook-grounded.
