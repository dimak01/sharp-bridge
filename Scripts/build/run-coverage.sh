#!/bin/bash
echo "Running tests with code coverage..."

# Create TestResults directory if it doesn't exist
mkdir -p TestResults

# Run tests with coverage and collect results with explicit output directory
dotnet test Tests/Tests.csproj --collect:"XPlat Code Coverage" --results-directory:TestResults

# Find the latest coverage report directory
latest_dir=$(ls -td TestResults/*/ | head -1)
echo "Latest coverage directory: $latest_dir"

# Generate HTML report using ReportGenerator
echo "Generating HTML report..."
reportgenerator -reports:"${latest_dir}coverage.cobertura.xml" -targetdir:"TestResults/CoverageReport" -reporttypes:Html

echo "Opening coverage report..."
# Using xdg-open which is the standard way to open files in Linux
xdg-open "TestResults/CoverageReport/index.html"

echo "Coverage report generated at TestResults/CoverageReport/index.html" 