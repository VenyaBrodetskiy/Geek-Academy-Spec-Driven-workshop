from app.console_ui import (
    COLOR_CYAN,
    COLOR_GRAY,
    COLOR_GREEN,
    COLOR_RESET,
    COLOR_WHITE,
    COLOR_YELLOW,
    write_colored_line,
    write_section_title,
)
from app.models import SupportRequestResult


def render(result: SupportRequestResult) -> None:
    write_section_title("Classification", COLOR_CYAN)
    _write_field("Intent", result.intent.value)
    _write_field("Sentiment", result.sentiment.value)
    _write_field("Urgency", result.urgency.value)

    write_section_title("Reasoning", COLOR_CYAN)
    for step in result.reasoning:
        write_colored_line(f"  - {step}", COLOR_GRAY)

    write_section_title("Action", COLOR_CYAN)
    _write_field("Taken", result.action_taken.value)
    if result.recommended_next_action and result.recommended_next_action.strip():
        _write_field("Next", result.recommended_next_action)

    write_section_title("Customer-Facing Response", COLOR_GREEN)
    write_colored_line(result.customer_facing_response, COLOR_YELLOW)


def _write_field(label: str, value: str) -> None:
    print(f"{COLOR_WHITE}  {label:<10}{COLOR_RESET} {value}")
