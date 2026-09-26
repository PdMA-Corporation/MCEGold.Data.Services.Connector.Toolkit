# Configuration

The Toolkit has two public configuration templates:

- CLI and Python package: `configs/connector.config.example.json`
- Windows Console package: `appsettings.example.json`

Copy the matching example file to an ignored development file before running live workflows.

## CLI / Python Configuration

Template:

```text
configs/connector.config.example.json
```

Local development file:

```text
configs/connector.config.development.json
```

Create it from the repository root or extracted Linux package root:

```powershell
copy .\configs\connector.config.example.json .\configs\connector.config.development.json
```

Example shape:

```json
{
  "host": "https://your-server/connector/1.0",
  "authenticationScheme": "BasicApi",
  "apiKey": "",
  "userName": "",
  "password": "",
  "publication": {
    "channel": ""
  },
  "request": {
    "channel": ""
  }
}
```

`publication.channel` and `request.channel` are blank by default. Configure a channel only when your environment requires one.

### CLI Precedence

Values are resolved in this order, highest priority first:

1. `--api-key-file` and `--password-file`.
2. `MCEGOLD_*` environment variables.
3. The file supplied with `--config`.
4. Built-in defaults.

Supported environment variables:

```text
MCEGOLD_HOST
MCEGOLD_AUTH_SCHEME
MCEGOLD_API_KEY
MCEGOLD_USERNAME
MCEGOLD_PASSWORD
```

`authenticationScheme` accepts `BasicApi` or `Basic`. The host must be an absolute HTTPS URI.

`request.payloadProfile` is an advanced optional config default for `request preview`, `request post`, and `request run`. It is intentionally omitted from the public beginner template. Use short-form JSON `payloadProfile` or `--payload-profile` when you need an explicit request payload profile.

`request.readResponse` and `request.removeResponseOnSuccess` are advanced optional settings used only by the atomic `request run` convenience command. They default to `true` when omitted and are intentionally omitted from the public beginner template.

## Console Configuration

Template:

```text
appsettings.example.json
```

Local development file:

```text
appsettings.Development.json
```

Create it in the extracted Windows Console package folder or the Console source folder:

```powershell
Copy-Item appsettings.example.json appsettings.Development.json
```

Example shape:

```json
{
  "host": "https://your-server/connector/1.0",
  "authenticationScheme": "BasicApi",
  "apiKey": "",
  "userName": "",
  "password": "",
  "includeRawResponse": false
}
```

The Console reads `appsettings.Development.json` for interactive mode. It first checks the executable folder and then the current working directory.

Optional Console settings such as request presets, publication channel IDs, request channel IDs, and `consoleOutput.showFullCommandResult` have code defaults and are not required in the beginner template.

## Secrets

Do not commit local development configs, credentials, tokens, raw service responses, secret files, or environment dumps.

For CLI and Python workflows, environment variables or mounted secret files are preferred. Command-line API-key/password values are deliberately unsupported because process listings and shell history can expose them.

`config show` requires `--redact` and has no unredacted mode. API key, username, and password values are never returned. `config validate` returns only configured/not-configured booleans.

## Raw Output

`includeRawResponse`, `--include-raw`, and `output.includeRaw` style options are for troubleshooting. Raw output may contain service-specific or proprietary data and should be handled as sensitive diagnostic material.
