@echo off
SETLOCAL EnableExtensions EnableDelayedExpansion

SET "ROOT_DIR=%~dp0"
SET "ROOT_DIR=%ROOT_DIR:~0,-1%"
SET "FRONTEND_DIR=%ROOT_DIR%\Frontend"
SET "REMOTE=origin"
SET "BRANCH=main"
SET "PM2_APP_NAME=LeakTesterFrontend"
SET "PORT=3000"

IF NOT "%~1"=="" SET "BRANCH=%~1"
IF NOT "%~2"=="" SET "PORT=%~2"

echo ============================================================
echo LeakTester Frontend App Update
echo Root: %ROOT_DIR%
echo Frontend: %FRONTEND_DIR%
echo Source: %REMOTE%/%BRANCH%
echo PM2 App: %PM2_APP_NAME%
echo Port: %PORT%
echo ============================================================

CALL :CheckCommand git
IF ERRORLEVEL 1 GOTO Failed
CALL :CheckCommand node
IF ERRORLEVEL 1 GOTO Failed
CALL :CheckCommand npm
IF ERRORLEVEL 1 GOTO Failed
CALL :CheckCommand pm2
IF ERRORLEVEL 1 GOTO Failed

cd /d "%ROOT_DIR%"
IF ERRORLEVEL 1 (
    echo ERROR: Failed to open project folder.
    GOTO Failed
)

echo.
echo Pulling latest source...
git fetch "%REMOTE%" "%BRANCH%"
IF ERRORLEVEL 1 GOTO Failed

git pull --ff-only "%REMOTE%" "%BRANCH%"
IF ERRORLEVEL 1 (
    echo.
    echo ERROR: Pull failed. Commit or stash local changes, then run this BAT again.
    GOTO Failed
)

echo.
echo Stopping old frontend process if running...
pm2 describe "%PM2_APP_NAME%" >nul 2>&1
IF NOT ERRORLEVEL 1 (
    pm2 delete "%PM2_APP_NAME%"
) ELSE (
    echo Old frontend process was not found. Continue to build.
)

IF NOT EXIST "%FRONTEND_DIR%\package.json" (
    echo ERROR: Frontend package.json was not found:
    echo %FRONTEND_DIR%\package.json
    GOTO Failed
)

echo.
echo Installing frontend packages...
cd /d "%FRONTEND_DIR%"
IF ERRORLEVEL 1 GOTO Failed

IF EXIST "package-lock.json" (
    npm ci
) ELSE (
    npm install
)
IF ERRORLEVEL 1 GOTO Failed

echo.
echo Building frontend...
npm run build
IF ERRORLEVEL 1 GOTO Failed

echo.
echo Starting frontend with PM2...
SET "NODE_ENV=production"
SET "PORT=%PORT%"
pm2 start "node_modules\next\dist\bin\next" --name "%PM2_APP_NAME%" -- start -p "%PORT%"
IF ERRORLEVEL 1 GOTO Failed

pm2 save
IF ERRORLEVEL 1 GOTO Failed

echo.
echo ============================================================
echo Frontend update completed successfully.
echo URL: http://localhost:%PORT%
echo Check status: pm2 status
echo View logs: pm2 logs %PM2_APP_NAME%
echo ============================================================
pause
exit /b 0

:CheckCommand
where %~1 >nul 2>&1
IF ERRORLEVEL 1 (
    echo ERROR: %~1 is not available in PATH.
    exit /b 1
)
exit /b 0

:Failed
cd /d "%ROOT_DIR%" >nul 2>&1
echo.
echo ============================================================
echo Frontend update failed.
echo Please check the error message above.
echo ============================================================
pause
exit /b 1
