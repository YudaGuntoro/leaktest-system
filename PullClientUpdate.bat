@echo off
SETLOCAL EnableExtensions EnableDelayedExpansion

SET "ROOT_DIR=%~dp0"
SET "ROOT_DIR=%ROOT_DIR:~0,-1%"
SET "BRANCH=main"
SET "REMOTE=origin"
SET "BACKUP_DIR=%ROOT_DIR%\.client-update-backup\%DATE:~-4%%DATE:~3,2%%DATE:~0,2%-%TIME:~0,2%%TIME:~3,2%%TIME:~6,2%"
SET "BACKUP_DIR=%BACKUP_DIR: =0%"

SET "API_SETTINGS=Backend\Web.API\Settings.ini"
SET "WORKER_SETTINGS=Backend\LeakTestWorker\Settings.ini"
SET "BROKER_SETTINGS=Backend\MqttBrokerService\Settings.ini"

echo ============================================================
echo LeakTester Client Update
echo Root: %ROOT_DIR%
echo Branch: %BRANCH%
echo ============================================================

cd /d "%ROOT_DIR%"
IF ERRORLEVEL 1 (
    echo ERROR: Failed to open project folder.
    pause
    exit /b 1
)

CALL :CheckCommand git
IF ERRORLEVEL 1 GOTO Failed

CALL :CheckCommand dotnet
IF ERRORLEVEL 1 GOTO Failed

CALL :CheckCommand node
IF ERRORLEVEL 1 GOTO Failed

CALL :CheckCommand npm
IF ERRORLEVEL 1 GOTO Failed

net session >nul 2>&1
IF ERRORLEVEL 1 (
    SET "IS_ADMIN=0"
    echo WARNING: Not running as Administrator. Windows services will not be restarted.
) ELSE (
    SET "IS_ADMIN=1"
)

echo.
echo Backing up local Settings.ini files...
mkdir "%BACKUP_DIR%" >nul 2>&1
CALL :BackupFile "%API_SETTINGS%"
CALL :BackupFile "%WORKER_SETTINGS%"
CALL :BackupFile "%BROKER_SETTINGS%"

echo.
echo Pulling latest source from GitHub...
git fetch "%REMOTE%" "%BRANCH%"
IF ERRORLEVEL 1 GOTO Failed

git pull --ff-only "%REMOTE%" "%BRANCH%"
IF ERRORLEVEL 1 (
    echo.
    echo ERROR: Pull failed. Please commit/stash local changes, then run this BAT again.
    GOTO Failed
)

echo.
echo Restoring local Settings.ini files...
CALL :RestoreFile "%API_SETTINGS%"
CALL :RestoreFile "%WORKER_SETTINGS%"
CALL :RestoreFile "%BROKER_SETTINGS%"
CALL :EnsureDefaultSettings

IF "%IS_ADMIN%"=="1" (
    echo.
    echo Stopping registered services before publish...
    CALL :StopServiceIfExists "LeakTestWorker"
    CALL :StopServiceIfExists "LeakTestMqttBroker"
)

echo.
echo Publishing backend API...
dotnet publish "Backend\Web.API\Web.API.csproj" -c Release -o "Backend\Web.API\bin\Release\net8.0\publish"
IF ERRORLEVEL 1 GOTO Failed
CALL :CopySettings "%API_SETTINGS%" "Backend\Web.API\bin\Release\net8.0\publish\Settings.ini"

echo.
echo Publishing MQTT worker...
dotnet publish "Backend\LeakTestWorker\LeakTestWorker.csproj" -c Release -o "Backend\LeakTestWorker\bin\Release\net8.0\publish"
IF ERRORLEVEL 1 GOTO Failed
CALL :CopySettings "%WORKER_SETTINGS%" "Backend\LeakTestWorker\bin\Release\net8.0\publish\Settings.ini"

echo.
echo Publishing MQTT broker service...
dotnet publish "Backend\MqttBrokerService\MqttBrokerService.csproj" -c Release -o "Backend\MqttBrokerService\bin\Release\net8.0\publish"
IF ERRORLEVEL 1 GOTO Failed
CALL :CopySettings "%BROKER_SETTINGS%" "Backend\MqttBrokerService\bin\Release\net8.0\publish\Settings.ini"

echo.
echo Installing frontend packages...
cd /d "%ROOT_DIR%\Frontend"
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

cd /d "%ROOT_DIR%"

IF "%IS_ADMIN%"=="1" (
    echo.
    echo Starting registered services...
    CALL :StartServiceIfExists "LeakTestMqttBroker"
    CALL :StartServiceIfExists "LeakTestWorker"
)

echo.
echo ============================================================
echo Update completed successfully.
echo Backup folder: %BACKUP_DIR%
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

:BackupFile
SET "SOURCE_FILE=%~1"
IF EXIST "%ROOT_DIR%\%SOURCE_FILE%" (
    SET "BACKUP_FILE=%BACKUP_DIR%\%SOURCE_FILE:\=__%"
    copy /Y "%ROOT_DIR%\%SOURCE_FILE%" "!BACKUP_FILE!" >nul
    echo Backed up %SOURCE_FILE%
)
exit /b 0

:RestoreFile
SET "TARGET_FILE=%~1"
SET "BACKUP_FILE=%BACKUP_DIR%\%TARGET_FILE:\=__%"
IF EXIST "!BACKUP_FILE!" (
    FOR %%P IN ("%ROOT_DIR%\%TARGET_FILE%") DO mkdir "%%~dpP" >nul 2>&1
    copy /Y "!BACKUP_FILE!" "%ROOT_DIR%\%TARGET_FILE%" >nul
    echo Restored %TARGET_FILE%
)
exit /b 0

:CopySettings
SET "SOURCE_FILE=%~1"
SET "TARGET_FILE=%~2"
IF EXIST "%ROOT_DIR%\%SOURCE_FILE%" (
    copy /Y "%ROOT_DIR%\%SOURCE_FILE%" "%ROOT_DIR%\%TARGET_FILE%" >nul
)
exit /b 0

:EnsureDefaultSettings
IF NOT EXIST "%ROOT_DIR%\%API_SETTINGS%" (
    FOR %%P IN ("%ROOT_DIR%\%API_SETTINGS%") DO mkdir "%%~dpP" >nul 2>&1
    > "%ROOT_DIR%\%API_SETTINGS%" echo [Database]
    >> "%ROOT_DIR%\%API_SETTINGS%" echo ConnectionString=Server=127.0.0.1;Port=3306;User ID=root;Password=YOUR_PASSWORD;Database=yanmarleaktest;SslMode=None;AllowPublicKeyRetrieval=True;
)

IF NOT EXIST "%ROOT_DIR%\%WORKER_SETTINGS%" (
    FOR %%P IN ("%ROOT_DIR%\%WORKER_SETTINGS%") DO mkdir "%%~dpP" >nul 2>&1
    > "%ROOT_DIR%\%WORKER_SETTINGS%" echo [Worker]
    >> "%ROOT_DIR%\%WORKER_SETTINGS%" echo IntervalSeconds=1
    >> "%ROOT_DIR%\%WORKER_SETTINGS%" echo ReconnectDelaySeconds=3
    >> "%ROOT_DIR%\%WORKER_SETTINGS%" echo.
    >> "%ROOT_DIR%\%WORKER_SETTINGS%" echo [MQTT]
    >> "%ROOT_DIR%\%WORKER_SETTINGS%" echo Host=localhost
    >> "%ROOT_DIR%\%WORKER_SETTINGS%" echo Port=1883
    >> "%ROOT_DIR%\%WORKER_SETTINGS%" echo ClientId=LeakTestWorker
    >> "%ROOT_DIR%\%WORKER_SETTINGS%" echo Topic=leaktest_mqtt
    >> "%ROOT_DIR%\%WORKER_SETTINGS%" echo Qos=1
    >> "%ROOT_DIR%\%WORKER_SETTINGS%" echo Username=
    >> "%ROOT_DIR%\%WORKER_SETTINGS%" echo Password=
    >> "%ROOT_DIR%\%WORKER_SETTINGS%" echo.
    >> "%ROOT_DIR%\%WORKER_SETTINGS%" echo [Database]
    >> "%ROOT_DIR%\%WORKER_SETTINGS%" echo ConnectionString=Server=127.0.0.1;Port=3306;User ID=root;Password=root_native;Database=yanmarleaktest;SslMode=None;AllowPublicKeyRetrieval=True;
    >> "%ROOT_DIR%\%WORKER_SETTINGS%" echo.
    >> "%ROOT_DIR%\%WORKER_SETTINGS%" echo [Buffer]
    >> "%ROOT_DIR%\%WORKER_SETTINGS%" echo FilePath=buffer/leaktest-history-buffer.jsonl
    >> "%ROOT_DIR%\%WORKER_SETTINGS%" echo MaxReprocessBatch=100
)

IF NOT EXIST "%ROOT_DIR%\%BROKER_SETTINGS%" (
    FOR %%P IN ("%ROOT_DIR%\%BROKER_SETTINGS%") DO mkdir "%%~dpP" >nul 2>&1
    > "%ROOT_DIR%\%BROKER_SETTINGS%" echo [Broker]
    >> "%ROOT_DIR%\%BROKER_SETTINGS%" echo Host=0.0.0.0
    >> "%ROOT_DIR%\%BROKER_SETTINGS%" echo Port=1883
    >> "%ROOT_DIR%\%BROKER_SETTINGS%" echo Username=
    >> "%ROOT_DIR%\%BROKER_SETTINGS%" echo Password=
)
exit /b 0

:StopServiceIfExists
SET "SERVICE_NAME=%~1"
sc.exe query "%SERVICE_NAME%" >nul 2>&1
IF ERRORLEVEL 1 (
    echo Service %SERVICE_NAME% is not registered. Skip stop.
    exit /b 0
)
echo Stopping %SERVICE_NAME%...
sc.exe stop "%SERVICE_NAME%" >nul 2>&1
CALL :WaitForServiceState "%SERVICE_NAME%" "STOPPED"
exit /b 0

:StartServiceIfExists
SET "SERVICE_NAME=%~1"
sc.exe query "%SERVICE_NAME%" >nul 2>&1
IF ERRORLEVEL 1 (
    echo Service %SERVICE_NAME% is not registered. Skip start.
    exit /b 0
)
echo Starting %SERVICE_NAME%...
sc.exe start "%SERVICE_NAME%" >nul 2>&1
exit /b 0

:WaitForServiceState
SET "SERVICE_NAME=%~1"
SET "TARGET_STATE=%~2"
FOR /L %%I IN (1,1,30) DO (
    sc.exe query "%SERVICE_NAME%" | findstr /I "%TARGET_STATE%" >nul
    IF "!ERRORLEVEL!"=="0" exit /b 0
    timeout /t 1 /nobreak >nul
)
exit /b 0

:Failed
cd /d "%ROOT_DIR%" >nul 2>&1
echo.
echo ============================================================
echo Update failed.
echo Please check the error message above.
echo ============================================================
pause
exit /b 1
