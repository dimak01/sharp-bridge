@echo off
setlocal enabledelayedexpansion

REM Cleanup firewall rule for SharpBridge UDP port
REM This script removes the specific firewall rule created by setup-firewall.bat

:: BatchGotAdmin
:-------------------------------------
REM  --> Check for permissions
    IF "%PROCESSOR_ARCHITECTURE%" EQU "amd64" (
>nul 2>&1 "%SYSTEMROOT%\SysWOW64\cacls.exe" "%SYSTEMROOT%\SysWOW64\config\system"
) ELSE (
>nul 2>&1 "%SYSTEMROOT%\system32\cacls.exe" "%SYSTEMROOT%\system32\config\system"
)

REM --> If error flag set, we do not have admin.
if '%errorlevel%' NEQ '0' (
    echo Requesting administrative privileges...
    goto UACPrompt
) else ( goto gotAdmin )

:UACPrompt
    echo Set UAC = CreateObject^("Shell.Application"^) > "%temp%\getadmin.vbs"
    set params= %*
    echo UAC.ShellExecute "cmd.exe", "/c ""%~s0"" %params:"=""%", "", "runas", 1 >> "%temp%\getadmin.vbs"

    "%temp%\getadmin.vbs"
    del "%temp%\getadmin.vbs"
    exit /B

:gotAdmin
    pushd "%CD%"
    CD /D "%~dp0"
:--------------------------------------

set "RULE_NAME=SharpBridge UDP Port 28964"
set "PORT=28964"

echo Cleaning up firewall rule for SharpBridge...
echo.

REM Check if dry run mode is enabled
if "%1"=="--dry-run" (
    echo [DRY RUN MODE] - No changes will be made
    echo.
    set "DRY_RUN=1"
) else (
    set "DRY_RUN=0"
)

REM Check if the specific rule exists
netsh advfirewall firewall show rule name="%RULE_NAME%" >nul 2>&1
if %errorlevel% neq 0 (
    echo Rule "%RULE_NAME%" not found. Nothing to clean up.
    goto :end
)

REM Remove the specific rule
if "%DRY_RUN%"=="1" (
    echo [DRY RUN] Would remove rule: %RULE_NAME%
) else (
    echo Removing firewall rule: %RULE_NAME%
    netsh advfirewall firewall delete rule name="%RULE_NAME%"
    if !errorlevel! equ 0 (
        echo Successfully removed firewall rule.
    ) else (
        echo Failed to remove firewall rule. You may need to run as administrator.
        exit /b 1
    )
)

echo.
echo Firewall cleanup completed.

:end
pause
endlocal 