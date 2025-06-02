## CI/CD Strategy Overview for .NET App

### Goals

* Consistent builds across local and CI environments
* Reusable, single-source test and build logic
* Coverage and test result visibility in CI
* Native debugging support (Visual Studio)
* Minimal duplication of scripts or config

---

### Build Strategy

* All builds and tests are run inside Docker containers (multi-stage).
* Primary Dockerfile defines full pipeline: restore, build, test, coverage, publish.
* Developers do not run `dotnet build` or `dotnet test` directly.
* Thin platform-specific wrappers (`build.sh`, `build.ps1`) optionally wrap Docker commands for convenience.

---

### Local Development Workflow

#### Visual Studio (Windows)

* Standard F5/debug workflow uses native build.
* Post-build event runs Docker build to validate parity with CI:

  * Ensures Docker-based build completes successfully.
  * Does **not** block local development.
  * Logs validation results to a file or output window.

#### Cursor (VS Code derivative)

* Run tests using built-in test runner (native `dotnet test`).
* Full test validation + coverage is done via `./test.sh`:

  * Runs Docker-based test stage
  * Outputs `testresults.trx` and `coverage.cobertura.xml`
  * Results can be consumed by extensions like Coverage Gutters

---

### CI Workflow (GitHub Actions)

* Reuse same Dockerfile used locally.
* Workflow steps:

  1. Checkout repo
  2. Run `./test.sh`
  3. Upload `testresults` folder as artifact
  4. Parse `.trx` file to report unit test results
  5. Fail build on test failure
* Optional: use `dorny/test-reporter` or similar for nice test summary

---

### Artifacts & Coverage

* Test results: `testresults/testresults.trx`
* Coverage: `testresults/coverage.cobertura.xml`
* Artifact output from publish stage: `/app/out/` in container

---

### Optional Enhancements

* Add `pre-push` git hook to run `test.sh`
* Add `.global.json` to pin SDK version
* Add parameters to Dockerfile (e.g. runtime ID, configuration)
* Use `just` or `Makefile` as cross-platform wrapper alternative

---

### Summary

* Native dev remains smooth (F5, test runner, Cursor integration)
* Docker is source of truth for builds + validation
* CI and local runs are fully consistent
* Coverage and test logs are always exported and available
* Strategy minimizes duplication, supports cross-platform teams
