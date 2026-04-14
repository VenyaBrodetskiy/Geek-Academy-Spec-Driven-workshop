# Support Agent Workflow

The workflow keeps routing deterministic, but lets agents use MCP tools at the right boundary.

```mermaid
flowchart TD
    A[Normalize request] --> B[Intake classifier agent]
    B -. lookup_customer .-> M[(SupportOps MCP)]
    B --> C[Policy gate]
    C -->|Reply or clarification| D[Draft customer message]
    C -->|Refund, cancellation, escalation| E[Agentic ticket action]
    E -. create_support_ticket .-> M
    E --> D
    D -->|Clarification| F[Clarification artifact]
    D -->|Reply or ticket acknowledgement| G[Assemble result]
    F --> G
```

`lookup_customer` may enrich intake before policy rules run. `create_support_ticket` is only available after the policy gate chooses a ticket-required route.

Created tickets are persisted by the MCP server under `support-ops-mcp-csharp/storage/` as Markdown files. That folder is ignored by git.
