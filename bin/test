#!/bin/bash
set -e

dotnet test --verbosity minimal --no-build --configuration Release --blame -p:CollectCoverage=true -p:CoverletOutput="../coverage.json" tests/Soulseek.Tests.Unit -p:Include="[Soulseek]*"
dotnet test --verbosity minimal --no-build --configuration Release --blame -p:CollectCoverage=true -p:MergeWith="../coverage.json" -p:CoverletOutput="../opencover.xml" -p:CoverletOutputFormat=opencover tests/Soulseek.Tests.Integration -p:Include="[Soulseek]*"