# Support Agent Workflow

The workflow keeps routing deterministic and uses agents for classification and message drafting. Ticket-like artifacts are persisted locally under `support-agent-csharp/storage/`.

```mermaid
flowchart TD
    A[Normalize request] --> B[Intake classifier agent]
    B --> C[Policy gate]
    C -->|Reply or clarification| D[Draft customer message]
    C -->|Refund or cancellation| E[Operational action artifact]
    C -->|Escalation| F[Escalation artifact]
    E -. persist ticket markdown .-> S[(storage)]
    F -. persist ticket markdown .-> S
    E --> D
    F --> D
    D -->|Clarification| G[Clarification artifact]
    D -->|Reply or acknowledgement| H[Assemble result]
    G --> H
```

`Agent Trace` lines are emitted while the workflow runs so the console shows intake, policy, artifact, drafting, storage, and final assembly progress.
