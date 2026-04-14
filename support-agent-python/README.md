# Support Agent Python

Prerequisite: Python 3.10 or newer.

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
