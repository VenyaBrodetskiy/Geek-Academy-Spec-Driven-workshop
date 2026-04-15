# Support Agent Python

Prerequisite: Python 3.10 or newer.

This project implements the `001-support-request-flow` feature as a Microsoft Agent Framework workflow over Azure OpenAI.

1. Create and activate a virtual environment:

```sh
python -m venv .venv
```

On macOS/Linux:

```sh
source .venv/bin/activate
```

On Windows PowerShell:

```powershell
.\.venv\Scripts\Activate.ps1
```

If `python` is not available on your Mac, use `python3` instead for setup and run commands.

2. Run `python -m pip install -r requirements.txt`.
3. Copy `.env.example` to `.env` and fill in your Azure OpenAI values.
	On macOS/Linux: `cp .env.example .env`
	In PowerShell: `Copy-Item .env.example .env`
4. Run:

```sh
python main.py
```

On macOS systems where only `python3` is available, run `python3 main.py`.

Paste one customer message and finish the input with `---`.

## Implemented Flow

1. Normalize the request and detect deterministic handbook-relevant signals.
2. Run AI intake classification with structured output.
3. Apply deterministic handbook routing rules.
4. Draft customer-facing text with AI where needed.
5. Simulate clarification, refund/cancellation, or escalation artifacts locally.
6. Assemble the final `SupportRequestResult` and render any artifact preview.

Ticket-like refund, cancellation, and escalation artifacts are persisted under `storage/`.

## Verify

```sh
python -m compileall app main.py tests
python -m unittest discover
```
