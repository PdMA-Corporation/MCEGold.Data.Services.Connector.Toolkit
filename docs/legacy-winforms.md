# WinForms Samples

The WinForms projects are .NET Framework 4.7.2 source/reference samples:

- `samples/csharp/MCEGold.Request.Consumer`
- `samples/csharp/MCEGold.Publication.Consumer`

They are not self-contained runtime packages and are not the recommended beginner path.

Use them when you need to inspect the older .NET Framework sample UI behavior or compare legacy package usage.

## Recommended Windows Sample Package

Windows users who want to try the WinForms samples should download `mcegold-windows-samples-v1.0.0.zip`. This focused package contains:

- MCEGold Request Consumer WinForms sample
- MCEGold Publication Consumer WinForms sample
- this WinForms setup guide

The same WinForms samples are also available in the full GitHub source archive. The Windows samples ZIP is the focused download for users who only want these two WinForms examples.

## How To Use

1. Download `mcegold-windows-samples-v1.0.0.zip`.
2. Extract it to a reasonably short Windows path, such as `C:\MCEGoldSamples` or `F:\MCEGoldSamples`.
3. Open either sample solution in Visual Studio.
4. Select **Build > Clean Solution**.
5. Select **Build > Rebuild Solution**.
6. Run the sample.

Extracting to a reasonably short path is recommended because very long nested paths can cause Visual Studio build or cache errors.

If Visual Studio reports missing NuGet packages, restore NuGet packages for the solution and rebuild. A separate .NET Framework developer pack may be required if Visual Studio does not already provide the 4.7.2 targeting pack.

These projects retain legacy project structure and package-restore behavior. They are not intended for the Linux/Python package or the Windows Console package.

## Recommended Alternatives

- For Windows beginners, use [Windows Console](windows-console.md).
- For Linux/Python users, use [Linux / Python](linux-python.md).
- For automation and scripting, use the CLI described in [CLI Reference](cli-reference.md).
