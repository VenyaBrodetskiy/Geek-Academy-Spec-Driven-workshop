from app.agent import create_placeholder_agent
from app.models import (
    ActionTaken,
    Intent,
    Sentiment,
    SupportRequestResult,
    Urgency,
)


class SupportRequestProcessor:
    def __init__(self) -> None:
        self._placeholder_agent = create_placeholder_agent()

    # TODO (lab 1): Replace this method's body with your real support flow.
    #
    # Right now this method:
    #   1. Calls a trivial placeholder agent so you can see a live LLM response
    #      and verify your Foundry/Azure OpenAI connection.
    #   2. Returns a hardcoded SupportRequestResult with mocked classification,
    #      reasoning, and action values.
    #
    # Your job: replace this with a real flow (likely a MAF workflow with several
    # executors) that actually classifies the request, consults the support
    # handbook, decides what to do, and populates every field of
    # SupportRequestResult from real logic instead of the mock values below.
    async def process(self, customer_message: str) -> SupportRequestResult:
        session = self._placeholder_agent.create_session()
        chunks: list[str] = []

        async for update in self._placeholder_agent.run(
            customer_message, session=session, stream=True
        ):
            if getattr(update, "text", None):
                chunks.append(update.text)

        response_text = "".join(chunks).strip()

        return SupportRequestResult(
            intent=Intent.Unclear,
            sentiment=Sentiment.Neutral,
            urgency=Urgency.Low,
            reasoning=[
                "(mock) This reasoning is hardcoded in SupportRequestProcessor.process.",
                "(mock) Replace it with real reasoning steps from your workflow.",
                "(mock) Every field of SupportRequestResult should come from real logic.",
            ],
            action_taken=ActionTaken.ReplySent,
            customer_facing_response=response_text,
            recommended_next_action="(mock) Replace SupportRequestProcessor.process with your real flow.",
        )
