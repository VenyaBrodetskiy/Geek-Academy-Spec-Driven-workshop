# Lab 2 Task Definition: Customer Support Agentic App with MCP

Continue from your Lab 1 customer support application and extend it with MCP.

In Lab 1 your agent could use the incoming customer message and the local support
handbook, but it had no way to look up customer/account details and no way to take
concrete business actions beyond generating text. In Lab 2 you close both of those
gaps by building a local **SupportOps MCP server** that gives the agent two new kinds
of capabilities:

- **Data access** — for example customer lookup, so the response can be personalized
  and so policy rules that depend on history can actually be evaluated.
- **Support actions** — taking concrete business actions such as creating a ticket,
  opening an escalation, or recording a refund request.

The host (your Lab 1 app) stops producing text-only outputs and starts calling MCP
tools where real behavior is needed.

## Technologies

- Python or C#
- Microsoft Agent Framework (MAF)
- Microsoft Foundry as the LLM provider
- Spec Kit
- MCP
- Console application
- Local MCP server with mock data
- Recommended for this workshop: Streamable HTTP MCP transport so the server can
  be checked independently before it is connected to the agent.

## Starting Point

- Your Lab 1 customer support application (or a provided Lab 2 starter based on the
  same scenario)
- Any mock data you introduced in Lab 1 (for example policies). Lab 2 adds a new kind
  of data — customer information — which you will place inside the MCP server rather
  than on the host.
- Starter customer/account data is provided in `mock-data-lab2/mock_customers.json`.
  Use it as the operational data source for your MCP customer lookup tool. You may
  copy it into your MCP server project or load it directly, but keep the sample
  customer emails so the provided support requests continue to work.

## Task

Build a local MCP server that exposes support capabilities, and integrate it into your
Lab 1 app so the agent calls MCP tools for the new behaviors.

You decide:

- How many tools to expose and what they are called.
- Which tools are data-access and which are business actions.
- How the host orchestrates calls to the MCP.
- Where policy evaluation lives — on the host, inside the MCP, or split between them.

Some directions you can consider (pick what fits your Lab 1 implementation — you do
not need to do all of these):

- A customer lookup tool so the agent can personalize responses.
- A ticket creation or escalation tool as a mock business action.
- A refund eligibility tool that applies refund policy rules.
- A combined `SupportOps` MCP exposing several of the above.

## Technical Requirements

- Build a local MCP server exposing at least:
  - one data-access tool
  - one business-support action tool
- Create the MCP server as a separate local app/project from the Lab 1 host app
  so it can be run and tested independently.
- Prefer a local Streamable HTTP MCP server for the first implementation. For
  example, expose the MCP endpoint at `http://localhost:5058/mcp` and use
  stateless transport if the server only handles request/response tools and does
  not use elicitation, sampling, roots, or other server-to-client requests.
- Before integrating with the Lab 1 app, validate the MCP server with MCP Inspector:
  - Start the MCP server, for example `dotnet run --project support-ops-mcp-csharp`.
  - Start Inspector with `npx -y @modelcontextprotocol/inspector`.
  - In Inspector, choose Streamable HTTP transport and connect to
    `http://localhost:5058/mcp`.
  - Confirm the expected tools are listed.
  - Call at least one data-access tool and one action tool successfully.
- Integrate the MCP into your Lab 1 app so the host calls MCP where appropriate
  instead of only producing text.
- Preserve the same customer-facing support experience from Lab 1.
- Keep the MCP small and understandable for workshop purposes.

## Optional Stretch Tasks

- Add a higher-level MCP capability that uses `sampling`.
- Use `elicitation` when the MCP needs more information from the host before completing
  an action.
- Split the MCP into two smaller servers (data vs. actions) and have the host call both.
- Improve tool contracts, validation, or error handling.

## Outcome

- The Lab 1 console app extended rather than replaced
- A local MCP server added behind a clean host-server boundary
- At least one real business action taken through the MCP
- A working end-to-end demo of a customer support request that uses MCP
