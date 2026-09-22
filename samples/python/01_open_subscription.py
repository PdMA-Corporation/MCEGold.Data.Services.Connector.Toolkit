"""Open a consumer publication subscription and save its demo session ID."""

from __future__ import annotations

import argparse
import sys
from datetime import datetime, timezone
from pathlib import Path

from cli_helpers import (
    CliLocationError,
    CommandExecutionError,
    DEFAULT_TIMEOUT_SECONDS,
    EnvelopeError,
    SampleConfigError,
    StateStoreError,
    append_include_raw,
    default_state_path,
    failure_exit_code,
    load_state,
    locate_cli,
    print_full_envelope,
    publication_session_id,
    required_data_string,
    resolve_connector_config,
    run_cli,
    save_state,
    set_publication_session,
)


def main() -> int:
    arguments = _parse_arguments()
    try:
        connector_config_path = resolve_connector_config(arguments.config)
        state_path = default_state_path()
        state = load_state(state_path)
        existing_session = publication_session_id(state)
        if existing_session:
            print(
                "Warning: replacing existing saved publication session. The old "
                "remote session was not closed by this script.",
                file=sys.stderr,
            )

        cli_command = locate_cli(arguments.cli)
        command = [
            "publication",
            "open-subscription",
            "--config",
            str(connector_config_path),
            "--output",
            "json",
        ]
        append_include_raw(command, arguments.include_raw)
        result = run_cli(cli_command, command, DEFAULT_TIMEOUT_SECONDS)
        print_full_envelope(result.envelope)
        if not result.envelope["success"]:
            return failure_exit_code(result)

        session_id = required_data_string(result.envelope, "sessionId")
        opened_at = datetime.now(timezone.utc).isoformat(timespec="seconds").replace(
            "+00:00", "Z"
        )
        set_publication_session(state, session_id, opened_at)
        try:
            save_state(state_path, state)
        except StateStoreError as error:
            print(
                "The remote subscription opened, but demo state could not be saved: "
                f"{error}",
                file=sys.stderr,
            )
            print(
                f"Session ID: {session_id}. Close this session explicitly before retrying.",
                file=sys.stderr,
            )
            return 1

        return 0
    except CommandExecutionError as error:
        print(f"Sample error: {error}", file=sys.stderr)
        return error.exit_code
    except (SampleConfigError, CliLocationError, EnvelopeError, StateStoreError) as error:
        print(f"Sample error: {error}", file=sys.stderr)
        return 1


def _parse_arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Open an MCEGold consumer publication subscription."
    )
    parser.add_argument(
        "--config",
        type=Path,
        help=(
            "Connector config path "
            "(default: configs/connector.config.development.json)."
        ),
    )
    parser.add_argument(
        "--cli",
        help="MCEGold CLI executable path or command name.",
    )
    parser.add_argument(
        "--include-raw",
        action="store_true",
        help="Include the original service response in the JSON envelope.",
    )
    return parser.parse_args()


if __name__ == "__main__":
    raise SystemExit(main())
