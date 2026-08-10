@echo off
SETLOCAL EnableExtensions

SET "ROOT_DIR=%~dp0"
SET "WORKER_REGISTER=%ROOT_DIR%Backend\LeakTestWorker\RegisterService.bat"

echo Registering Leak Test Worker service...

IF NOT EXIST "%WORKER_REGISTER%" (
    echo ERROR: Worker registration script was not found:
    echo %WORKER_REGISTER%
    pause
    exit /b 1
)

net session >nul 2>&1
IF ERRORLEVEL 1 (
    echo Requesting Administrator permission...
    powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b 0
)

CALL "%WORKER_REGISTER%"
exit /b %ERRORLEVEL%
