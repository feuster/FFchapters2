@echo off
cd %~dp0
call Cake_Init_Restore.cmd
dotnet tool run dotnet-cake build.cake --configuration=release --framework=net7.0 --runtime=win-x64 --target=build
echo.
pause