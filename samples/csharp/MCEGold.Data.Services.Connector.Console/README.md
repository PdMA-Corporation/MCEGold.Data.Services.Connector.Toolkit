# MCEGold.Data.Services.Connector.Console

This project is the interactive C# Console for exploring `MCEGold.Data.Services.Connector`. It is distinct from the automation-focused Connector CLI.

## Ready-to-Run Windows Package

Download `mcegold-console-win-x64-v1.0.0.zip` from [GitHub Releases](https://github.com/PdMA-Corporation/temp-MCEGold.Data.Services.Connector.Toolkit/releases/latest). The packaged build is self-contained for Windows x64, so it does not require a separate .NET runtime.

After extracting the package:

1. Copy `appsettings.example.json` to `appsettings.Development.json`.
2. Enter the connector host and credentials in `appsettings.Development.json`.
3. Run `MCEGold.Data.Services.Connector.Console.exe`.

See the [Windows Console guide](../../../docs/windows-console.md) for the complete first-run path.

## Configuration

Interactive mode reads `appsettings.Development.json` from the executable folder and then the current working directory. Create it from the public example:

```powershell
Copy-Item appsettings.example.json appsettings.Development.json
```

The beginner shape is:

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

Do not commit `appsettings.Development.json`. See [Configuration](../../../docs/configuration.md) for details.

## Interactive Mode

The main menu provides two explicit lifecycles:

- **Publication / Subscription:** open subscription, read publication, remove publication, and close subscription.
- **Request / Response:** open request session, post request, read response, remove response, and close request session.

Request workflows are available for all [supported request types](../../../docs/request-types.md). Keep each lifecycle in the same Console process because the sample holds session state in memory.

## Run From Source

Install the .NET 8 SDK, create `appsettings.Development.json`, then run from the Toolkit root:

```powershell
dotnet run --project samples/csharp/MCEGold.Data.Services.Connector.Console/MCEGold.Data.Services.Connector.Console.csproj
```

IDE users can open the project in Visual Studio or another .NET-capable IDE. See [Build from Source](../../../docs/source-development.md).

## JSON Input

For advanced testing, pass operation-specific JSON from a file:

```powershell
.\MCEGold.Data.Services.Connector.Console.exe --input <path>
```

or from stdin:

```powershell
Get-Content .\command.json | .\MCEGold.Data.Services.Connector.Console.exe --input -
```

When running from source, place the same arguments after `--`:

```powershell
dotnet run --project samples/csharp/MCEGold.Data.Services.Connector.Console/MCEGold.Data.Services.Connector.Console.csproj -- --input <path>
```

Input examples are under `samples/json/`; schemas are under `docs/json-schema/`. For workflow concepts, see [Request Workflow](../../../docs/request-workflow.md) and [Publication Workflow](../../../docs/publication-workflow.md).
