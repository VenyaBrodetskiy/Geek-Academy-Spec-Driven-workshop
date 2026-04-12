# Research: Support Request Processing Flow

## Decision 1: Build in the existing C# skeleton on .NET 10

- Decision: Use `support-agent-csharp/` as the implementation target and keep the console entry point as the operator-facing shell.
- Rationale: The user explicitly selected C#. The skeleton already contains configuration loading, rendering, and a placeholder processor that can be replaced incrementally.
- Alternatives considered:
  - Python skeleton: rejected because the user selected C#.
  - New separate workflow host project: rejected because the current scope fits inside the existing console app.

## Decision 2: Use the same Azure OpenAI + MAF workflow pattern as the reference workflow project

- Decision: Stay with `Azure.AI.OpenAI` + `Microsoft.Agents.AI.OpenAI` and add `Microsoft.Agents.AI.Workflows`, matching the already-working `6-AgentFrameworkWorkflows` approach.
- Rationale: The target skeleton already uses Azure OpenAI and MAF OpenAI integration, and the reference workflow project proves that this stack is enough for structured-output agent executors plus workflow orchestration. Foundry is optional here, not required.
- Alternatives considered:
  - Keep `Microsoft.Agents.AI.OpenAI` only: rejected because the feature explicitly targets workflows.
  - Switch to `Microsoft.Agents.AI.Foundry`: rejected for this lab because it adds a different client model than the one you already use and prefer.

## Decision 3: Prefer a workflow over a single support agent

- Decision: Use a typed workflow with explicit executors and edges instead of one large agent prompt.
- Rationale: The process has well-defined steps with policy-sensitive branching. Workflow structure gives deterministic routing, observable execution, and safer separation between fuzzy interpretation and business rules.
- Alternatives considered:
  - Single agent with a long prompt: rejected because it would blur handbook enforcement and make route decisions harder to validate.
  - Fully rule-based parser: rejected because messy customer language and intent ambiguity are part of the core problem.

## Decision 4: Split responsibility between AI and deterministic logic

- Decision: Use AI for intake classification and customer-facing draft generation; use deterministic executors for handbook policy routing, escalation logic, simulated actions, and final result assembly.
- Rationale: This minimizes hallucination risk where the business rules are explicit while still using the model where language ambiguity is highest.
- Alternatives considered:
  - AI-driven routing: rejected because refund, escalation, and disclosure rules should not depend on prompt interpretation alone.
  - Rule-only drafting: rejected because the output needs to sound human and adapt to messy input.

## Decision 5: Use a route-driven workflow graph with a policy gate plus conditional edges

- Decision: Use a shared core pipeline `normalize -> intake -> policy_gate`, then route into branch-specific executors through thin conditional edges.
- Rationale: This aligns closely with the reference `6-AgentFrameworkWorkflows` structure, where the policy gate computes route metadata and edge conditions read that decision.
- Alternatives considered:
  - Parallel fan-out for all branches: rejected for MVP because it adds synchronization complexity without improving workshop value.
  - Deeply nested route subgraphs: rejected because they complicate the first implementation.

## Decision 6: Keep model deployment configurable, with GPT-4.1 as the planning default

- Decision: Recommend `gpt-4.1` or `gpt-4.1-mini` for the plan, but keep `ModelName` configurable because the workspace does not include a verified deployment inventory.
- Rationale: Microsoft model guidance recommends GPT-4.1 for customer support and low-latency chat workloads, while GPT-5 is better when deep reasoning is worth the latency tradeoff.
- Alternatives considered:
  - Hardcode `gpt-4o`: rejected because it reflects the older skeleton rather than current model guidance.
  - Hardcode `gpt-5.4-mini`: rejected because this workload prioritizes responsiveness over maximum reasoning depth and access was not verified.

## Decision 7: Keep outbound operational effects local, but name executors after business purpose rather than simulation

- Decision: Clarification emails, refund/cancellation tickets, and escalation handoffs will remain local workflow artifacts and console-visible outputs, but executor names will describe the business action rather than the fact that the prototype simulates it.
- Rationale: The spec explicitly says MVP behavior should imitate sending without external APIs. This keeps the workshop focused on decisioning and response quality rather than transport plumbing.
- Alternatives considered:
  - Real email or ticket integrations: rejected as out of scope for Lab 1.

## Decision 8: Preserve `SupportRequestResult` as the canonical output contract

- Decision: Keep `SupportRequestResult` as the final workflow output and treat simulated artifacts as workflow-side outputs or state rather than expanding the result model immediately.
- Rationale: The existing renderer and the clarified spec both depend on that contract. Simulated artifacts can still be shown in the console without forcing a broader model change.
- Alternatives considered:
  - Replace `SupportRequestResult` with a larger run-result envelope: rejected because it would create unnecessary churn in the skeleton before the core flow exists.