---
sidebar_position: 6
title: CI
---

# CI

The analyzers run during ordinary compilation, so no extra tool is needed in the pipeline.

## GitHub Actions

```yaml
name: build

on: [push, pull_request]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x
      - run: dotnet restore
      - run: dotnet build --no-restore -warnaserror
```

`-warnaserror` turns every analyzer warning into a build failure. Pair it with a baseline so that only new violations fail the build.

## SARIF output

The CLI writes SARIF 2.1.0, which is what GitHub code scanning expects:

```bash
heimdall analyze . --format sarif > results.sarif
```

Upload the file to GitHub code scanning:

```yaml
- uses: github/codeql-action/upload-sarif@v3
  with:
    sarif_file: results.sarif
```

`dotnet build -p:ErrorLog=results.sarif` produces SARIF 1.0.0, which code scanning rejects. Use the CLI form when you need to upload.

## GitLab

```yaml
build:
  image: mcr.microsoft.com/dotnet/sdk:8.0
  script:
    - dotnet build -warnaserror
```

## Azure Pipelines

```yaml
- task: DotNetCoreCLI@2
  inputs:
    command: build
    arguments: '--configuration Release -warnaserror'
```

## Severity in CI only

Some teams keep rules as warnings locally and errors in CI:

```ini
# .editorconfig
dotnet_diagnostic.ARCH1001.severity = warning
```

```xml
<!-- Directory.Build.props, imported only when CI is set -->
<PropertyGroup Condition="'$(CI)' == 'true'">
  <WarningsAsErrors>ARCH1001;ARCH1002</WarningsAsErrors>
</PropertyGroup>
```

## Baseline in CI

Regenerate and compare:

```bash
heimdall baseline create .
git diff --exit-code heimdall-baseline.json
```

This fails the build when a change introduces new violations or changes the baseline. Review the diff, then commit it deliberately.