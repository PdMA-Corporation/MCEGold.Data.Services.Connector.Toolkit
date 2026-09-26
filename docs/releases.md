# Releases

This repository supports separate release-user paths for Windows and Linux users.

Download published runtime packages from [GitHub Releases](https://github.com/PdMA-Corporation/temp-MCEGold.Data.Services.Connector.Toolkit/releases/latest). GitHub also provides source archives for each tagged release.

## Windows

Artifact:

```text
mcegold-console-win-x64-v1.0.0.zip
```

Purpose:

- Interactive C# Console sample.
- Windows x64.
- Self-contained .NET runtime.
- First-run config template: `appsettings.example.json`.

See [Windows Console](windows-console.md).

## Linux

Artifact:

```text
mcegold-python-linux-x64-v1.0.0.tar.gz
```

Purpose:

- Self-contained `linux-x64` CLI.
- Numbered Python wrapper samples.
- Request payload examples.
- First-run config template: `configs/connector.config.example.json`.

See [Linux / Python](linux-python.md).

## Source Archives

GitHub automatically provides source archives for tags and releases. Source users can also clone the repository directly.

Source archives are not the same as the self-contained runtime packages. They require source-development prerequisites such as the .NET 8 SDK.

See [Source Development](source-development.md).

## Legacy WinForms Samples

The WinForms samples remain source/reference samples. They do not have self-contained release packages.

See [Legacy WinForms](legacy-winforms.md).

## Self-Contained Meaning

Self-contained packages include the .NET runtime needed by the packaged executable. Users should not need to install a separate .NET runtime to run the packaged Console or CLI.
