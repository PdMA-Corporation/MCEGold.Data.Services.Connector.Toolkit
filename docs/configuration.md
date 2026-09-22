# CLI Configuration

Copy the committed example to the ignored development config:

```powershell
copy .\configs\connector.config.example.json .\configs\connector.config.development.json
```

Edit `configs/connector.config.development.json` before running network commands. The example contains placeholders and must not be used for a connection.

```json
{
  "host": "https://your-server/connector/1.0",
  "authenticationScheme": "BasicApi",
  "apiKey": "",
  "userName": "",
  "password": "",
  "publication": {
    "channel": "/YourOrganization/Publication",
    "payloadProfile": "Full"
  },
  "request": {
    "channel": "/YourOrganization/Request",
    "readResponse": true,
    "removeResponseOnSuccess": true
  }
}
```

## Precedence

Values are resolved in this order, highest priority first:

1. `--api-key-file` and `--password-file`.
2. `MCEGOLD_*` environment variables.
3. The file supplied with `--config`.
4. Safe built-in defaults.

Supported environment variables:

```text
MCEGOLD_HOST
MCEGOLD_AUTH_SCHEME
MCEGOLD_API_KEY
MCEGOLD_USERNAME
MCEGOLD_PASSWORD
```

`authenticationScheme` accepts `BasicApi` or `Basic`. The host must be an absolute HTTPS URI.

`request.payloadProfile` is optional and accepts `Full` or `Minimal`. If it is omitted, no request payload profile is injected; the connector behaves as `Full` by default without serializing `payloadProfile`. Request payload profile precedence is CLI `--payload-profile`, then short-form JSON `payloadProfile`, then `request.payloadProfile`, then connector default behavior. The current RapidRedPanda subscription adapter call does not expose a schema-safe subscription `userArea`, so the Toolkit validates and carries `publication.payloadProfile` but does not yet serialize it into the open-subscription transport request.

The Python trial uses `configs/connector.config.development.json`, created from `configs/connector.config.example.json`. It includes predefined publication and request channel labels and intentionally omits topic settings. The current MCEGold Connector does not require users to configure topics. Generic OIIE server support and dynamic topic discovery are future possibilities, not current Toolkit behavior.

## Secrets

Environment variables or mounted secret files are preferred. Command-line API-key/password values are deliberately unsupported because process listings and shell history can expose them.

`config show` requires `--redact` and has no unredacted mode. API key, username, and password values are never returned. `config validate` returns only configured/not-configured booleans.

Do not copy `connector.config.development.json`, `appsettings.Development.json`, secret files, or environment dumps into release output.

## Request behavior

- `request.readResponse`: when true, `request run` performs one connector read after posting.
- `request.removeResponseOnSuccess`: when true, a successfully read response is removed before cleanup.
- `request.payloadProfile`: optional default for `request run`, staged `request post`, and `request preview` when `--config` is supplied. Use `Minimal` only when the server and workflow expect reduced payloads.

The current connector read call controls server-side waiting behavior. Configurable polling and timeout policy are deferred until the connector exposes cancellation/timeout controls.

## Output behavior

`output.format` accepts `json`, `json-pretty`, `text`, or `payload`. A command-line `--output` value overrides it.

`output.includeRaw` adds the underlying connector response when available. Raw output may contain service-specific or proprietary data and should be handled as sensitive diagnostic material.
