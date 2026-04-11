import os

from agent_framework import Agent
from agent_framework.openai import OpenAIChatCompletionClient


def create_support_agent(customers_json: str, policies_json: str):
    endpoint = _require_env("AZURE_OPENAI_ENDPOINT")
    api_key = _require_env("AZURE_OPENAI_API_KEY")
    deployment_name = _require_env("AZURE_OPENAI_DEPLOYMENT_NAME")

    client = OpenAIChatCompletionClient(
        model=deployment_name,
        azure_endpoint=endpoint,
        api_key=api_key,
    )

    instructions = f"""
You are a helpful customer support agent.
Use the customer and policy data below to resolve issues accurately.
If the data does not contain enough information, ask a concise follow-up question.

Customers:
{customers_json}

Policies:
{policies_json}
""".strip()

    return Agent(
        client=client,
        name="CustomerSupportAgent",
        instructions=instructions,
    )


def _require_env(name: str) -> str:
    value = os.getenv(name)
    if not value or not value.strip():
        raise RuntimeError(f"Missing environment variable {name}. Add it to .env.")
    return value
