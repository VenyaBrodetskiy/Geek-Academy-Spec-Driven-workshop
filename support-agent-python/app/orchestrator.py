import json
import pathlib

from app.agent import create_support_agent

DATA_DIR = pathlib.Path(__file__).parent.parent / "data"


class SupportOrchestrator:
    def __init__(self) -> None:
        customers_json = json.dumps(
            json.loads((DATA_DIR / "customers.json").read_text(encoding="utf-8")),
            indent=2,
        )
        policies_json = json.dumps(
            json.loads((DATA_DIR / "policies.json").read_text(encoding="utf-8")),
            indent=2,
        )

        self.agent = create_support_agent(customers_json, policies_json)
        self.session = self.agent.create_session()

    async def handle(self, user_message: str) -> str:
        response = await self.agent.run(user_message, session=self.session)
        return response.text.strip()
