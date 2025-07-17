@echo off
setlocal enabledelayedexpansion

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

echo Removing SharpBridge firewall rules...

:: Set the fixed port we're using
set FIXED_PORT=21413

:: Try to delete each possible rule (ignoring errors if rule doesn't exist)
echo Removing all SharpBridge rules...

:: Remove the specific port rule (this is the main one created by firewall-secure.bat)
netsh advfirewall firewall delete rule name="SharpBridge UDP Port %FIXED_PORT%"
if %errorlevel% equ 0 (
    echo Successfully removed rule for UDP port %FIXED_PORT%
) else (
    echo Rule for UDP port %FIXED_PORT% not found or could not be removed
)

:: Just in case the original rules exist, remove them too
netsh advfirewall firewall delete rule name="SharpBridge" >nul 2>&1
netsh advfirewall firewall delete rule name="SharpBridge UDP" >nul 2>&1
netsh advfirewall firewall delete rule name="SharpBridge dotnet" >nul 2>&1

:: Look for any other SharpBridge rules
echo Looking for any other SharpBridge rules...
for /f "tokens=*" %%i in ('netsh advfirewall firewall show rule name^=all ^| findstr /i "SharpBridge"') do (
    for /f "tokens=2 delims=:" %%j in ("%%i") do (
        set rule=%%j
        set rule=!rule:~1!
        echo Removing rule: !rule!
        netsh advfirewall firewall delete rule name="!rule!" >nul 2>&1
    )
)

echo.
echo Firewall rules removal completed!
pause 