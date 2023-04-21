@echo off
cd %~dp0
dotnet --info
dotnet tool restore
dotnet tool run dotnet-cake build.cake --configuration=release --framework=net7.0 --runtime=win-x64 --target=ziprelease
echo.
pause