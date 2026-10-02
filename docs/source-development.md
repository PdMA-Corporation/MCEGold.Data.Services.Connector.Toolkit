# Source Development

Use this page when building or running the Toolkit directly from the repository.

## Prerequisites

- .NET 8 SDK.
- Python 3.10 or later when running Python samples.
- Visual Studio 2022 or another .NET-capable IDE for C# development.
- Access to an MCEGold Data Services Connector endpoint only for live workflows.

Offline request preview does not require credentials or a live service.

## Windows Source ZIP Extraction

When downloading the GitHub source ZIP on Windows, extract it to a reasonably short path, such as `C:\MCEGoldToolkit` or `F:\MCEGoldToolkit`. Avoid creating an extra deeply nested directory structure when extracting; very long paths can cause Visual Studio build or cache errors.

## Project Locations

- CLI: `samples/csharp/MCEGold.Data.Services.Connector.Cli`
- Console: `samples/csharp/MCEGold.Data.Services.Connector.Console`
- Python samples: `samples/python`
- WinForms request sample: `samples/csharp/MCEGold.Request.Consumer`
- WinForms publication sample: `samples/csharp/MCEGold.Publication.Consumer`

There is no root solution requirement documented for the public source workflow. Restore and build the project you intend to run.

## Build The CLI

```powershell
dotnet restore .\samples\csharp\MCEGold.Data.Services.Connector.Cli\MCEGold.Data.Services.Connector.Cli.csproj
dotnet build .\samples\csharp\MCEGold.Data.Services.Connector.Cli\MCEGold.Data.Services.Connector.Cli.csproj --no-restore
dotnet run --project .\samples\csharp\MCEGold.Data.Services.Connector.Cli -- version
```

Offline preview:

```powershell
dotnet run --project .\samples\csharp\MCEGold.Data.Services.Connector.Cli -- request preview --input .\payloads\requests\get-sites.example.json
```

## Run The Console

```powershell
dotnet run --project .\samples\csharp\MCEGold.Data.Services.Connector.Console\MCEGold.Data.Services.Connector.Console.csproj
```

Interactive mode requires `appsettings.Development.json` in the Console source folder or current working directory.

## Run Python Samples From Source

Build the CLI first, then follow [Linux / Python](linux-python.md) or [samples/python/README.md](../samples/python/README.md).

The Python samples can discover a source-built CLI DLL in common Debug or Release output locations.

## Local Files

Do not commit:

- `configs/connector.config.development.json`
- `appsettings.Development.json`
- `samples/python/sample_state.local.json`
- credentials, tokens, raw responses, or local secret files
