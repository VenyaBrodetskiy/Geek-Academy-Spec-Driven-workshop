import os

from agent_framework import Agent
from agent_framework.openai import OpenAIChatCompletionClient


# TODO (lab 1): This file is a placeholder to show the minimal pattern for creating
# and calling a MAF agent. Your real solution will almost certainly replace it with
# multiple specialized agents (classifier, responder, escalation-prep, etc.) inside
# a MAF workflow. See lab1.md.
def create_placeholder_agent():
    endpoint = _require_env("AZURE_OPENAI_ENDPOINT")
    api_key = _require_env("AZURE_OPENAI_API_KEY")
    deployment_name = _require_env("AZURE_OPENAI_DEPLOYMENT_NAME")

    client = OpenAIChatCompletionClient(
        model=deployment_name,
        azure_endpoint=endpoint,
        api_key=api_key,
    )

    instructions = """
You are a friendly customer support assistant.
Respond briefly and politely to acknowledge the customer's message.
Do not try to solve the issue, do not promise anything, and do not ask
for additional information. Keep the reply to two or three sentences.
""".strip()

    return Agent(
        client=client,
        name="PlaceholderSupportAgent",
        instructions=instructions,
    )


def _require_env(name: str) -> str:
    value = os.getenv(name)
    if not value or not value.strip():
        raise RuntimeError(f"Missing environment variable {name}. Add it to .env.")
    return value
