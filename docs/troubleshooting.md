# Troubleshooting

## Missing Console Config

Interactive Console mode requires:

```text
appsettings.Development.json
```

Create it by copying `appsettings.example.json` in the extracted Console package or Console source folder.

## Missing CLI / Python Config

CLI and Python live workflows require:

```text
configs/connector.config.development.json
```

Create it by copying `configs/connector.config.example.json`.

## Authentication Failures

Check:

- `host`
- `authenticationScheme`
- `apiKey`
- `userName`
- `password`
- environment-variable overrides
- secret-file overrides

Do not paste credentials into issues, logs, or documentation.

## Linux CLI Execute Permission

The Linux package should preserve the executable bit. If validation fails, check:

```bash
ls -l ./MCEGold.Data.Services.Connector.Cli
test -x ./MCEGold.Data.Services.Connector.Cli
```

The packaged CLI is self-contained and should not require a system .NET runtime.

## Windows Console Runtime

The Windows Console package is self-contained and should not require a separate .NET runtime.

Run the executable from the extracted package folder:

```powershell
.\MCEGold.Data.Services.Connector.Console.exe
```

## JSON Validation Failures

For request payloads, make sure the input contains a PascalCase connector `requestType`, such as:

```json
{
  "requestType": "GetSites"
}
```

See [Supported Request Types](request-types.md).

## Stale Session Or Message IDs

Python sample state is local demo state. Replacing or deleting `samples/python/sample_state.local.json` does not close remote sessions.

When possible, run the close script before starting a new session:

```bash
python3 samples/python/04_close_subscription.py
python3 samples/python/09_close_request_session.py
```

## NuGet Restore Or Config Permission Issues

Source builds use NuGet restore. If restore fails while reading a user NuGet config, inspect local NuGet permissions and configuration outside the repository.

If restore cannot reach NuGet feeds, check network access, TLS inspection, proxy settings, and credential provider configuration.

## TLS Or Network Issues

Transport failures usually point to:

- unreachable host
- DNS problems
- proxy/firewall rules
- TLS or certificate trust issues
- incorrect endpoint path

Use `--include-raw` only when you need diagnostic detail, and handle raw responses as sensitive data.
