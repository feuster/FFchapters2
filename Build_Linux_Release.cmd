@echo off
cd %~dp0
call Cake_Init_Restore.cmd
dotnet tool run dotnet-cake build.cake --configuration=linux_release --framework=net9.0 --runtime=linux-x64 --target=build
echo.
pause