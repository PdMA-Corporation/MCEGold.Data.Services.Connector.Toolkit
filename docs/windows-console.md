# Windows Console

The Windows Console package is the beginner-friendly interactive path for Windows users.

Artifact:

```text
mcegold-console-win-x64-v1.0.0.zip
```

The package is self-contained, so users do not need to install a separate .NET runtime.

## First Run

1. Download and extract `mcegold-console-win-x64-v1.0.0.zip`.
2. In the extracted folder, copy:

   ```text
   appsettings.example.json
   ```

   to:

   ```text
   appsettings.Development.json
   ```

3. Edit `appsettings.Development.json` and fill in the connection settings.
4. Run:

   ```powershell
   .\MCEGold.Data.Services.Connector.Console.exe
   ```

## Configuration

The Console reads `appsettings.Development.json` for interactive mode. It first checks the executable folder and then the current working directory.

The public example contains placeholders only:

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

Do not put real credentials in documentation, Git commits, or issue reports.

See [Configuration](configuration.md) for details.

## Interactive Workflows

The Console menu exposes:

- Consumer publication operations: open subscription, read publication, remove publication, close subscription.
- Consumer request operations: open request session, post request, read response, remove response, close request session.

The Console keeps session state in the running process. Open, read/post, remove, and close operations should be run in the same Console session.

For workflow concepts, see [Publication Workflow](publication-workflow.md) and [Request Workflow](request-workflow.md).

## Advanced JSON Input

The Console also accepts:

```powershell
.\MCEGold.Data.Services.Connector.Console.exe --input <path>
```

or stdin:

```powershell
Get-Content .\command.json | .\MCEGold.Data.Services.Connector.Console.exe --input -
```

This path passes operation-specific JSON to the connector runner. It is intended for advanced testing, not as the main beginner workflow.
