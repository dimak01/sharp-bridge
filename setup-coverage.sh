#!/bin/bash
echo "Installing required tools for code coverage reporting..."

# Install the .NET global tool for ReportGenerator
dotnet tool install -g dotnet-reportgenerator-globaltool

# Add coverlet.collector to the test project if not already there
dotnet add Tests/Tests.csproj package coverlet.collector

echo "Setup complete! You can now run './run-coverage.sh' to generate coverage reports."

# Make run-coverage.sh executable if it exists
if [ -f "run-coverage.sh" ]; then
    chmod +x run-coverage.sh
fi

# Make this script executable
chmod +x setup-coverage.sh 