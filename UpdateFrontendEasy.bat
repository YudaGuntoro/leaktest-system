@echo off
SETLOCAL EnableExtensions

SET "ROOT_DIR=%~dp0"
SET "ROOT_DIR=%ROOT_DIR:~0,-1%"
IF EXIST "%ROOT_DIR%\package.json" (
    SET "FRONTEND_DIR=%ROOT_DIR%"
    FOR %%I IN ("%ROOT_DIR%\..") DO SET "PROJECT_ROOT=%%~fI"
) ELSE (
    SET "FRONTEND_DIR=%ROOT_DIR%\Frontend"
    SET "PROJECT_ROOT=%ROOT_DIR%"
)
SET "BRANCH=main"
SET "PORT=3000"
SET "HOST=0.0.0.0"
SET "NODE_DIR=%ProgramFiles%\nodejs"
SET "POWERSHELL=%WINDIR%\System32\WindowsPowerShell\v1.0\powershell.exe"

IF NOT "%~1"=="" SET "BRANCH=%~1"
IF NOT "%~2"=="" SET "PORT=%~2"

IF NOT EXIST "%NODE_DIR%\npm.cmd" (
    echo ERROR: Node.js was not found at "%NODE_DIR%".
    echo Install Node.js once, then run this script again.
    GOTO Failed
)
IF NOT EXIST "%POWERSHELL%" (
    echo ERROR: Windows PowerShell was not found at "%POWERSHELL%".
    GOTO Failed
)
IF NOT EXIST "%FRONTEND_DIR%\package.json" (
    echo ERROR: Frontend package.json was not found:
    echo %FRONTEND_DIR%\package.json
    GOTO Failed
)

SET "PATH=%NODE_DIR%;%WINDIR%\System32;%PATH%"

"%WINDIR%\System32\fltmc.exe" >nul 2>&1
IF ERRORLEVEL 1 (
    echo Requesting administrator access to stop the old frontend process...
    "%POWERSHELL%" -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~f0' -ArgumentList '%BRANCH%', '%PORT%' -Verb RunAs"
    exit /b 0
)

echo ============================================================
echo LeakTester Frontend Update (without PM2)
echo Frontend: %FRONTEND_DIR%
echo Branch: %BRANCH%
echo Port: %PORT%
echo ============================================================

cd /d "%PROJECT_ROOT%"
IF ERRORLEVEL 1 GOTO Failed

echo.
echo Pulling latest source...
git pull --ff-only origin "%BRANCH%"
IF ERRORLEVEL 1 GOTO Failed

echo.
echo Stopping the old LeakTester frontend, if running...
"%POWERSHELL%" -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference = 'Stop'; try { $serverPath = '%FRONTEND_DIR%\.next\standalone\server.js'; Get-CimInstance Win32_Process | Where-Object { $_.Name -eq 'node.exe' -and $_.CommandLine -like ('*' + $serverPath + '*') } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force } } catch { Write-Error $_; exit 1 }"
IF ERRORLEVEL 1 GOTO Failed
timeout /t 2 /nobreak >nul

cd /d "%FRONTEND_DIR%"
IF ERRORLEVEL 1 GOTO Failed

echo.
echo Installing frontend packages...
IF EXIST "package-lock.json" (
    CALL "%NODE_DIR%\npm.cmd" ci
) ELSE (
    CALL "%NODE_DIR%\npm.cmd" install
)
IF ERRORLEVEL 1 GOTO Failed

echo.
echo Building frontend...
CALL "%NODE_DIR%\npm.cmd" run build
IF ERRORLEVEL 1 GOTO Failed

IF NOT EXIST ".next\standalone\server.js" (
    echo ERROR: Standalone server was not generated.
    GOTO Failed
)

echo.
echo Copying standalone assets...
IF EXIST ".next\static" (
    IF NOT EXIST ".next\standalone\.next" mkdir ".next\standalone\.next"
    xcopy ".next\static" ".next\standalone\.next\static" /E /I /Y >nul
    IF ERRORLEVEL 1 GOTO Failed
)
IF EXIST "public" (
    xcopy "public" ".next\standalone\public" /E /I /Y >nul
    IF ERRORLEVEL 1 GOTO Failed
)

echo.
echo Starting frontend in the background...
SET "NODE_ENV=production"
SET "PORT=%PORT%"
SET "HOSTNAME=%HOST%"
SET "SERVER_API_BASE_URL=http://localhost:5241"
"%POWERSHELL%" -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%NODE_DIR%\node.exe' -ArgumentList '%FRONTEND_DIR%\.next\standalone\server.js' -WorkingDirectory '%FRONTEND_DIR%' -WindowStyle Hidden"
IF ERRORLEVEL 1 GOTO Failed

echo.
echo Frontend updated and started without PM2.
echo URL: http://localhost:%PORT%
echo ============================================================
pause
exit /b 0

:Failed
echo.
echo Frontend update failed. Review the error above.
pause
exit /b 1
