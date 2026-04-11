import asyncio
import pathlib
import sys

from dotenv import load_dotenv

from app.orchestrator import SupportOrchestrator

load_dotenv(pathlib.Path(__file__).with_name(".env"))


async def main() -> int:
    try:
        orchestrator = SupportOrchestrator()
    except RuntimeError as ex:
        print(ex)
        return 1

    print("Customer Support Agent - type 'quit' to exit\n")

    while True:
        try:
            user_input = input("You: ").strip()
        except (EOFError, KeyboardInterrupt):
            print()
            break

        if user_input.lower() in ("quit", "exit"):
            break
        if not user_input:
            continue

        response = await orchestrator.handle(user_input)
        print(f"Agent: {response}\n")

    return 0


if __name__ == "__main__":
    sys.exit(asyncio.run(main()))
