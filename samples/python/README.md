# MCEGold Python Samples

These samples demonstrate the staged ISBM consumer publication and consumer request lifecycles. Each Python process invokes one explicit CLI operation and reuses identifiers saved by the preceding step. Atomic receive/run and provider workflows are not included.

Python calls `MCEGold.Data.Services.Connector.Cli.exe` through `subprocess`. It does not implement connector HTTP, authentication, session, BOD, or RapidRedPanda behavior.

See [Calling the MCEGold Connector CLI](../../docs/calling-mcegold-cli.md) for the scripting contract used by these samples.

The trial layout is intentionally flat so new users can see every relevant file at once:

```text
samples/python/
|-- 01_open_subscription.py
|-- 02_read_publication.py
|-- 03_remove_publication.py
|-- 04_close_subscription.py
|-- 05_open_request_session.py
|-- 06_post_request.py
|-- 07_read_response.py
|-- 08_remove_response.py
|-- 09_close_request_session.py
|-- cli_helpers.py
`-- README.md
```

CLI discovery, config paths, JSON handling, subprocess execution, and demo state are consolidated in `cli_helpers.py` so the numbered scripts remain approachable.

## Setup

### 1. Create the connector config

From the repository root:

```powershell
copy .\configs\connector.config.example.json .\configs\connector.config.development.json
```

### 2. Edit the connector config

Edit `.\configs\connector.config.development.json`. This is the only file users normally edit for the basic Python demo.

Set the host and the predefined publication/request channel values. Topic settings are intentionally omitted: the current MCEGold Connector targets predefined publication and request services, so users do not need to select topics for this trial. Request payload profile is optional: use short-form JSON `payloadProfile`, `06_post_request.py --payload-profile`, or config `request.payloadProfile` when you need `Full` or `Minimal` explicitly. If omitted, the connector behaves as `Full` without serializing `payloadProfile`.

The tracked example contains placeholders only. Credentials may be placed in the ignored local file for a local demo, but environment variables are preferred:

```text
MCEGOLD_HOST
MCEGOLD_AUTH_SCHEME
MCEGOLD_API_KEY
MCEGOLD_USERNAME
MCEGOLD_PASSWORD
```

Never commit real credentials, tokens, or production endpoints.

### 3. Run the staged calls

The scripts locate the CLI in this order:

1. Explicit `--cli <path>`.
2. `MCEGOLD_CLI` environment variable.
3. `MCEGold.Data.Services.Connector.Cli` at the repository root.
4. `MCEGold.Data.Services.Connector.Cli.exe` at the repository root.
5. `samples/csharp/MCEGold.Data.Services.Connector.Cli/bin/Debug/net8.0/MCEGold.Data.Services.Connector.Cli.dll` from a source build.
6. `samples/csharp/MCEGold.Data.Services.Connector.Cli/bin/Release/net8.0/MCEGold.Data.Services.Connector.Cli.dll` from a source build.

After creating the connector config, normal users can simply run:

```powershell
python .\samples\python\01_open_subscription.py
python .\samples\python\02_read_publication.py
python .\samples\python\03_remove_publication.py
python .\samples\python\04_close_subscription.py

python .\samples\python\05_open_request_session.py
python .\samples\python\06_post_request.py
python .\samples\python\07_read_response.py
python .\samples\python\08_remove_response.py
python .\samples\python\09_close_request_session.py
```

The source-build DLL fallback runs through `dotnet` and requires a .NET 8 SDK or runtime.

An advanced override is available when the CLI lives elsewhere:

PowerShell example:

```powershell
$env:MCEGOLD_CLI = "C:\path\to\MCEGold.Data.Services.Connector.Cli.exe"
```

Open and save a subscription session:

```powershell
python .\samples\python\01_open_subscription.py
```

Read the next publication for that saved subscription session:

```powershell
python .\samples\python\02_read_publication.py
```

Remove the current publication from the subscription session:

```powershell
python .\samples\python\03_remove_publication.py
```

The connector removes the current publication and does not require a message ID as a command argument. The demo records the ID returned by read as local tutorial context. For safety, remove asks you to run the read script first; `--force` bypasses that local check when the current publication is already known through another workflow.

Close that saved subscription session:

```powershell
python .\samples\python\04_close_subscription.py
```

### Request workflow

Run the request samples in order:

```powershell
python .\samples\python\05_open_request_session.py
python .\samples\python\06_post_request.py
python .\samples\python\07_read_response.py
python .\samples\python\08_remove_response.py
python .\samples\python\09_close_request_session.py
```

Read Response is optional before Remove Response. This shorter workflow is also valid:

```powershell
python .\samples\python\05_open_request_session.py
python .\samples\python\06_post_request.py
python .\samples\python\08_remove_response.py
python .\samples\python\09_close_request_session.py
```

`06_post_request.py` defaults to `payloads/requests/get-sites.example.json`. The short-form payload is the authoritative source of the request type and mirrors how long-form CCOM/OIIE BODs determine the request interface from the request document itself. Override the payload when needed:

```powershell
python .\samples\python\06_post_request.py --input <payload-path>
```

Request payload profile can also be supplied as a CLI override. This wins over JSON and config defaults:

```powershell
python .\samples\python\06_post_request.py --payload-profile Minimal
```

Omitting payload profile keeps default `Full` behavior and does not add `payloadProfile` to the generated BOD.

Post saves the original `requestMessageId`; both read-response and remove-response pass that ID back to the CLI. RemoveResponse follows actual ISBM behavior: the server returns success even if no response messages remain in the session queue associated with the request. The Python samples intentionally mirror that server behavior, so Read Response is optional before Remove Response.

### Available Example Request Payloads

- `payloads/requests/get-sites.example.json`
- `payloads/requests/get-segments.example.json`
- `payloads/requests/get-assets.example.json`
- `payloads/requests/get-measurement-locations.example.json`
- `payloads/requests/get-measurements.example.json`
- `payloads/requests/get-assessments.example.json`
- `payloads/requests/get-asset-segment-events.example.json`

See [Supported Request Types](../../docs/request-types.md) for the accepted short-form fields and descriptions.

Override the normal connector config or CLI when needed:

```text
python .\samples\python\01_open_subscription.py --config <path> --cli <path>
```

Every Python sample prints the complete standardized Connector JSON envelope on success and failure. The former `--show-json` display flag has been removed because full-envelope output is now the only sample output mode.

`--include-raw` asks the CLI to include the original service/transport response in the envelope when available. Raw collection is off by default, so `raw` remains `null` unless this flag is supplied:

```powershell
python .\samples\python\02_read_publication.py
python .\samples\python\02_read_publication.py --include-raw
```

Failures use one normalized top-level `fault` object. There are no separate `error`, ISBM fault, or `transportFault` nodes. Raw responses are troubleshooting data and may contain service-specific or proprietary content; the samples never save them to `sample_state.local.json`.

The default connector config is `configs/connector.config.development.json`. Paths supplied with `--config` are resolved from the current working directory. All scripts otherwise work from any current directory. The tracked example is used only to explain how to create the development config; the scripts never connect with it.

### CLI discovery troubleshooting

If discovery fails, the error lists every repository-relative location that was checked. Build the CLI in Debug mode, copy a deployed `MCEGold.Data.Services.Connector.Cli.exe` to the repository root, or set `MCEGOLD_CLI`/`--cli` to an existing executable or `.dll`. A `.dll` override also requires `dotnet` on `PATH`.

## Demo state

`samples/python/sample_state.local.json` is created and managed automatically. Users should not edit it. It stores only non-secret demo session metadata:

```json
{
  "schemaVersion": "1.0",
  "publication": {
    "sessionId": "subscription-session-id",
    "openedAtUtc": "2026-06-19T00:00:00Z"
  },
  "request": {
    "sessionId": "request-session-id",
    "openedAtUtc": "2026-06-19T00:00:00Z",
    "requestType": "get-sites",
    "requestMessageId": "request-message-id",
    "postedAtUtc": "2026-06-19T00:01:00Z",
    "responseMessageId": "response-message-id",
    "responseReadAtUtc": "2026-06-19T00:02:00Z"
  }
}
```

The repository ignores this file through `*.local.json`. It stores only demo session/message identifiers and their timestamps. It never stores host configuration, credentials, tokens, publication payloads, config contents, or raw responses.

This state file is demo convenience, not production session-state architecture. Real applications should manage session IDs in their own workflow and durable state according to their ownership, concurrency, expiry, recovery, and cleanup requirements.

Rerunning `01_open_subscription.py` opens a new subscription and replaces the saved local publication session and read metadata. It does not retain the old session ID and does not close the previous remote ISBM session.

Run `04_close_subscription.py` first whenever possible. Replacing or deleting `sample_state.local.json` changes only local demo state and does not close a remote subscription. Failed close commands preserve state so the operation can be retried or diagnosed.

Rerunning `05_open_request_session.py` similarly replaces local request state without closing the previous remote request session. Run `09_close_request_session.py` first whenever possible. Publication and request state are maintained independently.
