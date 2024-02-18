#!/bin/bash
set -e
dir="bin/Release/netcoreapp2.1/packed"
mkdir -p "$dir"

dotnet publish -c Release -r win-x64 --self-contained true
warp-packer --arch windows-x64 --input_dir bin/Release/netcoreapp2.1/win-x64/publish --exec slsk-ex.exe --output "$dir"/slsk-ex.win-x64.exe

dotnet publish -c Release -r linux-x64 --self-contained true
warp-packer --arch linux-x64 --input_dir bin/Release/netcoreapp2.1/linux-x64/publish --exec slsk-ex --output "$dir"/slsk-ex.linux-x64
chmod +x "$dir"/slsk-ex.linux-x64

dotnet publish -c Release -r osx-x64 --self-contained true
warp-packer --arch macos-x64 --input_dir bin/Release/netcoreapp2.1/osx-x64/publish --exec slsk-ex --output "$dir"/slsk-ex.osx-x64
chmod +x "$dir"/slsk-ex.osx-x64