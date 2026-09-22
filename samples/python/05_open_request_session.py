"""Open a consumer request session and save its demo session ID."""

from __future__ import annotations

import argparse
import sys
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
    request_session_id,
    required_data_string,
    resolve_connector_config,
    run_cli,
    save_state,
    set_request_session,
    utc_timestamp,
)


def main() -> int:
    arguments = _parse_arguments()
    try:
        connector_config_path = resolve_connector_config(arguments.config)
        state_path = default_state_path()
        state = load_state(state_path)
        if request_session_id(state):
            print(
                "Warning: replacing existing saved request session. The old "
                "remote request session was not closed by this script.",
                file=sys.stderr,
            )

        cli_command = locate_cli(arguments.cli)
        command = [
            "request",
            "open-session",
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
        set_request_session(state, session_id, utc_timestamp())
        try:
            save_state(state_path, state)
        except StateStoreError as error:
            print(
                "The remote request session opened, but demo state could not be "
                f"saved: {error}",
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
        description="Open an MCEGold consumer request session."
    )
    parser.add_argument(
        "--config",
        type=Path,
        help=(
            "Connector config path "
            "(default: configs/connector.config.development.json)."
        ),
    )
    parser.add_argument("--cli", help="MCEGold CLI executable path or command name.")
    parser.add_argument(
        "--include-raw",
        action="store_true",
        help="Include the original service response in the JSON envelope.",
    )
    return parser.parse_args()


if __name__ == "__main__":
    raise SystemExit(main())
