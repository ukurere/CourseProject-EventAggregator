@echo off
title EventAggregator
echo.
echo  ==============================
echo   EventAggregator - Starting...
echo  ==============================
echo.

start "Backend (.NET)" cmd /k "title Backend && cd /d %~dp0EventAggregator && dotnet run"

echo  Waiting for backend to initialize...
timeout /t 5 /nobreak > nul

start "Frontend (Angular)" cmd /k "title Angular && cd /d %~dp0event-aggregator-client && npm start"

echo.
echo  Backend:  https://localhost:7238
echo  Frontend: http://localhost:4200  ^<-- open this in browser
echo.
echo  Close both terminal windows to stop.
pause
