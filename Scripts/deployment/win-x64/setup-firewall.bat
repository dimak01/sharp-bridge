@echo off

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

echo Setting up secure firewall rules for SharpBridge...

:: Get the current directory (where the application is located)
set APP_PATH=%~dp0

:: Check if we're running as a published executable or through dotnet
if exist "%APP_PATH%SharpBridge.exe" (
    set PROGRAM_PATH=%APP_PATH%SharpBridge.exe
    echo Found standalone executable at %PROGRAM_PATH%
) else (
    echo No standalone executable found, assuming dotnet runtime execution
    set PROGRAM_PATH=%ProgramFiles%\dotnet\dotnet.exe
)

:: Using the fixed port - 28964 (matches application default)
set LOCAL_PORT=28964
echo Using static port %LOCAL_PORT% for firewall rule

echo Creating minimal firewall rule for UDP port %LOCAL_PORT% only...

:: Create a rule specifically for the UDP port the application is listening on
:: This is much more secure than allowing all inbound traffic
if "%PROGRAM_PATH%" == "%ProgramFiles%\dotnet\dotnet.exe" (
    :: For dotnet runtime, create a localport rule for the specific UDP port
    netsh advfirewall firewall add rule name="SharpBridge UDP Port %LOCAL_PORT%" dir=in action=allow protocol=UDP localport=%LOCAL_PORT% enable=yes profile=private,public
    echo Added rule for dotnet runtime: UDP port %LOCAL_PORT% (private and public networks)
) else (
    :: For standalone executable, create a program+localport rule for the specific UDP port
    netsh advfirewall firewall add rule name="SharpBridge UDP Port %LOCAL_PORT%" dir=in action=allow program="%PROGRAM_PATH%" protocol=UDP localport=%LOCAL_PORT% enable=yes profile=private,public
    echo Added rule for executable: %PROGRAM_PATH% UDP port %LOCAL_PORT% (private and public networks)
)

echo.
echo IMPORTANT SECURITY NOTES:
echo 1. Only the specific UDP port %LOCAL_PORT% is now open, not all traffic to the application
echo 2. The rule applies to both private and public networks
echo 3. To remove this rule, use cleanup-firewall.bat or run: netsh advfirewall firewall delete rule name="SharpBridge UDP Port %LOCAL_PORT%"
echo.

echo Secure firewall rule added successfully!
pause 