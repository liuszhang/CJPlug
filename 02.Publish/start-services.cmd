@echo off
rem ===================================================================
rem  CJPlug local services launcher (manual, no Aspire)
rem
rem  Why env vars instead of app.Run("--urls"):
rem    Kestrel:EndPoints in appsettings.json takes precedence over
rem    --urls / ASPNETCORE_URLS, and the packaged per-service json files
rem    (appsettings.CJ.Plug.*.json) are NOT refreshed by build. So the
rem    port MUST be given explicitly -- same convention as AppHost.cs
rem    (KestrelUrlEnvKey). Missing 8686 is what caused
rem    "HttpRequestException ... (localhost:8686)" on the Web host.
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

echo [INFO] service dir: %SERVICEDIR%
echo [INFO] starting 4 services in separate windows...

rem 1) DispatchServer - SignalR Hub center  :8686
start "CJPlug-DispatchServer:8686" cmd /k "cd /d "%SERVICEDIR%" && set Kestrel__EndPoints__Http__Url=http://*:8686 && dotnet CJ.Plug.DispatchServer.dll"

rem 2) ApiServer - HTTP API  :8687
start "CJPlug-ApiServer:8687" cmd /k "cd /d "%SERVICEDIR%" && set Kestrel__EndPoints__Http__Url=http://*:8687 && dotnet CJ.Plug.ApiServer.dll"

rem 3) McpServer - MCP bridge  :3001
start "CJPlug-McpServer:3001" cmd /k "cd /d "%SERVICEDIR%" && set Kestrel__EndPoints__Http__Url=http://*:3001 && dotnet CJ.Plug.McpServer.dll"

rem 4) HostWebServer - Blazor Web UI  :5066
start "CJPlug-HostWebServer:5066" cmd /k "cd /d "%SERVICEDIR%" && set Kestrel__EndPoints__Http__Url=http://*:5066 && dotnet CJ.Plug.HostWebServer.dll"

echo [INFO] all launched. wait ~15s then open http://127.0.0.1:5066/
echo [INFO] quick check: netstat -ano ^| findstr LISTENING ^| findstr ":8686 :8687 :3001 :5066"
