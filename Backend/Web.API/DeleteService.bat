@echo off
SETLOCAL

:: Get the directory of the batch file
SET "BAT_DIR=%~dp0"

:: Output the directory to verify
echo The batch file is running from: %BAT_DIR%

:: Set the name of the service to be deleted
:: Replace 'YourServiceName' with the actual service name or pass it as an argument
SET "PROJECT_NAME=Web.API"

:: Delete the Windows Service
echo Deleting service...
sc delete %PROJECT_NAME%

:: Check if the service was deleted successfully
IF %ERRORLEVEL% EQU 0 (
    echo Service deleted successfully.
) ELSE (
    echo Failed to delete the service. Error code: %ERRORLEVEL%
)

ENDLOCAL
pause
