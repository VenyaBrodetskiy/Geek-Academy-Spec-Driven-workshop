# Tasks: Support Request Processing Flow

**Input**: Design documents from `/specs/001-support-request-flow/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Not included. The specification does not require a TDD-first workflow; validation is covered by scenario verification tasks.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated independently.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare the existing C# skeleton for workflow-based implementation.

- [ ] T001 Add the `Microsoft.Agents.AI.Workflows` package to support-agent-csharp/support-agent-csharp.csproj
- [ ] T002 [P] Document Azure OpenAI workflow configuration in support-agent-csharp/appsettings.json and support-agent-csharp/README.md
- [ ] T003 [P] Add shared workflow state constants in support-agent-csharp/Workflow/State/SupportWorkflowState.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Build the shared workflow contracts and infrastructure required by every route.

**⚠️ CRITICAL**: No user story work should start until this phase is complete.

- [ ] T004 [P] Create parsed-request and policy models in support-agent-csharp/Models/ParsedSupportRequest.cs and support-agent-csharp/Models/PolicyDecision.cs
- [ ] T005 [P] Create intake, draft, artifact, and context contracts in support-agent-csharp/Models/IntakeAssessment.cs, support-agent-csharp/Models/CustomerMessageDraft.cs, support-agent-csharp/Models/SimulatedArtifact.cs, and support-agent-csharp/Models/WorkflowContexts.cs
- [ ] T006 [P] Create shared workflow events in support-agent-csharp/Workflow/Events/IntakeCompletedEvent.cs, support-agent-csharp/Workflow/Events/PolicyAppliedEvent.cs, support-agent-csharp/Workflow/Events/ArtifactPreparedEvent.cs, and support-agent-csharp/Workflow/Events/ResponseDraftedEvent.cs
- [ ] T007 Implement the shared Azure OpenAI `IChatClient` factory in support-agent-csharp/Agents/SupportAgent.cs
- [ ] T008 Implement request normalization and handbook-loading helpers in support-agent-csharp/Workflow/Executors/NormalizeRequestExecutor.cs and support-agent-csharp/Workflow/SupportPolicyRules.cs
- [ ] T009 Implement structured-output intake classification foundations in support-agent-csharp/Workflow/Executors/IntakeClassifierExecutor.cs
- [ ] T010 Implement the base policy-gate contract and route evaluation shell in support-agent-csharp/Workflow/Executors/PolicyGateExecutor.cs
- [ ] T011 Implement final result assembly foundations in support-agent-csharp/Workflow/Executors/AssembleSupportResultExecutor.cs and support-agent-csharp/Common/SupportRequestRenderer.cs
- [ ] T012 Replace the placeholder processor with workflow construction and execution in support-agent-csharp/Orchestration/SupportRequestProcessor.cs and support-agent-csharp/Program.cs

**Checkpoint**: Foundation ready. The app can run a workflow skeleton and emit a `SupportRequestResult` shape, even if story-specific routes are still incomplete.

---

## Phase 3: User Story 1 - Resolve Clear Support Requests (Priority: P1) 🎯 MVP

**Goal**: Handle straightforward plan and policy questions with one clear, handbook-grounded response.

**Independent Test**: Run the console app with direct questions such as API access and cancellation-policy queries and confirm the result is routed to direct reply with a customer-ready answer.

### Implementation for User Story 1

- [ ] T013 [US1] Implement direct-reply handbook rules for plan questions and normal explanations in support-agent-csharp/Workflow/SupportPolicyRules.cs and support-agent-csharp/Workflow/Executors/PolicyGateExecutor.cs
- [ ] T014 [US1] Implement normal reply drafting mode in support-agent-csharp/Workflow/Executors/DraftCustomerMessageExecutor.cs
- [ ] T015 [US1] Wire the direct-reply branch into the workflow graph in support-agent-csharp/Orchestration/SupportRequestProcessor.cs
- [ ] T016 [US1] Verify direct-reply scenarios using support-agent-csharp/Data/sample_requests.md and update the validation notes in specs/001-support-request-flow/quickstart.md

**Checkpoint**: User Story 1 should be independently functional for straightforward questions and explanations.

---

## Phase 4: User Story 2 - Ask for Clarification When Details Are Missing (Priority: P1)

**Goal**: Produce a clarification email instead of guessing when critical information is missing.

**Independent Test**: Run the console app with ambiguous billing or charge-confusion messages and confirm the result produces a clarification email plus a local operational artifact.

### Implementation for User Story 2

- [ ] T017 [US2] Implement missing-information detection and clarification routing in support-agent-csharp/Workflow/Executors/IntakeClassifierExecutor.cs and support-agent-csharp/Workflow/Executors/PolicyGateExecutor.cs
- [ ] T018 [P] [US2] Implement clarification-email artifact generation in support-agent-csharp/Workflow/Executors/OperationalActionExecutor.cs and support-agent-csharp/Models/SimulatedArtifact.cs
- [ ] T019 [P] [US2] Extend draft generation for clarification email mode in support-agent-csharp/Workflow/Executors/DraftCustomerMessageExecutor.cs
- [ ] T020 [US2] Wire the clarification branch and final-assembly handling in support-agent-csharp/Orchestration/SupportRequestProcessor.cs and support-agent-csharp/Workflow/Executors/AssembleSupportResultExecutor.cs
- [ ] T021 [US2] Verify clarification scenarios using support-agent-csharp/Data/sample_requests.md and update the validation notes in specs/001-support-request-flow/quickstart.md

**Checkpoint**: User Story 2 should independently produce clarification emails without relying on interactive follow-up.

---

## Phase 5: User Story 3 - Apply Refund and Cancellation Rules Consistently (Priority: P2)

**Goal**: Route refund and cancellation requests through handbook-compliant operational handling and customer acknowledgements.

**Independent Test**: Run the console app with first-month refund, duplicate-charge, and cancellation-policy requests and confirm the route, action, and reply all align with the handbook.

### Implementation for User Story 3

- [ ] T022 [US3] Implement refund and cancellation rule evaluation in support-agent-csharp/Workflow/SupportPolicyRules.cs and support-agent-csharp/Workflow/Executors/PolicyGateExecutor.cs
- [ ] T023 [P] [US3] Implement refund and cancellation operational artifacts in support-agent-csharp/Workflow/Executors/OperationalActionExecutor.cs
- [ ] T024 [P] [US3] Extend draft generation for refund and cancellation acknowledgements in support-agent-csharp/Workflow/Executors/DraftCustomerMessageExecutor.cs
- [ ] T025 [US3] Wire the refund/cancellation branch and recommended next-action handling in support-agent-csharp/Orchestration/SupportRequestProcessor.cs and support-agent-csharp/Workflow/Executors/AssembleSupportResultExecutor.cs
- [ ] T026 [US3] Verify refund and cancellation scenarios using support-agent-csharp/Data/sample_requests.md and update the validation notes in specs/001-support-request-flow/quickstart.md

**Checkpoint**: User Story 3 should independently handle refund and cancellation routes with correct operational artifacts and acknowledgements.

---

## Phase 6: User Story 4 - Escalate High-Risk or Sensitive Cases (Priority: P3)

**Goal**: Detect escalation triggers and prepare a local escalation artifact plus acknowledgement response.

**Independent Test**: Run the console app with manager requests, chargeback threats, or repeated unresolved billing complaints and confirm the route is escalation with appropriate SLA and next steps.

### Implementation for User Story 4

- [ ] T027 [US4] Implement escalation trigger rules, queue selection, and SLA logic in support-agent-csharp/Workflow/SupportPolicyRules.cs and support-agent-csharp/Workflow/Executors/PolicyGateExecutor.cs
- [ ] T028 [US4] Create escalation event and context contracts in support-agent-csharp/Workflow/Events/EscalationPreparedEvent.cs and support-agent-csharp/Models/WorkflowContexts.cs
- [ ] T029 [US4] Implement escalation artifact preparation in support-agent-csharp/Workflow/Executors/PrepareEscalationArtifactExecutor.cs
- [ ] T030 [US4] Extend draft generation and workflow wiring for escalation acknowledgements in support-agent-csharp/Workflow/Executors/DraftCustomerMessageExecutor.cs and support-agent-csharp/Orchestration/SupportRequestProcessor.cs
- [ ] T031 [US4] Verify escalation scenarios using support-agent-csharp/Data/sample_requests.md and update the validation notes in specs/001-support-request-flow/quickstart.md

**Checkpoint**: User Story 4 should independently route and prepare escalation outcomes for high-risk cases.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Finish tone, documentation, and end-to-end validation across all routes.

- [ ] T032 [P] Refine intake and drafting prompts for handbook tone consistency in support-agent-csharp/Workflow/Executors/IntakeClassifierExecutor.cs and support-agent-csharp/Workflow/Executors/DraftCustomerMessageExecutor.cs
- [ ] T033 [P] Update final setup and run guidance in support-agent-csharp/README.md and specs/001-support-request-flow/quickstart.md
- [ ] T034 Run end-to-end build and scenario validation for support-agent-csharp/support-agent-csharp.csproj and reconcile any final output polish in support-agent-csharp/Common/SupportRequestRenderer.cs

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Can start immediately.
- **Foundational (Phase 2)**: Depends on Setup completion and blocks all story work.
- **User Stories (Phases 3-6)**: Depend on Foundational completion.
- **Polish (Phase 7)**: Depends on all desired user stories being complete.

### User Story Dependencies

- **User Story 1 (P1)**: Starts after Foundational and is the recommended MVP slice.
- **User Story 2 (P1)**: Starts after Foundational; shares workflow files with US1, so parallel work requires coordination.
- **User Story 3 (P2)**: Starts after Foundational; builds on the same gate and drafting executors but remains independently testable.
- **User Story 4 (P3)**: Starts after Foundational; adds escalation-specific contracts and branch wiring.

### Within Each User Story

- Route rules before branch wiring.
- Branch artifacts before final assembly updates.
- Validation after branch wiring is complete.

### Parallel Opportunities

- T002 and T003 can run in parallel during Setup.
- T004, T005, and T006 can run in parallel during Foundational.
- In US2, T018 and T019 can run in parallel after T017.
- In US3, T023 and T024 can run in parallel after T022.
- T032 and T033 can run in parallel during Polish.

---

## Parallel Example: User Story 2

```bash
# After T017 completes, launch these together:
Task: "Implement clarification-email artifact generation in support-agent-csharp/Workflow/Executors/OperationalActionExecutor.cs and support-agent-csharp/Models/SimulatedArtifact.cs"
Task: "Extend draft generation for clarification email mode in support-agent-csharp/Workflow/Executors/DraftCustomerMessageExecutor.cs"
```

## Parallel Example: User Story 3

```bash
# After T022 completes, launch these together:
Task: "Implement refund and cancellation operational artifacts in support-agent-csharp/Workflow/Executors/OperationalActionExecutor.cs"
Task: "Extend draft generation for refund and cancellation acknowledgements in support-agent-csharp/Workflow/Executors/DraftCustomerMessageExecutor.cs"
```

## Parallel Example: User Story 4

```bash
# User Story 4 is mostly sequential because the escalation executor depends on the new escalation contracts:
Task: "Create escalation event and context contracts in support-agent-csharp/Workflow/Events/EscalationPreparedEvent.cs and support-agent-csharp/Models/WorkflowContexts.cs"
Task: "Implement escalation artifact preparation in support-agent-csharp/Workflow/Executors/PrepareEscalationArtifactExecutor.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational.
3. Complete Phase 3: User Story 1.
4. Stop and validate the direct-reply route before expanding the graph.

### Incremental Delivery

1. Add direct reply.
2. Add clarification.
3. Add refund/cancellation handling.
4. Add escalation handling.
5. Finish with prompt and output polish.

### Parallel Team Strategy

1. One developer completes Setup + Foundational.
2. After foundation, branch work can be split carefully:
   - Developer A: direct reply and clarification drafting paths
   - Developer B: operational-action branch
   - Developer C: escalation branch
3. Coordinate changes to shared files such as PolicyGateExecutor, DraftCustomerMessageExecutor, and SupportRequestProcessor.

---

## Notes

- `[P]` tasks use different files and have no blocking dependency on an unfinished sibling task.
- User stories are organized to preserve independent validation even though they share a common workflow graph.
- `SupportRequestResult` remains the final canonical output for every route.