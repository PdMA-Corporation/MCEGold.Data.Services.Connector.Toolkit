"""Standard-library helpers shared by the trial MCEGold Python demos."""

from __future__ import annotations

import json
import os
import shutil
import subprocess
import tempfile
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Sequence


# -----------------------------------------------------------------------------
# Repository paths and connector configuration
# -----------------------------------------------------------------------------

DEFAULT_TIMEOUT_SECONDS = 120


class SampleConfigError(Exception):
    """Raised when the connector configuration cannot be located."""


class SampleInputError(Exception):
    """Raised when a sample payload cannot be located."""


def sample_root() -> Path:
    return Path(__file__).resolve().parent


def toolkit_root() -> Path:
    return sample_root().parents[1]


def default_connector_config_path() -> Path:
    return toolkit_root() / "configs" / "connector.config.development.json"


def example_connector_config_path() -> Path:
    return toolkit_root() / "configs" / "connector.config.example.json"


def default_state_path() -> Path:
    return sample_root() / "sample_state.local.json"


def default_request_payload_path() -> Path:
    return toolkit_root() / "payloads" / "requests" / "get-sites.example.json"


def resolve_connector_config(path: str | Path | None = None) -> Path:
    selected = (
        Path(path).expanduser().resolve()
        if path is not None
        else default_connector_config_path()
    )
    if selected.is_file():
        return selected

    if path is None and example_connector_config_path().is_file():
        raise SampleConfigError(
            "Please copy configs/connector.config.example.json to "
            "configs/connector.config.development.json and edit it before "
            "running the demo."
        )

    raise SampleConfigError(f"Connector configuration was not found: {selected}")


def resolve_request_payload(path: str | Path | None = None) -> Path:
    selected = (
        Path(path).expanduser().resolve()
        if path is not None
        else default_request_payload_path()
    )
    if selected.is_file():
        return selected
    raise SampleInputError(f"Request payload was not found: {selected}")


# -----------------------------------------------------------------------------
# CLI discovery
# -----------------------------------------------------------------------------


class CliLocationError(Exception):
    """Raised when the MCEGold CLI cannot be located."""


def locate_cli(explicit_value: str | Path | None = None) -> list[str]:
    if explicit_value is not None:
        return _command_prefix_for_path(
            Path(explicit_value).expanduser().resolve(), "--cli"
        )

    environment_value = os.environ.get("MCEGOLD_CLI", "").strip()
    if environment_value:
        return _command_prefix_for_path(
            Path(environment_value).expanduser().resolve(), "MCEGOLD_CLI"
        )

    root = toolkit_root()
    checked = [
        root / "MCEGold.Data.Services.Connector.Cli",
        root / "MCEGold.Data.Services.Connector.Cli.exe",
        root
        / "samples"
        / "csharp"
        / "MCEGold.Data.Services.Connector.Cli"
        / "bin"
        / "Debug"
        / "net8.0"
        / "MCEGold.Data.Services.Connector.Cli.dll",
        root
        / "samples"
        / "csharp"
        / "MCEGold.Data.Services.Connector.Cli"
        / "bin"
        / "Release"
        / "net8.0"
        / "MCEGold.Data.Services.Connector.Cli.dll",
        # Legacy pre-reorganization source layout.
        root
        / "src"
        / "MCEGold.Data.Services.Connector.Cli"
        / "bin"
        / "Debug"
        / "net8.0"
        / "MCEGold.Data.Services.Connector.Cli.dll",
    ]
    for candidate in checked:
        if candidate.is_file():
            return _command_prefix_for_path(candidate, "repository discovery")

    raise CliLocationError(
        "Could not locate MCEGold.Data.Services.Connector.Cli. Pass --cli, set "
        "MCEGOLD_CLI, deploy the CLI at the repository root, or build the Debug "
        "net8.0 CLI. Checked:\n  - "
        + "\n  - ".join(str(path) for path in checked)
    )


def _command_prefix_for_path(path: Path, source: str) -> list[str]:
    if not path.is_file():
        raise CliLocationError(
            f"{source} does not point to a CLI file: {path}\nChecked:\n  - {path}"
        )
    if path.suffix.lower() == ".dll":
        if shutil.which("dotnet") is None:
            raise CliLocationError(
                f"{source} points to a DLL, but dotnet was not found on PATH: {path}"
            )
        return ["dotnet", str(path)]
    return [str(path)]


# -----------------------------------------------------------------------------
# JSON envelope validation and display
# -----------------------------------------------------------------------------


class EnvelopeError(Exception):
    """Raised when CLI output does not match the expected envelope shape."""


def validate_envelope(value: Any) -> dict[str, Any]:
    if not isinstance(value, dict):
        raise EnvelopeError("The MCEGold CLI response must be one JSON object.")

    required = {
        "schemaVersion",
        "success",
        "command",
        "timestampUtc",
        "data",
        "fault",
        "raw",
    }
    missing = sorted(required - set(value))
    if missing:
        raise EnvelopeError(
            f"The MCEGold CLI response is missing field(s): {', '.join(missing)}"
        )
    if not isinstance(value["schemaVersion"], str):
        raise EnvelopeError("The response schemaVersion must be a string.")
    if not isinstance(value["success"], bool):
        raise EnvelopeError("The response success field must be true or false.")
    if not isinstance(value["command"], str):
        raise EnvelopeError("The response command field must be a string.")
    if not isinstance(value["timestampUtc"], str):
        raise EnvelopeError("The response timestampUtc field must be a string.")
    forbidden = sorted({"error", "transportFault"} & set(value))
    if forbidden:
        raise EnvelopeError(
            f"The response contains obsolete field(s): {', '.join(forbidden)}"
        )
    if value["success"] and not isinstance(value["data"], dict):
        raise EnvelopeError("A successful response must contain a data object.")
    if value["success"] and value["fault"] is not None:
        raise EnvelopeError("A successful response must contain fault: null.")
    if not value["success"] and value["data"] is not None:
        raise EnvelopeError("A failed response must contain data: null.")
    if not value["success"] and not isinstance(value["fault"], dict):
        raise EnvelopeError("A failed response must contain a fault object.")
    if not value["success"]:
        fault = value["fault"]
        required_fault = {
            "category",
            "code",
            "message",
            "statusCode",
            "details",
        }
        missing_fault = sorted(required_fault - set(fault))
        if missing_fault:
            raise EnvelopeError(
                "The response fault is missing field(s): "
                + ", ".join(missing_fault)
            )
        if not all(isinstance(fault[name], str) for name in ("category", "code", "message")):
            raise EnvelopeError("Fault category, code, and message must be strings.")
        if fault["statusCode"] is not None and not isinstance(fault["statusCode"], int):
            raise EnvelopeError("Fault statusCode must be an integer or null.")
        if not isinstance(fault["details"], list):
            raise EnvelopeError("Fault details must be an array.")
    return value


def required_data_string(envelope: dict[str, Any], name: str) -> str:
    data = envelope.get("data")
    value = data.get(name) if isinstance(data, dict) else None
    if not isinstance(value, str) or not value.strip():
        raise EnvelopeError(
            f"The successful CLI response did not contain data.{name}."
        )
    return value


def print_full_envelope(envelope: dict[str, Any]) -> None:
    print(json.dumps(envelope, indent=2, ensure_ascii=False))


# -----------------------------------------------------------------------------
# CLI process execution
# -----------------------------------------------------------------------------


class CommandExecutionError(Exception):
    """Raised when the CLI process cannot provide a valid result envelope."""

    def __init__(self, message: str, exit_code: int = 1) -> None:
        super().__init__(message)
        self.exit_code = exit_code


@dataclass(frozen=True)
class CommandResult:
    exit_code: int
    envelope: dict[str, Any]
    stderr: str


def run_cli(
    command_prefix: Sequence[str],
    arguments: Sequence[str],
    timeout_seconds: int,
    stdin_text: str | None = None,
) -> CommandResult:
    command = [*command_prefix, *arguments]
    try:
        completed = subprocess.run(
            command,
            shell=False,
            capture_output=True,
            text=True,
            encoding="utf-8",
            input=stdin_text,
            check=False,
            timeout=timeout_seconds,
        )
    except subprocess.TimeoutExpired as error:
        raise CommandExecutionError(
            f"The MCEGold CLI timed out after {timeout_seconds} seconds."
        ) from error
    except FileNotFoundError as error:
        raise CommandExecutionError(
            f"The MCEGold CLI command could not be started: {command_prefix[0]}"
        ) from error
    except PermissionError as error:
        raise CommandExecutionError(
            f"The MCEGold CLI command is not executable: {command_prefix[0]}"
        ) from error
    except OSError as error:
        raise CommandExecutionError(f"Could not start the MCEGold CLI: {error}") from error

    stdout = completed.stdout.strip()
    if not stdout:
        raise CommandExecutionError(
            f"The MCEGold CLI returned exit code {completed.returncode} without JSON output.",
            _safe_exit_code(completed.returncode),
        )

    try:
        envelope = json.loads(stdout)
    except json.JSONDecodeError as error:
        raise CommandExecutionError(
            "The MCEGold CLI returned malformed JSON on stdout "
            f"(exit code {completed.returncode}): {error.msg}",
            _safe_exit_code(completed.returncode),
        ) from error

    validate_envelope(envelope)
    success = envelope["success"]
    if completed.returncode == 0 and not success:
        raise CommandExecutionError(
            "The CLI returned exit code 0 with a failed JSON envelope."
        )
    if completed.returncode != 0 and success:
        raise CommandExecutionError(
            "The CLI returned a nonzero exit code with a successful JSON envelope.",
            _safe_exit_code(completed.returncode),
        )

    return CommandResult(
        exit_code=completed.returncode,
        envelope=envelope,
        stderr=completed.stderr.strip(),
    )


def append_include_raw(arguments: list[str], include_raw: bool) -> None:
    """Request transport response capture without changing normal output."""
    if include_raw:
        arguments.append("--include-raw")


def failure_exit_code(result: CommandResult) -> int:
    return _safe_exit_code(result.exit_code)


def _safe_exit_code(value: int) -> int:
    return value if 1 <= value <= 255 else 1


# -----------------------------------------------------------------------------
# Demo-only session state
# -----------------------------------------------------------------------------

SCHEMA_VERSION = "1.0"
PUBLICATION_FIELDS = {
    "sessionId",
    "openedAtUtc",
    "lastMessageId",
    "lastReadAtUtc",
}
REQUEST_FIELDS = {
    "sessionId",
    "openedAtUtc",
    "requestType",
    "requestMessageId",
    "postedAtUtc",
    "responseMessageId",
    "responseReadAtUtc",
}


class StateStoreError(Exception):
    """Raised when demo state cannot be read, validated, or written."""


def empty_state() -> dict[str, Any]:
    return {"schemaVersion": SCHEMA_VERSION, "publication": None, "request": None}


def load_state(path: Path) -> dict[str, Any]:
    if not path.exists():
        return empty_state()

    try:
        value = json.loads(path.read_text(encoding="utf-8"))
    except OSError as error:
        raise StateStoreError(f"Could not read demo state '{path}': {error}") from error
    except json.JSONDecodeError as error:
        raise StateStoreError(
            f"Demo state '{path}' is not valid JSON: {error.msg}"
        ) from error

    return _validate_state(value)


def save_state(path: Path, state: dict[str, Any]) -> None:
    normalized = _validate_state(state)
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary_path: Path | None = None
    try:
        with tempfile.NamedTemporaryFile(
            mode="w",
            encoding="utf-8",
            dir=path.parent,
            prefix=f".{path.name}.",
            suffix=".tmp",
            delete=False,
        ) as temporary:
            temporary_path = Path(temporary.name)
            json.dump(normalized, temporary, indent=2, ensure_ascii=False)
            temporary.write("\n")
            temporary.flush()
            os.fsync(temporary.fileno())
        try:
            os.chmod(temporary_path, 0o600)
        except OSError:
            pass
        os.replace(temporary_path, path)
    except OSError as error:
        raise StateStoreError(f"Could not write demo state '{path}': {error}") from error
    finally:
        if temporary_path is not None and temporary_path.exists():
            try:
                temporary_path.unlink()
            except OSError:
                pass


def publication_session_id(state: dict[str, Any]) -> str | None:
    publication = state.get("publication")
    if not isinstance(publication, dict):
        return None
    value = publication.get("sessionId")
    return value if isinstance(value, str) and value else None


def publication_last_message_id(state: dict[str, Any]) -> str | None:
    publication = state.get("publication")
    if not isinstance(publication, dict):
        return None
    value = publication.get("lastMessageId")
    return value if isinstance(value, str) and value else None


def set_publication_session(
    state: dict[str, Any], session_id: str, opened_at_utc: str
) -> None:
    """Replace publication state while preserving the independent request state."""
    if not session_id:
        raise StateStoreError("Cannot save an empty publication session ID.")
    state["publication"] = {
        "sessionId": session_id,
        "openedAtUtc": opened_at_utc,
    }


def set_publication_read_metadata(
    state: dict[str, Any], message_id: str, read_at_utc: str
) -> None:
    publication = state.get("publication")
    if not isinstance(publication, dict) or not publication_session_id(state):
        raise StateStoreError(
            "Cannot save publication read metadata without a publication session."
        )
    if not message_id:
        raise StateStoreError("Cannot save an empty publication message ID.")
    publication["lastMessageId"] = message_id
    publication["lastReadAtUtc"] = read_at_utc


def clear_publication_read_metadata(state: dict[str, Any]) -> None:
    publication = state.get("publication")
    if not isinstance(publication, dict) or not publication_session_id(state):
        raise StateStoreError(
            "Cannot clear publication read metadata without a publication session."
        )
    publication.pop("lastMessageId", None)
    publication.pop("lastReadAtUtc", None)


def clear_publication_state(state: dict[str, Any]) -> None:
    state["publication"] = None


def request_session_id(state: dict[str, Any]) -> str | None:
    request = state.get("request")
    if not isinstance(request, dict):
        return None
    value = request.get("sessionId")
    return value if isinstance(value, str) and value else None


def request_message_id(state: dict[str, Any]) -> str | None:
    request = state.get("request")
    if not isinstance(request, dict):
        return None
    value = request.get("requestMessageId")
    return value if isinstance(value, str) and value else None


def response_message_id(state: dict[str, Any]) -> str | None:
    request = state.get("request")
    if not isinstance(request, dict):
        return None
    value = request.get("responseMessageId")
    return value if isinstance(value, str) and value else None


def set_request_session(
    state: dict[str, Any], session_id: str, opened_at_utc: str
) -> None:
    """Replace request state while preserving the independent publication state."""
    if not session_id:
        raise StateStoreError("Cannot save an empty request session ID.")
    state["request"] = {
        "sessionId": session_id,
        "openedAtUtc": opened_at_utc,
    }


def set_request_post_metadata(
    state: dict[str, Any], request_type: str, message_id: str, posted_at_utc: str
) -> None:
    request = state.get("request")
    if not isinstance(request, dict) or not request_session_id(state):
        raise StateStoreError(
            "Cannot save request post metadata without a request session."
        )
    if not request_type or not message_id:
        raise StateStoreError("Request type and message ID must not be empty.")
    request["requestType"] = request_type
    request["requestMessageId"] = message_id
    request["postedAtUtc"] = posted_at_utc
    request.pop("responseMessageId", None)
    request.pop("responseReadAtUtc", None)


def set_response_read_metadata(
    state: dict[str, Any], message_id: str, read_at_utc: str
) -> None:
    request = state.get("request")
    if (
        not isinstance(request, dict)
        or not request_session_id(state)
        or not request_message_id(state)
    ):
        raise StateStoreError(
            "Cannot save response metadata without a posted request."
        )
    if not message_id:
        raise StateStoreError("Cannot save an empty response message ID.")
    request["responseMessageId"] = message_id
    request["responseReadAtUtc"] = read_at_utc


def clear_request_message_metadata(state: dict[str, Any]) -> None:
    request = state.get("request")
    if not isinstance(request, dict) or not request_session_id(state):
        raise StateStoreError(
            "Cannot clear request message metadata without a request session."
        )
    request.pop("requestMessageId", None)
    request.pop("postedAtUtc", None)
    request.pop("responseMessageId", None)
    request.pop("responseReadAtUtc", None)


def clear_request_state(state: dict[str, Any]) -> None:
    state["request"] = None


def utc_timestamp() -> str:
    return datetime.now(timezone.utc).isoformat(timespec="seconds").replace(
        "+00:00", "Z"
    )


def _validate_state(value: Any) -> dict[str, Any]:
    if not isinstance(value, dict):
        raise StateStoreError("Demo state must contain one JSON object.")

    unknown = sorted(set(value) - {"schemaVersion", "publication", "request"})
    if unknown:
        raise StateStoreError(f"Unknown demo state field(s): {', '.join(unknown)}")
    if value.get("schemaVersion") != SCHEMA_VERSION:
        raise StateStoreError(
            f"Unsupported demo state schemaVersion: {value.get('schemaVersion')!r}"
        )

    publication = _validate_section(
        value.get("publication"), PUBLICATION_FIELDS, "publication"
    )
    request = _validate_section(value.get("request"), REQUEST_FIELDS, "request")
    return {
        "schemaVersion": SCHEMA_VERSION,
        "publication": publication,
        "request": request,
    }


def _validate_section(
    value: Any, allowed: set[str], name: str
) -> dict[str, Any] | None:
    if value is None:
        return None
    if not isinstance(value, dict):
        raise StateStoreError(f"Demo state {name} must be an object or null.")
    unknown = sorted(set(value) - allowed)
    if unknown:
        raise StateStoreError(
            f"Demo state {name} contains forbidden/unknown field(s): "
            f"{', '.join(unknown)}"
        )
    for field, field_value in value.items():
        if not isinstance(field_value, str) or not field_value:
            raise StateStoreError(f"Demo state {name}.{field} must be a string.")
    return dict(value)
