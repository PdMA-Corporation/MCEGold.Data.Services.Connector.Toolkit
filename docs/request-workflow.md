# Request Workflow

The request workflow is a staged asynchronous lifecycle:

```text
Open Request Session
-> Post Request
-> Read Response
-> Remove Response
-> Close Request Session
```

## Identifiers

- `sessionId` identifies the request session returned by Open Request Session.
- `requestMessageId` identifies the posted request returned by Post Request.
- `responseMessageId` may be returned by Read Response when a response is available.

Read Response and Remove Response use the original request message ID.

## Python Flow

The numbered Python samples mirror the staged lifecycle:

```text
05_open_request_session.py
06_post_request.py
07_read_response.py
08_remove_response.py
09_close_request_session.py
```

`06_post_request.py` defaults to `payloads/requests/get-sites.example.json`.

To use another payload file:

```powershell
python .\samples\python\06_post_request.py --input .\payloads\requests\get-measurements.example.json
```

To pipe JSON through stdin:

```bash
printf '{"requestType":"GetSites"}\n' | python3 samples/python/06_post_request.py --input -
```

## CLI Flow

The same staged flow is available directly through the CLI:

```powershell
MCEGold.Data.Services.Connector.Cli.exe request open-session --config configs/connector.config.development.json
MCEGold.Data.Services.Connector.Cli.exe request post --config configs/connector.config.development.json --session-id <session-id> --input payloads/requests/get-sites.example.json
MCEGold.Data.Services.Connector.Cli.exe request read-response --config configs/connector.config.development.json --session-id <session-id> --request-id <request-message-id>
MCEGold.Data.Services.Connector.Cli.exe request remove-response --config configs/connector.config.development.json --session-id <session-id> --request-id <request-message-id>
MCEGold.Data.Services.Connector.Cli.exe request close-session --config configs/connector.config.development.json --session-id <session-id>
```

## Atomic `request run`

`request run` is a convenience workflow that opens a session, posts, optionally reads/removes, and closes the session in one command.

Use staged commands or the numbered Python samples when your application needs to own session state explicitly.

## Request Payloads

Request inputs are short-form JSON documents. They must include `requestType`. Optional fields include `payloadProfile`, limits, and type-specific filters.

See [Supported Request Types](request-types.md).
