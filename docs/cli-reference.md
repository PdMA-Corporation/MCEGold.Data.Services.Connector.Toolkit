# MCEGold Connector CLI Reference

The Windows executable name is `MCEGold.Data.Services.Connector.Cli.exe`. Commands and options use lowercase kebab-case. JSON is the default output format.

Top-level commands are `version`, `run`, `config`, `request`, and `publication`. Use [Calling the Connector CLI](calling-mcegold-cli.md) for subprocess integration and the [CLI JSON Contract](json-contract.md) for envelope and exit-code details.

## Global output option

```text
--output json|json-pretty|text|payload
```

- `json`: compact complete envelope; default.
- `json-pretty`: indented complete envelope.
- `text`: one-line human summary.
- `payload`: payload/BOD only when the successful command has one; otherwise falls back to JSON.

Diagnostics go to stderr. JSON stdout never contains banners, prompts, or progress messages.

## Connector response option

```text
--include-raw
```

All publication and request commands that execute connector operations accept `--include-raw`. Without it, the envelope always contains `"raw": null`. With it, `raw` contains the original service response exposed by the connector when one is available. The envelope shape does not change.

For staged commands, `raw` is the parsed `ISBMHTTPResponse` from that operation. Successful atomic workflows may perform several transport calls, so their `raw` value is an array of step/response objects. Raw capture is intended for troubleshooting, is disabled by default, and may expose service-specific or proprietary content.

All failures use the single normalized top-level `fault` object with `category`, `code`, `message`, `statusCode`, and `details`. The envelope never emits top-level `error` or `transportFault` nodes.

The CLI preserves fault and HTTP status information. Retry, backoff, polling, cancellation, and timeout policies are determined by the calling application. For publication and response reads, a caller may wait and issue another read according to its own polling policy.

## Request payload profile option

```text
--payload-profile Full|Minimal
```

`request preview`, `request run`, and staged `request post` accept `--payload-profile`. Values are case-insensitive and are normalized to `Full` or `Minimal` in generated BOD JSON.

Precedence is:

1. `--payload-profile`
2. Short-form JSON `payloadProfile`
3. Optional `request.payloadProfile` in the config supplied with `--config`
4. Connector default behavior, which is `Full`

When `payloadProfile` is omitted at every level, the connector behaves as `Full` and does not serialize `applicationArea.userArea.mcegold.payloadProfile`. Explicit `Full` and explicit `Minimal` are serialized.

## `MCEGold.Data.Services.Connector.Cli.exe version`

Returns the CLI assembly version and loaded connector assembly version.

```powershell
MCEGold.Data.Services.Connector.Cli.exe version
```

## `MCEGold.Data.Services.Connector.Cli.exe run`

Runs one operation-specific typed JSON command through the connector command runner. This advanced path accepts Consumer Request and Consumer Publication command shapes from a file or stdin:

```powershell
MCEGold.Data.Services.Connector.Cli.exe run --input samples/json/request/open-request-session-input.example.json
Get-Content .\command.json | MCEGold.Data.Services.Connector.Cli.exe run --input -
```

```text
--input <path|->            Required
```

See the examples under `samples/json/`, schemas under `docs/json-schema/`, and [Advanced CLI JSON Shapes](connector-cli-json-shapes.md) for detailed mappings. Most automation should use the named `request` and `publication` commands below.

## `MCEGold.Data.Services.Connector.Cli.exe config validate`

Loads a configuration file, applies `MCEGOLD_*` environment overrides, and validates required non-secret settings. It reports whether secrets are configured without printing them.

The Toolkit sample configuration keeps publication and request channels blank by default. `request.payloadProfile` is optional and accepts `Full` or `Minimal`; omit it to use connector default `Full` behavior without serializing `payloadProfile`.

```powershell
MCEGold.Data.Services.Connector.Cli.exe config validate --config configs/connector.config.development.json
```

## `MCEGold.Data.Services.Connector.Cli.exe config show`

Shows effective configuration. The `--redact` safety switch is required and API keys, usernames, and passwords are always replaced with `***REDACTED***` when configured.

```powershell
MCEGold.Data.Services.Connector.Cli.exe config show --config configs/connector.config.development.json --redact
```

Optional secret-file overrides:

```text
--api-key-file <path>
--password-file <path>
```

There is intentionally no unredacted mode.

## `MCEGold.Data.Services.Connector.Cli.exe request preview`

Validates short-form request JSON and generates the outgoing OIIE/CCOM BOD without opening a network session.

```powershell
MCEGold.Data.Services.Connector.Cli.exe request preview `
  --input payloads/requests/get-sites.example.json
```

Optional config defaults are also available for preview:

```powershell
MCEGold.Data.Services.Connector.Cli.exe request preview `
  --input payloads/requests/get-sites.example.json `
  --config configs/connector.config.development.json `
  --payload-profile Minimal
```

Supported request types:

- `get-sites`
- `get-segments`
- `get-assets`
- `get-measurement-locations`
- `get-measurements`
- `get-assessments`
- `get-asset-segment-events`

`--input -` reads JSON from stdin. The short-form payload is the authoritative source of the request type and mirrors how long-form CCOM/OIIE BODs determine the request interface from the request document itself. The input document must contain a PascalCase connector `requestType`, such as `GetSites`. All supported request types accept optional root-level `payloadProfile` values of `Full` or `Minimal`; omitted values behave as `Full` without serializing the field.

Generated previews use `mcegold-cli-preview` for `applicationArea.sender.logicalID`, because a real session ID does not exist. Creation time and BOD ID remain generated values.

## `MCEGold.Data.Services.Connector.Cli.exe request run`

Runs one atomic request workflow:

1. Validate config and input.
2. Open a request session.
3. Post the selected request.
4. Read a response when `request.readResponse` is true.
5. Remove the response when `request.removeResponseOnSuccess` is true.
6. Always attempt to close an opened request session.

```powershell
MCEGold.Data.Services.Connector.Cli.exe request run `
  --input .\payloads\requests\get-measurements.example.json `
  --config .\configs\connector.config.development.json
```

Options:

```text
--input <path|->            Required
--config <path>             Required
--api-key-file <path>       Optional secret override
--password-file <path>      Optional secret override
--include-raw               Include raw connector responses when available
--payload-profile <value>   Optional Full or Minimal override
```

If a workflow step fails, its fault remains primary. A later close failure is attached under `fault.details` with field `cleanup`.

## `MCEGold.Data.Services.Connector.Cli.exe publication receive`

Opens a subscription, reads one publication, optionally removes it, and closes the subscription.

```powershell
MCEGold.Data.Services.Connector.Cli.exe publication receive --config configs/connector.config.development.json --once --remove
```

Options:

```text
--config <path>             Required
--once                      Required in v1
--remove                    Remove after successful read
--api-key-file <path>       Optional secret override
--password-file <path>      Optional secret override
--include-raw               Include raw connector responses when available
```

## Staged publication lifecycle

```powershell
MCEGold.Data.Services.Connector.Cli.exe publication open-subscription --config configs/connector.config.development.json
MCEGold.Data.Services.Connector.Cli.exe publication read --config configs/connector.config.development.json --session-id <session-id>
MCEGold.Data.Services.Connector.Cli.exe publication remove --config configs/connector.config.development.json --session-id <session-id>
MCEGold.Data.Services.Connector.Cli.exe publication close-subscription --config configs/connector.config.development.json --session-id <session-id>
```

`open-subscription` returns `data.sessionId`. Pass that value to every later command. `read` returns `data.messageId` and `data.payload`. Publication removal acts on the current item in the subscription session; it does not accept a message ID.

Each command is a separate process, so `--config` and the normal secret environment/file sources are required on every call. The connector restores the explicit session context before executing the requested ISBM operation.

## Staged request lifecycle

```powershell
MCEGold.Data.Services.Connector.Cli.exe request open-session --config configs/connector.config.development.json

MCEGold.Data.Services.Connector.Cli.exe request post `
  --config configs/connector.config.development.json `
  --session-id <session-id> `
  --input payloads/requests/get-sites.example.json `
  --payload-profile Minimal

MCEGold.Data.Services.Connector.Cli.exe request read-response `
  --config configs/connector.config.development.json `
  --session-id <session-id> `
  --request-id <request-message-id>

MCEGold.Data.Services.Connector.Cli.exe request remove-response `
  --config configs/connector.config.development.json `
  --session-id <session-id> `
  --request-id <request-message-id>

MCEGold.Data.Services.Connector.Cli.exe request close-session --config configs/connector.config.development.json --session-id <session-id>
```

`open-session` returns `data.sessionId`; `post` returns `data.requestMessageId`; and `read-response` returns both `data.requestMessageId` and `data.responseMessageId`. The existing connector removes a response by its original request message ID, so `remove-response` uses `--request-id` rather than a response ID.

All staged network commands also support `--api-key-file`, `--password-file`, and `--include-raw`. Staged `request post` also supports `--payload-profile`. Atomic `request run` and `publication receive` remain convenience commands but are not substitutes for the explicit lifecycle when callers need to own session state.
