"""Post a request through the saved consumer request session."""

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
    SampleInputError,
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
    resolve_request_payload,
    run_cli,
    save_state,
    set_request_post_metadata,
    utc_timestamp,
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
        use_stdin = arguments.input == Path("-")
        input_value = "-"
        stdin_text = None
        if use_stdin:
            stdin_text = sys.stdin.read()
        else:
            input_value = str(resolve_request_payload(arguments.input))

        cli_command = locate_cli(arguments.cli)
        command = [
            "request",
            "post",
            "--config",
            str(connector_config_path),
            "--session-id",
            session_id,
            "--input",
            input_value,
            "--output",
            "json",
        ]
        if arguments.payload_profile:
            command.extend(["--payload-profile", arguments.payload_profile])
        append_include_raw(command, arguments.include_raw)
        result = run_cli(
            cli_command,
            command,
            DEFAULT_TIMEOUT_SECONDS,
            stdin_text=stdin_text,
        )
        print_full_envelope(result.envelope)
        if not result.envelope["success"]:
            return failure_exit_code(result)

        message_id = required_data_string(result.envelope, "requestMessageId")
        request_type = required_data_string(result.envelope, "requestType")
        set_request_post_metadata(
            state,
            request_type,
            message_id,
            utc_timestamp(),
        )
        try:
            save_state(state_path, state)
        except StateStoreError as error:
            print(
                "The request was posted, but demo metadata could not be saved: "
                f"{error}",
                file=sys.stderr,
            )
            print(f"Request message ID: {message_id}", file=sys.stderr)
            return 1
        return 0
    except CommandExecutionError as error:
        print(f"Sample error: {error}", file=sys.stderr)
        return error.exit_code
    except (
        SampleConfigError,
        SampleInputError,
        CliLocationError,
        EnvelopeError,
        StateStoreError,
    ) as error:
        print(f"Sample error: {error}", file=sys.stderr)
        return 1


def _parse_arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Post a request through the saved consumer request session."
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
        "--input",
        type=Path,
        help=(
            "Request payload path, or - to read JSON from stdin "
            "(default: payloads/requests/get-sites.example.json)."
        ),
    )
    parser.add_argument(
        "--include-raw",
        action="store_true",
        help="Include the original service response in the JSON envelope.",
    )
    parser.add_argument(
        "--payload-profile",
        help=(
            "Request payload profile, Full or Minimal. Overrides the JSON "
            "payload and connector config default."
        ),
    )
    return parser.parse_args()


if __name__ == "__main__":
    raise SystemExit(main())
