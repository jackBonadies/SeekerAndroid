# Development

## Debugging

Visual Studio is the preferred IDE: open `Seeker.sln`, select a Configuration (Debug / Debug Mock), 
Debug > Start Debugging (F5) / Start Without Debugging (Ctrl+F5).

CLI equivalents:

```powershell
# build + fast-deploy
dotnet build .\Seeker\Seeker.csproj -p:AndroidKeyStore=false -c "Debug Mock" -t:Install

# build + fast-deploy + launch
dotnet build .\Seeker\Seeker.csproj -p:AndroidKeyStore=false -c "Debug Mock" -t:Run

# build standalone + deploy
dotnet publish .\Seeker\Seeker.csproj -p:AndroidKeyStore=false -p:EmbedAssembliesIntoApk=true -c "Debug Mock" -f net9.0-android

# view app logs
adb logcat --pid=(adb shell pidof com.companyname.andriodapp1)
```

Notes:

- Debug configs use fast deployment by default so they are not standalone apks. 
  Use `-p:EmbedAssembliesIntoApk=true` for standalone apks.
- `Debug` uses the real `SoulseekClient` and logs into the live
  Soulseek server. `Debug Mock` uses an in process mock server `MockSoulseekClient` — search, 
  download, upload behavior can be controlled via keywords (`n:N`, `t:N`, `speed:N`, `failat:N`, `stall`, etc).



## Publishing

### One-shot release script

Builds the universal + all four per-ABI IzzySoft APKs and stages them in
`.\release-apks\` with IzzyOnDroid-friendly names. 

```powershell
.\Misc\package-release.ps1
.\Misc\package-release.ps1 -Version 120
```

### Universal APK (all ABIs in one file)

```powershell
dotnet publish -c Release -f net9.0-android .\Seeker\Seeker.csproj
dotnet publish -c "Release IzzySoft" -f net9.0-android .\Seeker\Seeker.csproj
```
