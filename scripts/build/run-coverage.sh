#!/bin/bash
echo "Running tests with code coverage..."

# Create testResults directory if it doesn't exist
mkdir -p testResults

# Run tests with coverage and collect results with explicit output directory
dotnet test tests/Tests.csproj --collect:"XPlat Code Coverage" --results-directory:testResults

# Find the latest coverage report directory
latest_dir=$(ls -td testResults/*/ | head -1)
echo "Latest coverage directory: $latest_dir"

# Generate HTML report using ReportGenerator
echo "Generating HTML report..."
reportgenerator -reports:"${latest_dir}coverage.cobertura.xml" -targetdir:"testResults/CoverageReport" -reporttypes:Html

echo "Opening coverage report..."
# Using xdg-open which is the standard way to open files in Linux
xdg-open "testResults/CoverageReport/index.html"

echo "Coverage report generated at testResults/CoverageReport/index.html" 