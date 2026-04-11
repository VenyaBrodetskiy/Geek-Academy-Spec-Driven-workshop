COLOR_RESET = "\033[0m"
COLOR_CYAN = "\033[96m"
COLOR_GREEN = "\033[92m"
COLOR_YELLOW = "\033[93m"
COLOR_GRAY = "\033[90m"
COLOR_WHITE = "\033[97m"
COLOR_DARK_YELLOW = "\033[33m"


def write_section_title(title: str, color: str = COLOR_CYAN) -> None:
    print(f"{color}\n=== {title} ==={COLOR_RESET}")


def write_colored_line(text: str, color: str) -> None:
    print(f"{color}{text}{COLOR_RESET}")
