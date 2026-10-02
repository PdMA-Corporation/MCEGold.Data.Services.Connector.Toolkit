# WinForms Samples

The WinForms projects are .NET Framework 4.7.2 source/reference samples:

- `samples/csharp/MCEGold.Request.Consumer`
- `samples/csharp/MCEGold.Publication.Consumer`

They are not self-contained release packages and are not the recommended beginner path.

Use them when you need to inspect the older .NET Framework sample UI behavior or compare legacy package usage.

## How To Use

Open the solution in Visual Studio, select **Build > Clean Solution**, then select **Build > Rebuild Solution**. After the rebuild completes, run the sample.

If Visual Studio reports missing NuGet packages, restore NuGet packages for the solution and rebuild. A separate .NET Framework developer pack may be required if Visual Studio does not already provide the 4.7.2 targeting pack.

These projects retain legacy project structure and package-restore behavior. They are not intended for the Linux/Python package or the Windows Console package.

## Recommended Alternatives

- For Windows beginners, use [Windows Console](windows-console.md).
- For Linux/Python users, use [Linux / Python](linux-python.md).
- For automation and scripting, use the CLI described in [CLI Reference](cli-reference.md).
