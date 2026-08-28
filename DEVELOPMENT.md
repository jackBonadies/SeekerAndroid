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
dotnet publish .\Seeker\Seeker.csproj -p:AndroidKeyStore=false -p:EmbedAssembliesIntoApk=true -c "Debug Mock" -f net10.0-android36.0

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
dotnet publish -c Release -f net10.0-android36.0 .\Seeker\Seeker.csproj
dotnet publish -c "Release IzzySoft" -f net10.0-android36.0 .\Seeker\Seeker.csproj
```

## AOT Profiles

To speed up startup time for Release builds we use AOT compilation of methods based on a custom profile.
To generate this profile:
```powershell
dotnet build .\Seeker\Seeker.csproj -c Release -t:BuildAndStartAotProfiling -p:Device=R5CW7238Q0D -p:RunAOTCompilation=false -p:CustomAfterMicrosoftCommonTargets=C:\Users\jack\.aotprof\profile.target
# Login, Perform Search and Browse
dotnet build .\Seeker\Seeker.csproj -c Release -t:FinishAotProfiling -p:Device=R5CW7238Q0D -p:CustomAfterMicrosoftCommonTargets=C:\Users\jack\.aotprof\profile.targets
```

This will generate the custom.aprof file in Seeker\custom.aproj with a list of methods that should be AOT compiled during the build step.  To inspect the file run:
```powershell
~\.aotprof\tools\aprofutil.exe Seeker\custom.aprof
```

Reset device state with:
```
adb -s R5CW7238Q0D shell setprop debug.mono.profile ""
```


To measure speedup
