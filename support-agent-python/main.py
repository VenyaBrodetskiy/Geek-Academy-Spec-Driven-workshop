import asyncio
import pathlib
import sys

from dotenv import load_dotenv

from app import renderer
from app.console_ui import (
    COLOR_CYAN,
    COLOR_DARK_YELLOW,
    COLOR_GRAY,
    write_colored_line,
    write_section_title,
)
from app.processor import SupportRequestProcessor

load_dotenv(pathlib.Path(__file__).with_name(".env"))


async def main() -> int:
    try:
        processor = SupportRequestProcessor()
    except RuntimeError as ex:
        print(ex)
        return 1

    write_section_title("Customer Support — Request Processor", COLOR_CYAN)
    write_colored_line(
        "Paste a customer message, then end it with a line containing only '---'.",
        COLOR_GRAY,
    )
    write_colored_line("Type 'quit' on its own line to exit.", COLOR_GRAY)

    while True:
        write_section_title("Paste customer message (end with '---')", COLOR_CYAN)

        message = read_multiline_input()
        if message is None:
            break

        if not message.strip():
            continue

        result = await processor.process(message)
        renderer.render(result)

        write_colored_line(
            "\n[Placeholder run — see SupportRequestProcessor.process to replace with your real flow]",
            COLOR_DARK_YELLOW,
        )

    return 0


def read_multiline_input() -> str | None:
    lines: list[str] = []
    while True:
        try:
            line = input()
        except (EOFError, KeyboardInterrupt):
            print()
            return None

        trimmed = line.strip()

        if not lines and trimmed.lower() == "quit":
            return None

        if trimmed == "---":
            break

        lines.append(line)

    return "\n".join(lines).strip()


if __name__ == "__main__":
    sys.exit(asyncio.run(main()))
