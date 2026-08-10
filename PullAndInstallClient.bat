@echo off
SETLOCAL EnableExtensions EnableDelayedExpansion

SET "ROOT_DIR=%~dp0"
SET "ROOT_DIR=%ROOT_DIR:~0,-1%"
SET "REMOTE=origin"
SET "BRANCH=main"
SET "BACKUP_DIR=%ROOT_DIR%\.client-update-backup\install-%DATE:~-4%%DATE:~3,2%%DATE:~0,2%-%TIME:~0,2%%TIME:~3,2%%TIME:~6,2%"
SET "BACKUP_DIR=%BACKUP_DIR: =0%"

SET "API_SETTINGS=Backend\Web.API\Settings.ini"
SET "WORKER_SETTINGS=Backend\LeakTestWorker\Settings.ini"
SET "BROKER_SETTINGS=Backend\MqttBrokerService\Settings.ini"

SET "API_PUBLISH=Backend\Web.API\bin\Release\net8.0\publish"
SET "WORKER_PUBLISH=Backend\LeakTestWorker\bin\Release\net8.0\publish"
SET "BROKER_PUBLISH=Backend\MqttBrokerService\bin\Release\net8.0\publish"

echo ============================================================
echo LeakTester Pull and Install
echo Root: %ROOT_DIR%
echo Remote: %REMOTE%/%BRANCH%
echo ============================================================

cd /d "%ROOT_DIR%"
IF ERRORLEVEL 1 (
    echo ERROR: Failed to open project folder.
    pause
    exit /b 1
)

net session >nul 2>&1
IF ERRORLEVEL 1 (
    echo ERROR: Please run this BAT as Administrator.
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
    echo ERROR: Pull failed. Commit or stash local changes, then run this BAT again.
    GOTO Failed
)

echo.
echo Restoring local Settings.ini files...
CALL :RestoreFile "%API_SETTINGS%"
CALL :RestoreFile "%WORKER_SETTINGS%"
CALL :RestoreFile "%BROKER_SETTINGS%"
CALL :EnsureDefaultSettings

echo.
echo Stopping old services if registered...
CALL :StopServiceIfExists "LeakTestWorker"
CALL :StopServiceIfExists "LeakTestMqttBroker"

echo.
echo Publishing backend API...
dotnet publish "Backend\Web.API\Web.API.csproj" -c Release -o "%API_PUBLISH%"
IF ERRORLEVEL 1 GOTO Failed
CALL :CopySettings "%API_SETTINGS%" "%API_PUBLISH%\Settings.ini"

echo.
echo Publishing MQTT worker...
dotnet publish "Backend\LeakTestWorker\LeakTestWorker.csproj" -c Release -o "%WORKER_PUBLISH%"
IF ERRORLEVEL 1 GOTO Failed
CALL :CopySettings "%WORKER_SETTINGS%" "%WORKER_PUBLISH%\Settings.ini"

echo.
echo Publishing MQTT broker...
dotnet publish "Backend\MqttBrokerService\MqttBrokerService.csproj" -c Release -o "%BROKER_PUBLISH%"
IF ERRORLEVEL 1 GOTO Failed
CALL :CopySettings "%BROKER_SETTINGS%" "%BROKER_PUBLISH%\Settings.ini"

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

echo.
echo Registering Windows Services...
CALL :InstallService "LeakTestMqttBroker" "Leak Test MQTT Broker" "PT. Yanmar Leak Test MQTT Broker Service" "%ROOT_DIR%\%BROKER_PUBLISH%\MqttBrokerService.exe"
IF ERRORLEVEL 1 GOTO Failed
CALL :InstallService "LeakTestWorker" "Leak Test Worker" "PT. Yanmar Leak Test MQTT Worker" "%ROOT_DIR%\%WORKER_PUBLISH%\LeakTestWorker.exe"
IF ERRORLEVEL 1 GOTO Failed

echo.
echo Starting services...
CALL :StartService "LeakTestMqttBroker"
CALL :StartService "LeakTestWorker"

echo.
echo ============================================================
echo Pull and install completed successfully.
echo Services:
echo   - LeakTestMqttBroker
echo   - LeakTestWorker
echo API has been published for IIS:
echo   %API_PUBLISH%
echo Frontend has been built. Run frontend with: cd Frontend ^&^& npm run start
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

:InstallService
SET "SERVICE_NAME=%~1"
SET "DISPLAY_NAME=%~2"
SET "DESCRIPTION=%~3"
SET "EXE_PATH=%~4"

IF NOT EXIST "%EXE_PATH%" (
    echo ERROR: Service executable was not found:
    echo %EXE_PATH%
    exit /b 1
)

sc.exe query "%SERVICE_NAME%" >nul 2>&1
IF NOT ERRORLEVEL 1 (
    echo Deleting existing %SERVICE_NAME%...
    sc.exe delete "%SERVICE_NAME%" >nul
    IF ERRORLEVEL 1 (
        echo ERROR: Failed to delete existing service %SERVICE_NAME%.
        exit /b 1
    )
    timeout /t 2 /nobreak >nul
)

echo Creating %SERVICE_NAME%...
sc.exe create "%SERVICE_NAME%" binPath= "\"%EXE_PATH%\"" start= auto DisplayName= "%DISPLAY_NAME%"
IF ERRORLEVEL 1 (
    echo ERROR: Failed to create service %SERVICE_NAME%.
    exit /b 1
)

sc.exe description "%SERVICE_NAME%" "%DESCRIPTION%" >nul
sc.exe config "%SERVICE_NAME%" start= auto >nul
sc.exe failure "%SERVICE_NAME%" reset= 86400 actions= restart/5000/restart/10000/restart/30000 >nul
sc.exe failureflag "%SERVICE_NAME%" 1 >nul 2>&1
exit /b 0

:StartService
SET "SERVICE_NAME=%~1"
echo Starting %SERVICE_NAME%...
sc.exe start "%SERVICE_NAME%"
IF ERRORLEVEL 1 (
    echo WARNING: Failed to start %SERVICE_NAME%. Check Windows Event Viewer.
)
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
echo Pull and install failed.
echo Please check the error message above.
echo ============================================================
pause
exit /b 1
