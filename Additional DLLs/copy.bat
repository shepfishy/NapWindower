@echo off
setlocal enabledelayedexpansion

set "SOURCE_DIR=%~dp0"
set "DEST_DIR=C:\Program Files (x86)\NAP Locked down browser"

echo Copying patched DLLs from "%SOURCE_DIR%" to "%DEST_DIR%"...
echo.

:: Check if running as administrator
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo this script requires administrator privileges.
    echo Requesting administrator access...
    powershell -Command "Start-Process cmd -ArgumentList '/c cd /d \"%~dp0\" && \"%~nx0\"' -Verb RunAs"
    exit /b
)

:: Check if destination exists
if not exist "%DEST_DIR%" (
    echo Error: nap installation directory not found at "%DEST_DIR%"
    echo Verify installation path and try again.
    pause
    exit /b 1
)

:: Stop the NAP service and kill the process
echo Stopping NAP Locked down browser Service...
net stop "NAPLDBService" >nul 2>&1
if %errorlevel% equ 0 (
    echo NAPLDBService stopped
) else (
    echo NAPLDBService not running or already stopped
)

echo Terminating JanisonReplayService.exe...
taskkill /f /im "JanisonReplayService.exe" >nul 2>&1
if %errorlevel% equ 0 (
    echo JanisonReplayService.exe terminated
) else (
    echo JanisonReplayService.exe not running or already terminated
)
timeout /t 2 /nobreak >nul

:: Copy only DLL files
for %%f in ("%SOURCE_DIR%*.dll") do (
    echo Installing: %%~nxf...
    copy "%%f" "%DEST_DIR%\" >nul
    if !errorlevel! neq 0 (
        echo   Error: Failed to copy %%~nxf
        set "COPY_FAILED=1"
    ) else (
        echo   Success: %%~nxf installed
    )
)

if defined COPY_FAILED (
    echo.
    echo Some files failed to copy.
) else (
    echo.
    echo All patched DLLs copied successfully!
    echo.
    echo To finish installation, your machine needs a restart.
    echo Restarting system in 10 seconds...
    timeout /t 10 /nobreak
    shutdown /r /t 0
)

echo.
pause