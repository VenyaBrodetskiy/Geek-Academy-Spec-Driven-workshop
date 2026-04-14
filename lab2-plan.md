# Lab 2 Plan: Build SupportOps MCP Server First

## Summary

Save this plan as `lab2-plan.md` at the repo root.

Yes, create a separate C# MCP server project. For the first Lab 2 step, build only the MCP server and do not connect it to the existing `support-agent-csharp` client yet.

Use the official MCP C# SDK for the server. Keep Microsoft Agent Framework as the host/client-side technology to investigate in the next step. This is the cleanest split: MCP server exposes reusable tools; MAF workflow or MAF agents can consume those tools later.

Verified current direction from official docs:

- MAF .NET can use the official MCP C# SDK to call MCP tools.
- MAF can also expose an agent as an MCP server tool, but that is a different pattern and should stay out of the first server-focused task.
- NuGet check on 2026-04-14 showed current stable packages available: `Microsoft.Agents.AI.*` 1.1.0 and `ModelContextProtocol` 1.2.0.

## Key Decisions

- Create new project: `support-ops-mcp-csharp`.
- Do not modify or connect `support-agent-csharp` yet.
- Implement a local stateless Streamable HTTP MCP server.
- Use deterministic tools, not an agent inside the MCP server.
- Keep the server small enough for workshop participants to understand MCP mechanics before mixing in client-side MAF integration.
- Treat MAF connection as a separate follow-up investigation.

## MCP Server Shape

Create a C# ASP.NET Core app with:

- `ModelContextProtocol.AspNetCore` package, latest stable.
- Streamable HTTP transport.
- `Stateless = true`, because the first server version does not use elicitation, sampling, roots, or other server-to-client requests.
- local endpoint `http://localhost:5058/mcp`.
- tool registration through the MCP C# SDK.
- mock data stored locally in the MCP server project.

Expose two first-version tools:

1. `lookup_customer`

Purpose: data-access MCP tool.

Input: customer email.

Output: customer/account profile from mock data.

Example fields: email, name, plan, signup date, last charge, refund count, contact count, status.

2. `create_support_ticket`

Purpose: business-action MCP tool.

Input: ticket kind, customer email, summary, reason, recommended next action, priority.

Output: mock ticket creation result.

Supported ticket kinds: `refund_review`, `cancellation`, `escalation`.

Use normal ASP.NET Core logging. HTTP transport does not require stdout to be reserved for protocol traffic.

## Implementation Notes

Mock customer data should include sample emails already used in `support-agent-csharp/Data/sample_requests.md`, especially:

- `alice@example.com`
- `jordan.p@example.com`
- `sam.r@example.com`
- `meera.k@example.com`
- `dev.tan@example.com`
- `rachel.b@example.com`
- `tomas.l@example.com`
- `priya.s@example.com`

The server should return predictable data, not call Foundry, Azure OpenAI, ticketing APIs, or databases.

Tool contracts should be explicit and boring. This lab is about MCP boundaries, not clever inference.

If an email is unknown, `lookup_customer` should return `found = false` instead of throwing.

If `create_support_ticket` receives an invalid ticket kind or missing email, it should return a clear tool error.

## Next Investigation After This Plan

After the standalone MCP server works, investigate client integration separately:

- Option A: existing MAF workflow calls MCP through a typed wrapper around the MCP C# SDK.
- Option B: convert MCP tools to `AITool`/function tools and let a MAF agent choose when to call them.
- Option C: show both, but keep deterministic workflow calls as the main workshop path.

For the current app, Option A is probably the better next step because `support-agent-csharp` already uses a deterministic MAF workflow and policy routing.

## Test Plan

Run server build:

```powershell
dotnet build support-ops-mcp-csharp
```

Run the HTTP MCP server:

```powershell
dotnet run --project support-ops-mcp-csharp
```

Validate MCP behavior with MCP Inspector or a tiny local MCP client:

- Server starts at `http://localhost:5058`.
- MCP endpoint is available at `http://localhost:5058/mcp`.
- Tool list contains `lookup_customer` and `create_support_ticket`.
- `lookup_customer("alice@example.com")` returns a Premium customer profile.
- `lookup_customer("unknown@example.com")` returns `found = false`.
- `create_support_ticket` returns a generated ticket id and queue.
- Invalid ticket kind returns a readable error.
- Inspector can connect using Streamable HTTP transport.

## Assumptions

- Target stack is C# only.
- First saved artifact is `lab2-plan.md`.
- This task creates only the MCP server.
- No client integration in this first task.
- MAF remains the client-side framework to investigate next.
- The MCP server itself should use the official MCP SDK, not a MAF agent, unless a later stretch explicitly explores "agent as MCP tool."

## References

- MAF local MCP tools: https://learn.microsoft.com/en-us/agent-framework/agents/tools/local-mcp-tools
- MCP C# SDK: https://github.com/modelcontextprotocol/csharp-sdk
- MCP server guide: https://modelcontextprotocol.io/docs/develop/build-server
