@echo off
REM Simple launcher that starts Core, AgIO, then UI (Windows)
start "" "%~dp0core\Nexus.Core.exe"
start "" "%~dp0agio\Nexus.AgIO.exe"
start "" "%~dp0ui\Nexus.UI.exe"
