@echo off
cd %~dp0
echo ========================================
echo .net Info
echo ========================================
echo.
dotnet --info
echo.
if exist ".config/dotnet-tools.json" (
	echo ========================================
	echo Cake Restore
	echo ========================================
	echo.
	dotnet tool restore
) else (
	echo ========================================
	echo Cake Install
	echo ========================================
	echo.
	dotnet new tool-manifest
	dotnet tool install Cake.Tool --version 3.0.0
	echo.
)
