@echo off
echo Running tests with code coverage...

REM Always navigate to project root from script location (regardless of current directory)
cd /d "%~dp0..\.."

REM Run tests with coverage and collect results with explicit output directory
dotnet test tests/Tests.csproj --collect:"XPlat Code Coverage" --results-directory:testResults --settings:coverlet.runsettings

REM Find the latest coverage report directory
SET "latest_dir="
FOR /F "delims=" %%i IN ('dir /b /ad /o-d testResults\* 2^>nul') DO (
    SET "latest_dir=%%i"
    GOTO :found_dir
)

:found_dir
if "%latest_dir%"=="" (
    echo Error: No coverage data found in testResults directory
    cd /d "%~dp0" 
    exit /b 1
)

echo Latest coverage directory: %latest_dir%

REM Generate HTML report using ReportGenerator
echo Generating HTML report...
reportgenerator -reports:"testResults/%latest_dir%/coverage.cobertura.xml" -targetdir:"testResults/CoverageReport" -reporttypes:Html

echo Opening coverage report...
start "" "testResults\CoverageReport\index.html"

echo Coverage report generated at testResults\CoverageReport\index.html

REM Navigate back to script directory so it can be run again
cd /d "%~dp0" 