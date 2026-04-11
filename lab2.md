# Lab 2 Task Definition: Customer Support Agentic App with MCP

Continue from your Lab 1 customer support application and extend it with MCP.

Develop a local MCP and integrate it into the existing app, so the system no longer reads everything directly from local code. Instead, use a clean host-tool boundary and call selected capabilities through MCP.

## Technologies

- Python or C#
- Microsoft Agent Framework (MAF)
- Microsoft Foundry as the LLM provider
- Spec Kit
- MCP
- Console application
- Local MCP server with mock data

## Starting Point

- The Lab 1 customer support application, or a provided Lab 2 starter based on the same scenario
- Working multi-agent flow
- Local mock customer and policy data
- App capabilities that currently access data or business logic through local code or helper functions

## Task

Extend the customer support app by developing a local MCP for selected support capabilities and integrating it into the existing flow.

One recommended direction is a hybrid SupportOps MCP that exposes both support data and support actions.

For example:

- Customer lookup can move behind an MCP tool
- Escalation options can be exposed through the MCP
- Refund eligibility, escalation execution, or support recommendations can be exposed as MCP tools

The app should keep the same end-to-end support behavior from Lab 1, but now use MCP where appropriate instead of relying only on local direct access.

The exact internal design is intentionally open. Choose the number of tools, the MCP shape, and the app orchestration approach that best fits your Lab 1 implementation.

## Technical Requirements

- Identify selected capabilities currently implemented locally in Lab 1.
- Move those capabilities behind a local MCP server.
- Expose both:
  - support data access
  - at least one business-support action
- Integrate the MCP into the existing app so the host calls MCP instead of directly reading all local helpers or mock data.
- Preserve the same customer-facing support experience while improving host-server boundaries and reuse.
- Keep the MCP small and understandable for workshop purposes.

## Optional Stretch Tasks

- Add one higher-level MCP capability that uses sampling.
- Use elicitation when the MCP needs more information before completing an action.
- Improve tool contracts, validation, or error handling.
- Add a second MCP tool or split responsibilities more cleanly.

## Outcome

- The Lab 1 console app extended rather than replaced
- A local MCP added behind a clean host-server boundary
- The app using MCP-based capabilities inside the existing support flow
- A clearer separation between orchestration logic and tool implementation
- A working end-to-end demo of a customer support request that uses MCP
