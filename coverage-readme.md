# Code Coverage Setup for SharpBridge

This document explains how to use the code coverage tools set up for this project.

## One-Time Setup

1. Run the setup script:
   ```
   .\setup-coverage.bat
   ```

   This will:
   - Install ReportGenerator as a global .NET tool
   - Add coverlet.collector to the Tests project

## Running Coverage

Simply run the coverage script:
```
.\run-coverage.bat
```

This will:
- Run the tests with coverage collection enabled
- Generate an HTML report
- Open the report in your default browser

## Reading the HTML Report

The HTML report provides several views:
- Summary of overall coverage
- Class-by-class breakdown
- Line-by-line coverage for each file
- Branch coverage statistics

Files highlighted in red have lower coverage and should be prioritized for additional tests.

## How It Works

1. The `coverlet.collector` package is used to collect coverage data during test execution
2. The XPlat Code Coverage collector generates XML coverage data in Cobertura format
3. ReportGenerator converts the XML data into a readable HTML report

## Using in CI/CD

For CI/CD integration, you can use these commands:

```bash
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:"TestResults/**/*.cobertura.xml" -targetdir:"TestResults/CoverageReport" -reporttypes:Html
```

## Reference

For more information, see:
- [ReportGenerator Documentation](https://github.com/danielpalme/ReportGenerator)
- [Coverlet Documentation](https://github.com/coverlet-coverage/coverlet)
- [Microsoft Docs: Use code coverage for unit testing](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-code-coverage) 