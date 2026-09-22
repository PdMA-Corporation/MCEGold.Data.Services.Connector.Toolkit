"""Remove the response associated with the saved request message."""

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
    clear_request_message_metadata,
    default_state_path,
    failure_exit_code,
    load_state,
    locate_cli,
    print_full_envelope,
    request_message_id,
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
        message_id = request_message_id(state)
        if not message_id:
            raise StateStoreError(
                "No request message ID is saved. Run 06_post_request.py first."
            )

        cli_command = locate_cli(arguments.cli)
        command = [
            "request",
            "remove-response",
            "--config",
            str(connector_config_path),
            "--session-id",
            session_id,
            "--request-id",
            message_id,
            "--output",
            "json",
        ]
        append_include_raw(command, arguments.include_raw)
        result = run_cli(cli_command, command, DEFAULT_TIMEOUT_SECONDS)
        print_full_envelope(result.envelope)
        if not result.envelope["success"]:
            return failure_exit_code(result)

        clear_request_message_metadata(state)
        try:
            save_state(state_path, state)
        except StateStoreError as error:
            print(
                "The response was removed, but local request metadata could not be "
                f"cleared: {error}",
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
        description="Remove the response for the saved request message."
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
