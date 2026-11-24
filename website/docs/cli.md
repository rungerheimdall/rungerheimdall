---
sidebar_position: 5
title: CLI
---

# CLI

```bash
dotnet tool install -g RungerHeimdall.Cli --version 1.1.1
```

| Command | Purpose |
|---------|---------|
| `heimdall init` | Create `architecture.json` using the current project file name as the namespace root. |
| `heimdall init --strict` | Same, with every rule set to `error`. |
| `heimdall analyze [path]` | Run all analyzers and print findings. Use `--format console`, `json`, `markdown` or `sarif`. |
| `heimdall baseline create [path]` | Scan a directory, solution or project and write `heimdall-baseline.json`. |
| `heimdall report [path]` | Write `hotpath-report.md` with findings grouped by method. |
| `heimdall doctor` | Print runtime details and whether a configuration file was found. |
| `heimdall --version` | Print the tool version. |

## analyze

```bash
$ heimdall analyze . --format console
Shop.Domain/OrderService.cs:3:21 warning ARCH1001 'OrderService' is in layer 'Domain' but depends on 'Shop.Infrastructure.SqlRepository' in layer 'Infrastructure'. Dependencies must flow inward.

Total: 1
```

`--format markdown` prints the same findings as a table, ready for a pull request comment.

`--format sarif` emits SARIF 2.1.0 with repository-relative paths, suitable for GitHub code scanning. See [CI](ci.md).

## report

```bash
$ heimdall report .
Created /repo/hotpath-report.md
Findings: 25
Methods: 9
```

The report groups findings by method, sorts the table by the number of findings, and repeats each group with file, line and message. Paste it into a pull request or keep it in the repository as a budget. Use `--output <file>` to change the destination, or `--output -` to print to stdout.

## init

```bash
$ heimdall init --strict
Created /repo/architecture.json
Layer root: MyApp
Fill in ARCH1002 types if you want to ban specific framework types.
Next: dotnet build
```

If `architecture.json` already exists, the command writes `architecture.json.new` instead of overwriting it.

## baseline create

```bash
$ heimdall baseline create .
Created /repo/heimdall-baseline.json
Entries: 42
```

Accepted targets:

- a directory — the first `.sln` inside it is used, otherwise every `.csproj` is scanned
- a `.sln` file
- a `.csproj` file

## doctor

```bash
$ heimdall doctor
RungerHeimdall doctor
  .NET          : .NET 9.0.14
  OS            : Microsoft Windows 10.0.26200
  Architecture  : X64
  Working dir   : /repo
  architecture.json : /repo/architecture.json
```

Use it when a rule does not seem to fire. Most "the analyzer does nothing" reports come from a configuration file that is not where the package expects it.