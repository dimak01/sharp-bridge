# Release Process

This document outlines the release process for Sharp Bridge using GitHub Actions.

## Release Strategy

We use a **tag-based release strategy** with the following workflow:

1. **Development**: Features are developed on feature branches
2. **Pre-release Testing**: Alpha/beta/RC versions for testing
3. **Release**: Stable versions for production use

## Versioning

We follow [Semantic Versioning](https://semver.org/) (SemVer):

- **MAJOR.MINOR.PATCH** (e.g., `1.2.3`)
- **Pre-release suffixes**: `-alpha.1`, `-beta.2`, `-rc.1`

## Workflow Architecture

### 1. Reusable Build Workflow (`build.yml`)

**Purpose**: Centralized build and test logic

**Features**:
- ✅ **Reusable**: Called by CI, release, and pre-release workflows
- ✅ **Configurable**: Optional version, SonarQube, and coverage threshold
- ✅ **Consistent**: Same build process across all workflows
- ✅ **Maintainable**: Single source of truth for build logic

**Inputs**:
- `version` (optional): Version to use for build
- `run-sonarqube` (default: false): Whether to run SonarQube analysis
- `enforce-coverage-threshold` (default: true): Whether to enforce 90% coverage

### 2. CI Workflow (`ci.yml`)

**Triggered by**: PRs and pushes to main branch

**Purpose**: Quality gates and continuous integration

**What it does**:
- ✅ **Builds and tests** using reusable build workflow
- ✅ **SonarQube analysis** for code quality
- ✅ **Coverage enforcement** (90% threshold)
- ✅ **Quality gates** to prevent bad code from merging

### 3. Release Workflow (`release.yml`)

**Triggered by**: Tags matching `v*` (all version tags)

**Purpose**: Create releases (both draft and public)

**Example tags**:
- **Pre-releases**: `v1.2.0-alpha.1`, `v1.2.0-beta.2`, `v1.2.0-rc.1`
- **Stable releases**: `v1.2.0`, `v1.2.1`

**What it does**:
- ✅ **Builds and tests** using reusable build workflow
- ✅ **Creates self-contained executables** for Windows (Linux/macOS commented out for future use)
- ✅ **Automatically determines release type** based on tag pattern
- ✅ **Creates draft releases** for pre-release tags (alpha/beta/rc) with appropriate warnings
- ✅ **Creates public releases** for stable tags
- ✅ **No SonarQube** (focus on distribution)
- ✅ **Requires release notes file** matching the version tag

## How to Create a Release

### Step 1: Prepare for Release

1. Ensure all tests pass locally:
   ```bash
   dotnet test
   ```

2. Update version in `SharpBridge.csproj` (optional - the workflow will update it automatically)

3. Commit and push your changes:
   ```bash
   git add .
   git commit -m "Prepare for release v1.2.0"
   git push origin main
   ```

### Step 2: Create a Pre-release (Optional)

For testing before the main release:

```bash
git tag v1.2.0-alpha.1
git push origin v1.2.0-alpha.1
```

This will trigger the pre-release workflow and create a draft release.

### Step 3: Create the Release

When ready for the final release:

```bash
git tag v1.2.0
git push origin v1.2.0
```

This will trigger the release workflow and create a public release.

## Release Artifacts

Each release includes:

- **Windows x64**: `SharpBridge-v1.2.0-win-x64.zip`

**Note**: Currently only Windows is supported. Linux and macOS publishing is commented out in the workflow for future use.

All executables are:
- Self-contained (no .NET runtime required)
- Single-file (easy to distribute)
- Trimmed (optimized size)

## Release Notes

We use a **file-based release notes system** with the following structure:

```
Docs/
├── ReleaseNotes/
│   ├── ReleaseNotesTemplate.md    # Template for new releases
│   ├── v1.2.0.md                 # Release notes for v1.2.0
│   ├── v1.1.0.md                 # Release notes for v1.1.0
│   └── v1.0.0.md                 # Release notes for v1.0.0
```

### Creating Release Notes

1. **Copy the template** for your new release:
   ```bash
   cp Docs/ReleaseNotes/ReleaseNotesTemplate.md Docs/ReleaseNotes/v1.2.0.md
   ```

2. **Edit the release notes** file:
   ```bash
   nano Docs/ReleaseNotes/v1.2.0.md
   ```

3. **Fill in the details**:
   - Replace `{VERSION}` with your actual version
   - Add new features, bug fixes, breaking changes
   - Remove sections that don't apply
   - Update download links and installation instructions
   - Customize for pre-release vs stable releases

4. **Commit the release notes**:
   ```bash
   git add Docs/ReleaseNotes/v1.2.0.md
   git commit -m "Add release notes for v1.2.0"
   git push origin main
   ```

**Note**: The `ReleaseNotesTemplate.md` file is for manual use only. The workflow will not automatically use it - you must create a specific release notes file for each version.

### Release Notes Format

Each release notes file should include:
- **What's New**: New features and improvements
- **Bug Fixes**: Issues that were resolved
- **Breaking Changes**: Incompatible changes (if any)
- **Performance Improvements**: Speed/optimization changes (if any)
- **Installation**: How to install this version
- **Configuration**: Setup instructions
- **Downloads**: Links to platform-specific files
- **Migration Guide**: Steps for upgrading (if breaking changes)

### Automated Release Notes

The workflow automatically:
1. **Looks for** `Docs/ReleaseNotes/v{version}.md`
2. **Fails the release** if no release notes file is found
3. **Creates GitHub release** with the notes

**Important**: The workflow **requires** a specific release notes file for each version. The `ReleaseNotesTemplate.md` is for manual use only. **No fallback templates are provided** - you must create proper release notes before tagging. The workflow will fail if the release notes file is missing.

## Quality Gates

The release process includes several quality checks:

1. **Build Success**: All code must compile
2. **Test Coverage**: Must maintain 90%+ coverage
3. **All Tests Pass**: No failing tests
4. **Cross-platform Build**: Must build for target platform (currently Windows)
5. **Release Notes**: Must have proper release notes file

## Rollback Process

If a release has issues:

1. **Immediate**: Delete the GitHub release and tag
2. **Code Fix**: Create a patch release (e.g., `v1.2.1`)
3. **Documentation**: Update release notes with known issues

## Best Practices

1. **Test Pre-releases**: Always test alpha/beta versions before final release
2. **Update Documentation**: Keep README and docs up to date
3. **Release Notes**: Write clear, user-friendly release notes
4. **Version Bumping**: Use semantic versioning appropriately
5. **Branch Protection**: Protect main branch to prevent direct pushes

## Automation Benefits

This setup provides:

- ✅ **Automated builds** for Windows platform
- ✅ **Automated testing** with coverage reports
- ✅ **Automated releases** with proper versioning
- ✅ **Self-contained executables** for easy distribution
- ✅ **Quality gates** to prevent bad releases
- ✅ **Consolidated release workflow** (handles both pre-releases and stable releases)
- ✅ **Cross-platform support** (Windows, with Linux/macOS commented out for future use)

## Troubleshooting

### Common Issues

1. **Build fails**: Check for compilation errors or missing dependencies
2. **Tests fail**: Ensure all tests pass locally before tagging
3. **Coverage below threshold**: Add more tests to reach 90% coverage
4. **Release notes missing**: Create release notes file before tagging
5. **Release not created**: Check GitHub Actions logs for errors

### Manual Release

If automation fails, you can create a release manually:

1. Build locally: `dotnet publish -c Release -r win-x64 --self-contained`
2. Create GitHub release manually
3. Upload built artifacts
4. Write release notes 