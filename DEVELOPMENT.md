# Development

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
