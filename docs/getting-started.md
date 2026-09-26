# Getting Started

Choose the path that matches how you want to use the Toolkit.

## Windows Console Package

Use this path when you want an interactive Windows demo without installing .NET separately.

1. Download `mcegold-console-win-x64-v1.0.0.zip`.
2. Extract the ZIP.
3. Copy `appsettings.example.json` to `appsettings.Development.json`.
4. Fill in the connection settings.
5. Run `MCEGold.Data.Services.Connector.Console.exe`.

See [Windows Console](windows-console.md).

## Linux / Python Package

Use this path when you want numbered Python samples that call the bundled CLI.

1. Download `mcegold-python-linux-x64-v1.0.0.tar.gz`.
2. Extract the archive.
3. Confirm `MCEGold.Data.Services.Connector.Cli` is executable.
4. Copy `configs/connector.config.example.json` to `configs/connector.config.development.json`.
5. Fill in the connection settings.
6. Run the numbered Python samples.

See [Linux / Python](linux-python.md).

## Source Development

Use this path when you want to inspect, build, or run projects from the repository.

You need the .NET 8 SDK. Python 3.10 or later is needed only for the Python samples.

See [Source Development](source-development.md).

## Legacy WinForms Reference

Use this path only when you need the older WinForms source samples. They are not self-contained release packages.

See [Legacy WinForms](legacy-winforms.md).

## Configuration

The Toolkit has two public config templates:

- CLI/Python: `configs/connector.config.example.json`
- Console: `appsettings.example.json` in the Console package or source folder

See [Configuration](configuration.md).
