@echo off
echo Running tests with code coverage...

REM Run tests with coverage and collect results
dotnet test Tests/Tests.csproj --collect:"XPlat Code Coverage"

REM Find the latest coverage report directory
FOR /F "delims=" %%i IN ('dir /b /ad /o-d TestResults\*') DO (
    SET "latest_dir=%%i"
    GOTO :found_dir
)

:found_dir
echo Latest coverage directory: %latest_dir%

REM Generate HTML report using ReportGenerator
echo Generating HTML report...
reportgenerator -reports:"TestResults/%latest_dir%/coverage.cobertura.xml" -targetdir:"TestResults/CoverageReport" -reporttypes:Html

echo Opening coverage report...
start "" "TestResults\CoverageReport\index.html"

echo Coverage report generated at TestResults\CoverageReport\index.html 