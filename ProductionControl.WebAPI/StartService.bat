@echo off
SETLOCAL

:: Get the directory of the batch file
SET "BAT_DIR=%~dp0"

:: Output the directory to verify
echo The batch file is running from: %BAT_DIR%

:: Example: Determine the project name and executable name
SET "PROJECT_NAME=ProductionControl.WebAPI"
SET "EXE_NAME=%PROJECT_NAME%.exe"
SET "EXE_PATH=%BAT_DIR%\%EXE_NAME%"

:: Check if the service already exists
sc query %PROJECT_NAME% >nul 2>&1
IF %ERRORLEVEL% EQU 0 (
    echo Service already exists.
    echo Starting service...
    sc start %PROJECT_NAME%
    
    :: Check if the service started successfully
    IF %ERRORLEVEL% EQU 0 (
        echo Service started successfully.
    ) ELSE (
        echo Failed to start the service. Error code: %ERRORLEVEL%
    )
) ELSE (
    echo Service does not exist. Creating service...
    sc create %PROJECT_NAME% binPath= "%EXE_PATH%" start= auto
    
    :: Check if the service was created successfully
    IF %ERRORLEVEL% EQU 0 (
        echo Service created successfully.
        
        :: Start the service
        echo Starting service...
        sc start %PROJECT_NAME%
        
        :: Check if the service started successfully
        IF %ERRORLEVEL% EQU 0 (
            echo Service started successfully.
        ) ELSE (
            echo Failed to start the service. Error code: %ERRORLEVEL%
        )
    ) ELSE (
        echo Failed to create service. Error code: %ERRORLEVEL%
    )
)

ENDLOCAL
pause
