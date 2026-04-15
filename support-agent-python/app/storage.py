from __future__ import annotations

from datetime import datetime, timezone
from pathlib import Path
from threading import Lock

from app.models import ArtifactType, SimulatedArtifact

_sequence = 0
_sequence_lock = Lock()


def persist_if_ticket_artifact(artifact: SimulatedArtifact) -> None:
    if artifact.artifact_type == ArtifactType.ClarificationEmail:
        return

    created_at = datetime.now(timezone.utc)
    ticket_id = _create_ticket_id(created_at)
    storage_path = _resolve_storage_path()
    storage_path.mkdir(parents=True, exist_ok=True)

    file_path = storage_path / f"{ticket_id}.md"
    artifact.metadata["ticket_id"] = ticket_id
    artifact.metadata["storage_path"] = str(file_path)
    artifact.metadata["created_utc"] = created_at.isoformat()

    file_path.write_text(_build_markdown(artifact, ticket_id, created_at), encoding="utf-8")


def _create_ticket_id(created_at: datetime) -> str:
    global _sequence
    with _sequence_lock:
        _sequence = (_sequence + 1) % 1000
        sequence = _sequence

    return f"SUP-{created_at:%Y%m%d-%H%M%S-%f}"[:-3] + f"-{sequence:03d}"


def _resolve_storage_path() -> Path:
    return Path(__file__).resolve().parents[1] / "storage"


def _build_markdown(artifact: SimulatedArtifact, ticket_id: str, created_at: datetime) -> str:
    metadata_rows = "\n".join(
        f"| {_escape(key)} | {_escape(value)} |"
        for key, value in sorted(artifact.metadata.items())
    )

    return f"""# {_escape(ticket_id)}

| Field | Value |
| --- | --- |
| Ticket id | {_escape(ticket_id)} |
| Artifact type | {_escape(artifact.artifact_type.value)} |
| Title | {_escape(artifact.display_title)} |
| Created UTC | {_escape(created_at.isoformat())} |

## Metadata

| Key | Value |
| --- | --- |
{metadata_rows}

## Payload

{artifact.payload.strip()}
"""


def _escape(value: str) -> str:
    return value.replace("\r\n", "\n").replace("\n", "<br>").replace("|", "\\|")
