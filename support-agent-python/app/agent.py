import os

from agent_framework import Agent
from agent_framework.openai import OpenAIChatCompletionClient


def create_chat_client() -> OpenAIChatCompletionClient:
    endpoint = _require_env("AZURE_OPENAI_ENDPOINT")
    api_key = _require_env("AZURE_OPENAI_API_KEY")
    deployment_name = _require_env("AZURE_OPENAI_DEPLOYMENT_NAME")

    return OpenAIChatCompletionClient(
        model=deployment_name,
        azure_endpoint=endpoint,
        api_key=api_key,
    )


def create_intake_classifier_agent(client: OpenAIChatCompletionClient) -> Agent:
    instructions = """
You are a support intake classifier.
Return JSON that matches the requested schema exactly.
Use only these intent values: Unclear, Refund, Cancellation, Question, Complaint.
Use only these sentiment values: Neutral, Frustrated, Angry, Confused.
Use only these urgency values: Low, Medium, High.
Be conservative. If details are missing for safe billing or refund handling, populate missing_information.
If you are unsure whether the customer is asking for a refund or only asking a question, prefer Question or Unclear and explain the uncertainty in confidence_notes.
""".strip()

    return Agent(
        client=client,
        name="SupportIntakeClassifier",
        instructions=instructions,
    )


def create_message_drafting_agent(client: OpenAIChatCompletionClient) -> Agent:
    instructions = """
You draft customer support messages.
Return JSON that matches the requested schema exactly.
Keep the tone short, human, and calm.
Do not invent company rules, features, or exceptions.
Do not use markdown.
""".strip()

    return Agent(
        client=client,
        name="SupportMessageDrafter",
        instructions=instructions,
    )


def _require_env(name: str) -> str:
    value = os.getenv(name)
    if not value or not value.strip():
        raise RuntimeError(f"Missing environment variable {name}. Add it to .env.")
    return value.strip()
