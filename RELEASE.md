# Release Instructions

## Prerequisites

- .NET 9.0 SDK installed
- Visual Studio or `dotnet` CLI available

## Steps

### 1. Bump the version

Update `<Version>` in `DWHelperUI/DWHelperUI.csproj`:

```xml
<Version>X.Y.Z</Version>
```

### 2. Update NuGet packages

Check for outdated packages:

```powershell
dotnet list DualWriteHelper.sln package --outdated
```

Update versions in the `.csproj` files:
- `DualWriteHelper/DWHelperCMD.csproj`
- `DWLibary/DWLibary.csproj`
- `DWHelperUI/DWHelperUI.csproj`

> **Note:** Selenium.WebDriver updates often change the DevTools version namespace. If upgraded, update the `V###` references in `DWLibary/EdgeUniversal.cs` (lines 9-10) to match the new version bundled in the package. Inspect available versions with:
> ```powershell
> [System.Reflection.Assembly]::LoadFrom("$env:USERPROFILE\.nuget\packages\selenium.webdriver\<VERSION>\lib\net8.0\WebDriver.dll").GetTypes() |
>   Where-Object { $_.Namespace -like "OpenQA.Selenium.DevTools.V*" } |
>   Select-Object -ExpandProperty Namespace -Unique | Sort-Object
> ```

### 3. Restore and build

```powershell
dotnet restore DualWriteHelper.sln
dotnet build DualWriteHelper.sln -c Release
```

Verify **0 errors** before proceeding (warnings are acceptable).

### 4. Create the release zip

```powershell
$src = "DWHelperUI\bin\Release\net9.0-windows7.0"
$zip = "DWHelper.zip"
if (Test-Path $zip) { Remove-Item $zip }
Compress-Archive -Path "$src\*" -DestinationPath $zip
```

The zip is excluded from git via `.gitignore`.

### 5. Distribute

Upload `DWHelper.zip` to the release target (GitHub Releases, SharePoint, etc.).

## A note on antivirus false positives

This tool automates browser sign-in (entering username/password/MFA codes with Selenium) and inspects
network traffic to capture the resulting OAuth token, then stores credentials locally using DPAPI
(`ProtectedData`) encryption. These behaviors are legitimate and necessary for unattended Dual-write
setup, but they resemble heuristics some antivirus engines use to flag credential-harvesting/downloader
malware (e.g. Windows Defender's `Trojan:Win32/Suschil!rfn`), which can result in a false-positive
detection of the compiled binary (see [#71](https://github.com/microsoft/Dual-write-automations/issues/71)).

If your antivirus flags a release build:
- Verify the file hash against the one published with the GitHub release before running it.
- If you still have concerns, build the tool yourself from source following the steps above.
- Report suspected false positives to your antivirus vendor (e.g. via the
  [Microsoft Defender submission portal](https://www.microsoft.com/en-us/wdsi/filesubmission)) so the
  detection can be reviewed.

## Release History

| Version | Date       | Notes |
|---------|------------|-------|
| 1.0.14  | 2026-04-15 | NuGet package updates, Selenium DevTools V143→V145, map config ordering fix |
| 1.0.13  | —          | Previous release |
