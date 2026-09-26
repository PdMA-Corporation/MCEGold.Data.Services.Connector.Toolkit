# MCEGold Python Samples

These samples demonstrate the staged Publication / Subscription and Request / Response lifecycles. Each numbered script invokes one explicit Connector CLI operation and reuses identifiers saved by the preceding step.

Python calls the Connector CLI through `subprocess`; connector HTTP, authentication, session, and payload behavior remain in the CLI and connector package.

Start with the [Linux / Python package guide](../../docs/linux-python.md) or [Build from Source](../../docs/source-development.md). For configuration and payload details, see [Configuration](../../docs/configuration.md) and [Request Types](../../docs/request-types.md).

## Setup

Create the local CLI/Python configuration from the repository or extracted package root:

```powershell
copy .\configs\connector.config.example.json .\configs\connector.config.development.json
```

Edit `configs/connector.config.development.json` and enter the connection settings. Never commit real credentials, tokens, or production endpoints.

The release package includes a self-contained CLI and does not require a system .NET runtime. Source users must build the CLI first.

The samples find the CLI in the extracted package and in common source-build output folders. You can override discovery with `--cli <path>` or by setting `MCEGOLD_CLI` to the CLI executable or DLL.

## Publication / Subscription

Run the scripts in order:

```bash
python3 samples/python/01_open_subscription.py
python3 samples/python/02_read_publication.py
python3 samples/python/03_remove_publication.py
python3 samples/python/04_close_subscription.py
```

The scripts explicitly open, read, remove, and close the subscription. See [Publication Workflow](../../docs/publication-workflow.md) for the identifiers and equivalent CLI commands.

## Request / Response

Run the scripts in order:

```bash
python3 samples/python/05_open_request_session.py
python3 samples/python/06_post_request.py
python3 samples/python/07_read_response.py
python3 samples/python/08_remove_response.py
python3 samples/python/09_close_request_session.py
```

The scripts explicitly open, post, read, remove, and close the request session. `07_read_response.py` is optional when the workflow does not need to inspect the response before removal. Settings used by the atomic `request run` command do not control these numbered scripts.

`06_post_request.py` defaults to `payloads/requests/get-sites.example.json`. Select that file explicitly with:

```bash
python3 samples/python/06_post_request.py \
  --input payloads/requests/get-sites.example.json
```

Read request JSON from stdin with:

```bash
printf '{"requestType":"GetSites"}\n' |
python3 samples/python/06_post_request.py --input -
```

Literal inline JSON passed as the `--input` argument is not supported. Use `--input <path>` or pipe JSON with `--input -`. See [Request Workflow](../../docs/request-workflow.md) for the identifiers and equivalent CLI commands.

## Demo State

The samples store non-secret demo identifiers and timestamps in `samples/python/sample_state.local.json`. Deleting it clears only local demo state and does not close remote sessions, so run the close scripts whenever possible.

For direct automation, use [Calling the Connector CLI](../../docs/calling-mcegold-cli.md). For every command and option, use the [CLI Reference](../../docs/cli-reference.md).
