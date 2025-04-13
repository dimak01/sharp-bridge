@echo off
echo Installing required tools for code coverage reporting...

REM Install the .NET global tool for ReportGenerator
dotnet tool install -g dotnet-reportgenerator-globaltool

REM Add coverlet.collector to the test project if not already there
dotnet add Tests/Tests.csproj package coverlet.collector

echo Setup complete! You can now run 'run-coverage.bat' to generate coverage reports. 