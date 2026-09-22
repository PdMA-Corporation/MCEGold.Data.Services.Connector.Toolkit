"""Remove the current publication from the saved consumer subscription."""

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
    clear_publication_read_metadata,
    default_state_path,
    failure_exit_code,
    load_state,
    locate_cli,
    print_full_envelope,
    publication_last_message_id,
    publication_session_id,
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
        session_id = publication_session_id(state)
        if not session_id:
            raise StateStoreError(
                "No publication session ID is saved. Run "
                "01_open_subscription.py first."
            )

        message_id = publication_last_message_id(state)
        if not message_id and not arguments.force:
            raise StateStoreError(
                "No publication read metadata is saved. Run "
                "02_read_publication.py first, or use --force to remove the "
                "current publication without local read context."
            )
        if not message_id:
            print(
                "Warning: removing the current publication without saved read "
                "metadata because --force was supplied.",
                file=sys.stderr,
            )

        cli_command = locate_cli(arguments.cli)
        command = [
            "publication",
            "remove",
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

        clear_publication_read_metadata(state)
        try:
            save_state(state_path, state)
        except StateStoreError as error:
            print(
                "The publication was removed, but local read metadata could not "
                f"be cleared: {error}",
                file=sys.stderr,
            )
            return 1

        return 0
    except CommandExecutionError as error:
        print(f"Sample error: {error}", file=sys.stderr)
        print("Demo state was preserved.", file=sys.stderr)
        return error.exit_code
    except (SampleConfigError, CliLocationError, EnvelopeError, StateStoreError) as error:
        print(f"Sample error: {error}", file=sys.stderr)
        return 1


def _parse_arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Remove the current publication from the saved subscription."
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
    parser.add_argument(
        "--force",
        action="store_true",
        help="Remove without saved read metadata.",
    )
    return parser.parse_args()


if __name__ == "__main__":
    raise SystemExit(main())
