@echo off
setlocal

:: Run Premake to generate the csproj file
call "Premake/premake5" --file=premake5.lua vs2022

:: Define paths
set "csprojFile=TGEHub\TGEHub.csproj"

:: Check if the .csproj file exists
if not exist "%csprojFile%" (
    echo ERROR: %csprojFile% not found!
    pause
    exit /b 1
)

:: Run PowerShell script to modify the .csproj file
powershell -ExecutionPolicy Bypass -File Premake/modify_csproj.ps1 "%csprojFile%"

echo Done!
pause