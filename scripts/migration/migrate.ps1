    # Sharp Bridge Code Migration Script
# Migrates codebase to new layer-based organization structure

param(
    [switch]$WhatIf = $false,
    [string]$ProjectRoot = ".",
    [string]$MigrationPath = "Scripts\migration"
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Initialize logging
$LogFile = Join-Path (Join-Path $ProjectRoot $MigrationPath) "migration-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"
$LogMessages = @()

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $LogEntry = "[$Timestamp] [$Level] $Message"
    Write-Host $LogEntry
    $LogMessages += $LogEntry
}

function Read-CsvFile {
    param([string]$FilePath)
    try {
        $FullPath = Join-Path $ProjectRoot $FilePath
        if (-not (Test-Path $FullPath)) {
            throw "CSV file not found: $FullPath"
        }
        return Import-Csv $FullPath
    }
    catch {
        Write-Log "Error reading CSV file $FilePath`: $_" "ERROR"
        throw
    }
}

function Create-FolderStructure {
    param([array]$FolderData)
    
    Write-Log "Creating folder structure..."
    
    # Group folders by level and create them level by level
    $FoldersByLevel = $FolderData | Group-Object Level | Sort-Object Name
    
    foreach ($LevelGroup in $FoldersByLevel) {
        Write-Log "Creating Level $($LevelGroup.Name) folders..."
        
        foreach ($Folder in $LevelGroup.Group) {
            $FullPath = Join-Path $ProjectRoot $Folder.FolderPath
            
            if (-not (Test-Path $FullPath)) {
                if ($WhatIf) {
                    Write-Log "Would create: $FullPath" "WHATIF"
                } else {
                    try {
                        New-Item -ItemType Directory -Path $FullPath -Force | Out-Null
                        Write-Log "Created: $FullPath"
                    }
                    catch {
                        Write-Log "Error creating folder $FullPath`: $_" "ERROR"
                        throw
                    }
                }
            } else {
                Write-Log "Folder already exists: $FullPath"
            }
        }
    }
}

function Move-FileAndUpdateNamespace {
    param([object]$Mapping)
    
    $SourcePath = Join-Path $ProjectRoot $Mapping.CurrentPath
    $TargetPath = Join-Path $ProjectRoot $Mapping.NewPath
    $TargetDir = Split-Path $TargetPath -Parent
    
    # Check if source file exists
    if (-not (Test-Path $SourcePath)) {
        Write-Log "Source file not found: $SourcePath" "WARNING"
        return
    }
    
    # Create target directory if needed
    if (-not (Test-Path $TargetDir)) {
        if ($WhatIf) {
            Write-Log "Would create directory: $TargetDir" "WHATIF"
        } else {
            New-Item -ItemType Directory -Path $TargetDir -Force | Out-Null
            Write-Log "Created directory: $TargetDir"
        }
    }
    
    # Move file
    if ($WhatIf) {
        Write-Log "Would move: $($Mapping.CurrentPath) -> $($Mapping.NewPath)" "WHATIF"
    } else {
        try {
            Move-Item -Path $SourcePath -Destination $TargetPath -Force
            Write-Log "Moved: $($Mapping.CurrentPath) -> $($Mapping.NewPath)"
        }
        catch {
            Write-Log "Error moving file $($Mapping.CurrentPath): $_" "ERROR"
            throw
        }
    }
    
    # Update namespace if it's a .cs file
    if ($Mapping.NewPath -like "*.cs") {
        if ($WhatIf) {
            Write-Log "Would update namespace in: $($Mapping.NewPath)" "WHATIF"
        } else {
            try {
                $Content = Get-Content -Path $TargetPath -Raw
                if ($Content -match "namespace\s+$([regex]::Escape($Mapping.CurrentNamespace))") {
                    $NewContent = $Content -replace "namespace\s+$([regex]::Escape($Mapping.CurrentNamespace))", "namespace $($Mapping.NewNamespace)"
                    Set-Content -Path $TargetPath -Value $NewContent -NoNewline
                    Write-Log "Updated namespace in: $($Mapping.NewPath)"
                } else {
                    Write-Log "No namespace declaration found in: $($Mapping.NewPath)" "WARNING"
                }
            }
            catch {
                Write-Log "Error updating namespace in $($Mapping.NewPath): $_" "ERROR"
                throw
            }
        }
    }
}

function Main {
    try {
        Write-Log "Starting Sharp Bridge Code Migration"
        Write-Log "Project Root: $ProjectRoot"
        Write-Log "Migration Path: $MigrationPath"
        Write-Log "WhatIf Mode: $WhatIf"
        
        # Read CSV files
        Write-Log "Reading migration mapping..."
        $MigrationMapping = Read-CsvFile "$MigrationPath\migration-mapping.csv"
        
        Write-Log "Reading folder structure..."
        $FolderStructure = Read-CsvFile "$MigrationPath\folder-structure.csv"
        
        # Validate inputs
        Write-Log "Validating inputs..."
        $MissingFiles = @()
        foreach ($Mapping in $MigrationMapping) {
            $SourcePath = Join-Path $ProjectRoot $Mapping.CurrentPath
            if (-not (Test-Path $SourcePath)) {
                $MissingFiles += $Mapping.CurrentPath
            }
        }
        
        if ($MissingFiles.Count -gt 0) {
            Write-Log "Missing source files:" "WARNING"
            foreach ($File in $MissingFiles) {
                Write-Log "  - $File" "WARNING"
            }
        }
        
        # Create folder structure
        Create-FolderStructure -FolderData $FolderStructure
        
        # Move files and update namespaces
        Write-Log "Moving files and updating namespaces..."
        $FileCount = 0
        foreach ($Mapping in $MigrationMapping) {
            $FileCount++
            Write-Log "Processing file $FileCount of $($MigrationMapping.Count): $($Mapping.CurrentPath)"
            Move-FileAndUpdateNamespace -Mapping $Mapping
        }
        
        Write-Log "Migration completed successfully!"
        Write-Log "Processed $FileCount files"
        
    }
    catch {
        Write-Log "Migration failed: $_" "ERROR"
        throw
    }
    finally {
        # Write log to file
        $LogMessages | Out-File -FilePath $LogFile -Encoding UTF8
        Write-Log "Log written to: $LogFile"
    }
}

# Run the migration
Main
