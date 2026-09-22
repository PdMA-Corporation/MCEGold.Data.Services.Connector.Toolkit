"""Close the saved consumer request session."""

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
    clear_request_state,
    default_state_path,
    failure_exit_code,
    load_state,
    locate_cli,
    print_full_envelope,
    request_session_id,
    resolve_connector_config,
    run_cli,
    save_state,
)


def main() -> int:
    arguments = _parse_arguments()
    try:
        connector_config_path = resolve_connector_config(arguments.config)
        state_path = default_state_path()
        state = load_state(state_path)
        session_id = request_session_id(state)
        if not session_id:
            raise StateStoreError(
                "No request session ID is saved. Run 05_open_request_session.py first."
            )

        cli_command = locate_cli(arguments.cli)
        command = [
            "request",
            "close-session",
            "--config",
            str(connector_config_path),
            "--session-id",
            session_id,
            "--output",
            "json",
        ]
        append_include_raw(command, arguments.include_raw)
        result = run_cli(cli_command, command, DEFAULT_TIMEOUT_SECONDS)
        print_full_envelope(result.envelope)
        if not result.envelope["success"]:
            return failure_exit_code(result)

        clear_request_state(state)
        try:
            save_state(state_path, state)
        except StateStoreError as error:
            print(
                "The remote request session closed, but local demo state could not "
                f"be cleared: {error}",
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
        description="Close the saved MCEGold consumer request session."
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
