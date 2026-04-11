# Lab 1 Task Definition: Customer Support Agentic App

Build a working customer support agentic app that can receive a customer request, figure out what is needed, gather relevant information, and return a final response.

The repo contains two skeletons: Python and C#. Choose one and implement the lab in that stack.

Start with Spec Kit first: turn the rough business request into clear user stories, requirements, expected behavior, and an implementation plan before coding.

## Technologies

- Python or C#
- Microsoft Agent Framework (MAF)
- Microsoft Foundry as the LLM provider
- Spec Kit
- Console application with local mock data

## Install Spec Kit

- GitHub: https://github.com/github/spec-kit
- Follow the installation guide in the repo README.
- Quick start in this repo:
	- `uv tool install specify-cli --from git+https://github.com/github/spec-kit.git`
	- `specify init --here --ai copilot`

## Starting Point

- A starter repo skeleton in Python and C# (use one)
- Basic console entry point
- Placeholder app/orchestration structure
- Mock customer data and policy data
- Incomplete implementation

## Task

Develop a customer support agentic app that can handle incoming support requests.

Use Spec Kit first to turn the rough business input below into proper user stories, requirements, expected behavior, and an implementation plan.

The implementation approach is intentionally open. You may choose a multi-agent approach with Agent Framework workflows, or a simpler design if it fits your interpretation of the requirements.

## Rough Business Requests

These are intentionally written in messy business language. Refine them into proper user stories, clear logic, and technical tasks.

We need something for support because customers write in a very messy way and half the time it is not even clear whether they want a refund, want to cancel, are angry about being charged again, or just want someone to explain what happened. Some of these cases are simple and should just get a normal answer, some are clearly refund or cancellation related, and some are upset enough that they probably need different handling or maybe escalation. The reply should not feel generic because obviously the answer may depend on who the customer is and what their history looks like. Also, please do not let the system invent company rules, because refunds, cancellations, plan limits, and similar things need to match what we actually allow. If the customer did not give enough details, the app should ask for clarification instead of guessing. From the customer side it should feel like one clear support response, not some weird internal handoff, and if the app does not have enough information it should deal with that in a sensible way instead of making things up.

## Developer Hints

- Use the provided mock data when you need customer-specific information or company policy context.
- The app structure is up to you. Choose the number of agents, workflow steps, or orchestration pattern yourself.
- A multi-agent design with Microsoft Agent Framework workflows is one good option, but it is not required.
- Try creating skills, agent instructions, or hooks to speed up development and keep behavior consistent, but it is optional.

## Optional Extended Tasks

Choose one or more advanced tasks if you finish the main scope earlier.

- Add a human-in-the-loop approval step for selected cases, such as refund-related requests, where the app prepares a recommendation, waits for human approval or rejection, and then continues.
- Add a clarification flow for cases with missing information, where the app asks the user for clarification once and then continues from the updated context without entering a repeated clarification loop.
- Add multi-agent collaboration for more complex cases, where several agents exchange information or review each other's output before the final response is produced.

## Outcome

- A working Python or C# console app built on top of the provided skeleton
- A Spec Kit flow that turns rough requests into structured requirements and a plan
- A customer support flow implemented with MAF
- Customer-aware and policy-aware responses using local mock data
- A clean base that can later be extended with MCP in Lab 2
