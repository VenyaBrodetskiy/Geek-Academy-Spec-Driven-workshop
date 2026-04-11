import asyncio
import pathlib
import sys

from dotenv import load_dotenv

from app.orchestrator import SupportOrchestrator

load_dotenv(pathlib.Path(__file__).with_name(".env"))

_RESET = "\033[0m"
_CYAN = "\033[96m"
_GREEN = "\033[92m"
_YELLOW = "\033[93m"
_GRAY = "\033[90m"


def _write_prefix(text: str, color: str) -> None:
    print(f"{color}{text}{_RESET}", end="", flush=True)


def _write_chunk(text: str, color: str) -> None:
    print(f"{color}{text}{_RESET}", end="", flush=True)


async def main() -> int:
    try:
        orchestrator = SupportOrchestrator()
    except RuntimeError as ex:
        print(ex)
        return 1

    print(f"{_CYAN}=== Customer Support Agent ==={_RESET}")
    print(f"{_GRAY}Type 'quit' to exit.{_RESET}\n")

    while True:
        try:
            _write_prefix("Me > ", _CYAN)
            user_input = input().strip()
        except (EOFError, KeyboardInterrupt):
            print()
            break

        if user_input.lower() in ("quit", "exit"):
            break
        if not user_input:
            continue

        _write_prefix("Agent > ", _GREEN)
        async for chunk in orchestrator.handle_stream(user_input):
            _write_chunk(chunk, _YELLOW)
        print("\n")

    return 0


if __name__ == "__main__":
    sys.exit(asyncio.run(main()))
