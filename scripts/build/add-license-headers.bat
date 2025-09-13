@echo off
echo Adding SPDX license headers to Sharp Bridge source files...
echo.

REM Add license headers to source files
addlicense -c "Dimak@Shift" -y 2025 -l mit -s=only src/ Tests/ Program.cs ServiceRegistration.cs

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✓ License headers added successfully!
) else (
    echo.
    echo ✗ Error adding license headers. Check the output above.
    exit /b 1
)

echo.
echo Done!
