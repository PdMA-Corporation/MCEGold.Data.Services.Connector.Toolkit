# MCEGold Data Services Connector Toolkit

Public automation tooling, samples, configuration templates, payload templates, and validation utilities for the `MCEGold.Data.Services.Connector` NuGet package.

The Connector package is the runtime library. The Toolkit is the public companion workspace for learning and validating package usage without referencing internal source projects. Active SDK-style samples consume `MCEGold.Data.Services.Connector` version `1.2.0` from NuGet.

## Prerequisites

- .NET 8 SDK for the CLI, developer console, and validation projects.
- Python 3.10 or later for the Python wrapper samples.
- Access to an MCEGold Data Services Connector endpoint only for live request/publication workflows.

Offline request preview does not require credentials or a live service.

## Recommended First Run

Build the automation CLI and run an offline preview:

```powershell
dotnet restore .\samples\csharp\MCEGold.Data.Services.Connector.Cli\MCEGold.Data.Services.Connector.Cli.csproj
dotnet build .\samples\csharp\MCEGold.Data.Services.Connector.Cli\MCEGold.Data.Services.Connector.Cli.csproj --no-restore
dotnet run --project .\samples\csharp\MCEGold.Data.Services.Connector.Cli -- version
dotnet run --project .\samples\csharp\MCEGold.Data.Services.Connector.Cli -- request preview --input .\payloads\requests\get-sites.example.json
```

Use `--input -` to read a request JSON document from stdin.

## Configuration

The canonical public template is `configs/connector.config.example.json`.

For live workflows, copy it to the ignored local development file and fill in local values:

```powershell
copy .\configs\connector.config.example.json .\configs\connector.config.development.json
```

Secrets may also be supplied with environment variables or secret files:

- `MCEGOLD_HOST`
- `MCEGOLD_AUTH_SCHEME`
- `MCEGOLD_API_KEY`
- `MCEGOLD_USERNAME`
- `MCEGOLD_PASSWORD`
- `--api-key-file <path>`
- `--password-file <path>`

Secret-file values override environment variables, which override the JSON config file. `config show` requires `--redact`; there is no unredacted display mode.

## CLI Workflows

The CLI project is `samples/csharp/MCEGold.Data.Services.Connector.Cli`.

Supported commands include:

- `version`
- `config validate`
- `config show`
- `request preview`
- `request run`
- `request open-session`
- `request post`
- `request read-response`
- `request remove-response`
- `request close-session`
- `publication receive`
- `publication open-subscription`
- `publication read`
- `publication remove`
- `publication close-subscription`

`request preview` is offline. All request/session and publication receive/read/remove/close commands that contact a service require local configuration and credentials.

The CLI emits structured JSON envelopes. Failure envelopes use a normalized top-level `fault` object. Raw service responses are omitted unless `--include-raw` is explicitly supplied.

## Request/Response Workflow

Request payload examples live under `payloads/requests/`. The short-form payloads support `requestType`, optional `payloadProfile`, paging/limit fields, and type-specific filters.

For explicit session ownership:

```powershell
dotnet run --project .\samples\csharp\MCEGold.Data.Services.Connector.Cli -- request open-session --config .\configs\connector.config.development.json
dotnet run --project .\samples\csharp\MCEGold.Data.Services.Connector.Cli -- request post --config .\configs\connector.config.development.json --session-id <session-id> --input .\payloads\requests\get-sites.example.json
dotnet run --project .\samples\csharp\MCEGold.Data.Services.Connector.Cli -- request read-response --config .\configs\connector.config.development.json --session-id <session-id> --request-id <message-id>
dotnet run --project .\samples\csharp\MCEGold.Data.Services.Connector.Cli -- request remove-response --config .\configs\connector.config.development.json --session-id <session-id> --request-id <message-id>
dotnet run --project .\samples\csharp\MCEGold.Data.Services.Connector.Cli -- request close-session --config .\configs\connector.config.development.json --session-id <session-id>
```

`request run` is a convenience workflow for simple end-to-end calls.

## Publication Workflow

For explicit subscription ownership:

```powershell
dotnet run --project .\samples\csharp\MCEGold.Data.Services.Connector.Cli -- publication open-subscription --config .\configs\connector.config.development.json
dotnet run --project .\samples\csharp\MCEGold.Data.Services.Connector.Cli -- publication read --config .\configs\connector.config.development.json --session-id <session-id>
dotnet run --project .\samples\csharp\MCEGold.Data.Services.Connector.Cli -- publication remove --config .\configs\connector.config.development.json --session-id <session-id>
dotnet run --project .\samples\csharp\MCEGold.Data.Services.Connector.Cli -- publication close-subscription --config .\configs\connector.config.development.json --session-id <session-id>
```

`publication receive` is a convenience workflow for receive/remove loops.

## Console Sample

`samples/csharp/MCEGold.Data.Services.Connector.Console` is an interactive developer console. Its public template is `appsettings.example.json`; copy it to the ignored `appsettings.Development.json` for local live use.

```powershell
dotnet restore .\samples\csharp\MCEGold.Data.Services.Connector.Console\MCEGold.Data.Services.Connector.Console.csproj
dotnet build .\samples\csharp\MCEGold.Data.Services.Connector.Console\MCEGold.Data.Services.Connector.Console.csproj --no-restore
```

## Python Wrapper Samples

Python samples in `samples/python/` call the CLI as a subprocess and parse its JSON output. They do not store credentials. Demo session state is written to the ignored `samples/python/sample_state.local.json`.

## Linux Python Package

Create the Linux Python Toolkit package from a Linux or WSL environment with the .NET SDK available:

```bash
bash ./scripts/package-python-linux-x64.sh
```

The script publishes the CLI as a self-contained `linux-x64` executable, stages the Python samples, config template, and request payload examples, then writes `artifacts/mcegold-python-linux-x64-v1.0.0.tar.gz`. The archive preserves the CLI executable bit and validates a clean extraction, so end users should not need to run `chmod +x`.

## Validation Utilities

- `validation/MCEGold.Data.Services.Connector.PackageConsumer` validates package consumption from NuGet.
- `validation/MCEGold.Data.Services.Connector.RuntimeSmoke` is a live-service smoke test and must only be run with an intentionally prepared local config.
- `scripts/validate-connector-package.ps1` runs package validation.
- `scripts/validate-connector-runtime.ps1` runs live runtime validation and should not be used for offline public-candidate checks.

## Legacy WinForms Samples

`samples/csharp/MCEGold.Request.Consumer` and `samples/csharp/MCEGold.Publication.Consumer` are legacy .NET Framework WinForms samples. They retain `packages.config` for normal package restore, but committed package binary caches are intentionally excluded from this public candidate.

## Related Projects

MCEGold Discovery Portal and MCEGold Discovery PublicationCollector are separate companion applications that may consume Connector data. This Toolkit focuses on Connector package usage, CLI automation, sample payloads, and validation utilities.

## License

MIT. See `LICENSE`.
