
## Local GitHub Actions Testing with `act`

### Overview
`act` is a tool that runs GitHub Actions workflows locally using Docker, allowing you to test workflows without pushing to GitHub.

### Installation

#### Windows
```bash
# Option 1: Chocolatey (Recommended)
choco install act-cli

# Option 2: Scoop
scoop install act

# Option 3: Manual download
# Download from https://github.com/nektos/act/releases
# Extract and add to PATH
```

#### Fedora Linux
```bash
# Option 1: COPR Repository (Recommended)
sudo dnf copr enable -y rubemlrm/act
sudo dnf install act

# Option 2: Manual download
wget https://github.com/nektos/act/releases/latest/download/act_Linux_x86_64.tar.gz
tar -xzf act_Linux_x86_64.tar.gz
sudo mv act /usr/local/bin/

# Option 3: Go install
go install github.com/nektos/act@latest
```

#### Prerequisites
- **Docker** - Required for running GitHub Actions environment
- **Git** - For repository operations

### Basic Usage

#### Test Installation
```bash
act --version
```

#### Run Workflows Locally
```bash
# Test push event
act push

# Test pull request
act pull_request

# Test manual workflow dispatch
act workflow_dispatch

# Dry run (see what would happen without executing)
act push --dry-run
```

#### Test with Custom Events
```bash
# Create test event file
cat > test-tag-event.json << EOF
{
  "ref": "refs/tags/v1.2.0",
  "repository": {
    "name": "sharp-bridge",
    "owner": "dimak01"
  }
}
EOF

# Run with custom event
act push -e test-tag-event.json
```

#### Set Secrets for Local Testing
```bash
# Create secrets file
cat > .secrets << EOF
SONAR_TOKEN=your_token_here
GITHUB_TOKEN=your_token_here
EOF

# Run with secrets
act push --secret-file .secrets
```

### Perfect for Testing Our Workflows

#### What Works Well
- ✅ **Build logic** - `dotnet build`, `dotnet test`
- ✅ **File operations** - Reading release notes from `Docs/ReleaseNotes/`
- ✅ **Conditional logic** - Version detection, pre-release logic
- ✅ **Environment setup** - .NET 8.0, ReportGenerator

#### What Doesn't Work
- ❌ **GitHub API calls** - Can't create real releases
- ❌ **External services** - SonarQube, etc.
- ❌ **Repository secrets** - Need to mock
- ❌ **Git operations** - Limited in Docker

### Testing Our Release Workflow

#### Test Release Notes Extraction
```bash
# Create test event for tag
echo '{"ref": "refs/tags/v1.2.0"}' > test-tag.json

# Test release workflow locally
act push -e test-tag.json --dry-run
```

#### Test Build Workflow
```bash
# Test CI workflow
act pull_request

# Test with specific branch
act push -e test-push.json
```

### Benefits for Our Project

1. **Test workflow logic** without creating real tags
2. **Validate release notes** extraction before pushing
3. **Debug workflow issues** locally
4. **Test different scenarios** with custom events
5. **Faster iteration** - no need to push to GitHub

### Example Test Scenarios

#### Test Missing Release Notes
```bash
# Create event for version without release notes
echo '{"ref": "refs/tags/v1.2.0"}' > test-missing-notes.json
act push -e test-missing-notes.json
# Should fail with "No release notes file found"
```

#### Test Pre-release Logic
```bash
# Test alpha release
echo '{"ref": "refs/tags/v1.2.0-alpha.1"}' > test-alpha.json
act push -e test-alpha.json --dry-run
# Should detect as pre-release
```

#### Test Build with Version
```bash
# Test build workflow with version override
act push -e test-tag.json
# Should build with correct version
```

### Integration with Development Workflow

1. **Write release notes** in `Docs/ReleaseNotes/v1.2.0.md`
2. **Test locally** with `act push -e test-tag.json`
3. **Fix issues** if workflow fails
4. **Push to GitHub** when ready
5. **Create real tag** to trigger actual release

This approach provides a much more robust testing strategy for our release process!
