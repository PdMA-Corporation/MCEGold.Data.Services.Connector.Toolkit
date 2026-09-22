# Calling the MCEGold Connector CLI

## Purpose

`MCEGold.Data.Services.Connector.Cli.exe` is the Toolkit's stable scripting interface. PowerShell, Python, and other automation should run it as a subprocess and parse its JSON envelope.

Do not reimplement connector logic, authentication, HTTP calls, session internals, or BOD mapping in scripts.

## Prerequisites

- A built or deployed `MCEGold.Data.Services.Connector.Cli.exe`.
- An edited `configs/connector.config.development.json`.
- Credentials supplied through approved environment variables, secret files, or local configuration.

Python samples locate the CLI automatically. Direct callers can invoke the executable or use `dotnet samples/csharp/MCEGold.Data.Services.Connector.Cli/bin/Debug/net8.0/MCEGold.Data.Services.Connector.Cli.dll` after a source build.

## JSON Envelope

Success:

```json
{"schemaVersion":"1.0","success":true,"command":"publication.open-subscription","timestampUtc":"2026-06-19T00:00:00Z","data":{"sessionId":"..."},"fault":null,"raw":null}
```

Failure:

```json
{"schemaVersion":"1.0","success":false,"command":"publication.read","timestampUtc":"2026-06-19T00:00:00Z","data":null,"fault":{"category":"remote","code":"CommandFailed","message":"Not Found","statusCode":404,"details":[]},"raw":null}
```

Always pass `--output json`. Check both the process exit code and `success`; read `data` on success and `fault` on failure. `raw` is null unless advanced troubleshooting is enabled with `--include-raw`.

The CLI preserves fault and HTTP status information. Retry, backoff, polling, cancellation, and timeout policies are determined by the calling application. For publication and response reads, a caller may wait and issue another read according to its own polling policy.

Request post payloads accept optional `payloadProfile` values of `Full` and `Minimal`. Omitted values behave as `Full` and are not serialized. Precedence is CLI `--payload-profile`, then JSON `payloadProfile`, then optional config `request.payloadProfile`, then connector default behavior.

The short-form payload is the authoritative source of the request type and mirrors how long-form CCOM/OIIE BODs determine the request interface from the request document itself. Include `requestType` in the payload, such as `"requestType": "GetSegments"`.

## Publication Consumer

```powershell
MCEGold.Data.Services.Connector.Cli.exe publication open-subscription --config configs/connector.config.development.json --output json
MCEGold.Data.Services.Connector.Cli.exe publication read --config configs/connector.config.development.json --session-id <sessionId> --output json
MCEGold.Data.Services.Connector.Cli.exe publication remove --config configs/connector.config.development.json --session-id <sessionId> --output json
MCEGold.Data.Services.Connector.Cli.exe publication close-subscription --config configs/connector.config.development.json --session-id <sessionId> --output json
```

Open returns `data.sessionId`; read, remove, and close reuse it. Remove acts on the current/read publication in that subscription session and does not require a message ID. Close the session when finished.

## Request Consumer

These commands are implemented:

```powershell
MCEGold.Data.Services.Connector.Cli.exe request open-session --config configs/connector.config.development.json --output json
MCEGold.Data.Services.Connector.Cli.exe request post --config configs/connector.config.development.json --session-id <sessionId> --input <payload.json> --payload-profile Minimal --output json
MCEGold.Data.Services.Connector.Cli.exe request read-response --config configs/connector.config.development.json --session-id <sessionId> --request-id <requestMessageId> --output json
MCEGold.Data.Services.Connector.Cli.exe request remove-response --config configs/connector.config.development.json --session-id <sessionId> --request-id <requestMessageId> --output json
MCEGold.Data.Services.Connector.Cli.exe request close-session --config configs/connector.config.development.json --session-id <sessionId> --output json
```

Open returns `data.sessionId`; post returns `data.requestMessageId`. Read-response and remove-response use that original request message ID. RemoveResponse follows actual ISBM behavior: the server returns success even if no response messages remain in the session queue associated with the request. Read-response is optional before remove-response. Close the session when finished.

## PowerShell

```powershell
$result = & ".\MCEGold.Data.Services.Connector.Cli.exe" publication open-subscription `
    --config ".\configs\connector.config.development.json" `
    --output json | ConvertFrom-Json

if ($LASTEXITCODE -ne 0 -or -not $result.success) {
    $result.fault | ConvertTo-Json -Depth 10
    exit 1
}

$sessionId = $result.data.sessionId
```

## Python

```python
import json
import subprocess

completed = subprocess.run(
    [
        r".\MCEGold.Data.Services.Connector.Cli.exe",
        "publication", "open-subscription",
        "--config", r".\configs\connector.config.development.json",
        "--output", "json",
    ],
    shell=False,
    capture_output=True,
    text=True,
    encoding="utf-8",
    check=False,
)
result = json.loads(completed.stdout)
if completed.returncode != 0 or not result["success"]:
    raise RuntimeError(result["fault"])

session_id = result["data"]["sessionId"]
```

## Raw Responses

Raw capture is disabled by default. Add `--include-raw` to request the original service or transport response when available. Raw content may be large, proprietary, or sensitive; do not persist or log it by default.

## State And Security

Applications must retain session IDs in their own workflow state. `samples/python/sample_state.local.json` is only a demo convenience. Replacing or deleting local state does not close a remote session; always close remote sessions when possible.

Do not store credentials or raw responses in session state. Prefer environment variables or approved secret storage, and never commit a development configuration containing real secrets.
