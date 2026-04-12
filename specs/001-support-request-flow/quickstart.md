# Quickstart: Support Request Processing Flow

## Goal

Implement the `support-agent-csharp` skeleton as a Microsoft Agent Framework workflow that classifies, routes, and responds to support requests while simulating clarification emails and escalation or operational actions locally.

## Planned Environment

1. Use the `support-agent-csharp` project only.
2. Keep the existing Azure OpenAI + MAF OpenAI stack and add workflows.
3. Keep the model deployment configurable.

## Planned Configuration

Keep the current Azure OpenAI-style configuration shape used by the skeleton and the reference workflow project.

Expected values after implementation:

```json
{
  "ModelName": "gpt-4.1",
  "Endpoint": "https://your-resource.openai.azure.com/",
  "ApiKey": "your-api-key"
}
```

If you later want Entra-based auth instead of API key auth, keep the same workflow structure and swap the client construction only.

## Planned Package Setup

Already present in the skeleton:

- `Azure.AI.OpenAI` 2.1.0
- `Microsoft.Agents.AI.OpenAI` 1.1.0

Add only the workflow package:

```powershell
dotnet add support-agent-csharp\support-agent-csharp.csproj package Microsoft.Agents.AI.Workflows --version 1.1.0
```

Optional if you later switch to Entra auth:

```powershell
dotnet add support-agent-csharp\support-agent-csharp.csproj package Azure.Identity
```

## Planned Run Flow

1. Start the console app.
2. Paste one sample support request.
3. The workflow runs these logical stages:
   - normalize request
   - classify intake with structured output
   - route with deterministic policy logic
   - draft customer text
   - simulate clarification, refund/cancellation, or escalation artifact when needed
   - assemble `SupportRequestResult`
4. Review the rendered classification, reasoning, action, response, and any simulated artifact.

## Primary Scenarios to Verify

1. API access question -> direct reply
2. First-month refund -> refund or refund-review path
3. Ambiguous billing confusion -> clarification email path
4. Explicit manager request or chargeback threat -> escalation path
5. Explicit cancellation request -> cancellation handling path
6. Cross-account disclosure request -> direct refusal without sharing account details

## Planned Verification Commands

```powershell
dotnet build support-agent-csharp\support-agent-csharp.csproj
dotnet run --project support-agent-csharp\support-agent-csharp.csproj
```

## Validation Notes

Validated against the implemented console workflow on 2026-04-12:

1. Direct reply
  - API access on Basic routed to `ReplySent` with a Premium-only answer.
2. Clarification
  - Duplicate-charge complaint routed to `ClarificationRequested` with a clarification email preview.
3. Refund handling
  - First-month, low-usage Premium refund routed to `RefundTicketCreated` with a refund review ticket preview.
4. Cancellation handling
  - Explicit cancellation request routed to `CancellationTicketCreated` with a cancellation task preview.
5. Escalation
  - Manager escalation request routed to `EscalatedToHuman` with an escalation handoff preview.
6. Disclosure guardrail
  - Cross-account information request routed to a direct refusal without exposing account or payment details.

## Implementation Notes

1. Use MAF workflow namespaces from current docs: `Microsoft.Agents.AI.Workflows`.
2. Use non-generic workflow build syntax: `var workflow = builder.Build();`.
3. Match the `6-AgentFrameworkWorkflows` pattern: create one shared `IChatClient`, inject it into AI executors, and keep policy routing deterministic.