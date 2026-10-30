@echo off
rem ===================================================================
rem  CJPlug local services launcher (manual, no Aspire)
rem
rem  2026-10-30 (P9, D1=A): **single process** -- CJPlug.Server on :5066.
rem
rem  What this script used to do (and why it was wrong):
rem    It launched 4 processes -- 8686 DispatchServer, 8687 ApiServer,
rem    3001 McpServer, 5066 HostWebServer. That layout predates the
rem    single-port consolidation. Running it after consolidation fabricates
rem    a "two hosts on one main.db" state: the two hosts wire DI differently,
rem    so their EF model sets differ (29 vs 80 tables), and CJCore's auto
rem    migration concludes "the model regressed, drop 51 tables" and
rem    fail-fasts => the main host cannot start at all (real incident,
rem    2026-10-30 morning).
rem
rem  The single host routes by PATH, not by port:
rem    /                           Blazor UI
rem    /api/*                      business APIs + swagger
rem    /elsa/api/*                 Elsa engine
rem    /mainHub, /api/dispatch/*   dispatch hub + REST
rem    /mcp                        MCP protocol surface
rem
rem  Port 5066 is a compile-time contract (GlobalData.* references it ~88
rem  times). Only ASPNETCORE_URLS may override it; do not change casually.
rem
rem  Working dir matters: connection strings use ../../main.db
rem    => must run from Debug\net10.0 (or Release\net10.0).
rem ===================================================================

set "CONFIG=%~1"
if "%CONFIG%"=="" set "CONFIG=Debug"
set "SERVICEDIR=%~dp0Services\%CONFIG%\net10.0"

if not exist "%SERVICEDIR%" (
  echo [ERROR] not found: %SERVICEDIR%
  echo usage: start-services.cmd [Debug^|Release]
  pause
  exit /b 1
)

if not exist "%SERVICEDIR%\CJPlug.Server.dll" (
  echo [ERROR] host not found: %SERVICEDIR%\CJPlug.Server.dll
  echo         build it first:  dotnet build src/Server/CJPlug.Server/CJPlug.Server.csproj -c %CONFIG%
  pause
  exit /b 1
)

echo [INFO] service dir: %SERVICEDIR%
echo [INFO] starting 1 service (CJPlug.Server, single host)...

start "CJPlug-Server:5066" cmd /k "cd /d "%SERVICEDIR%" && set ASPNETCORE_URLS=http://*:5066 && dotnet CJPlug.Server.dll"

echo [INFO] launched. wait ~15s then open http://127.0.0.1:5066/
echo [INFO] quick check: netstat -ano ^| findstr LISTENING ^| findstr ":5066"
