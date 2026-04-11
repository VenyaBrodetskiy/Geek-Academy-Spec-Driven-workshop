# Lab 2 Task Definition: Customer Support Agentic App with MCP

Continue from your Lab 1 customer support application and extend it with MCP.

In Lab 1 your agent only saw the incoming customer message. It had no way to know who
the customer is, and no way to take concrete business actions beyond generating text.
In Lab 2 you will close both of those gaps by building a local **SupportOps MCP server**
that gives the agent two new kinds of capabilities:

- **Data access** — for example customer lookup, so the response can be personalized
  and so policy rules that depend on history can actually be evaluated.
- **Support actions** — taking concrete business actions such as creating a ticket,
  opening an escalation, or recording a refund request.

The host (your Lab 1 app) stops producing text-only outputs and starts calling MCP
tools where real behavior is needed. The result is a cleaner host/server boundary and
a demo of how the same agent system can be extended with reusable tools.

## Technologies

- Python or C#
- Microsoft Agent Framework (MAF)
- Microsoft Foundry as the LLM provider
- Spec Kit
- MCP
- Console application
- Local MCP server with mock data

## Starting Point

- Your Lab 1 customer support application (or a provided Lab 2 starter based on the
  same scenario)
- Any mock data you introduced in Lab 1 (for example policies). Lab 2 adds a new kind
  of data — customer information — which you will place inside the MCP server rather
  than on the host.

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
