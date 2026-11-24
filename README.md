# RungerHeimdall

<img src="https://raw.githubusercontent.com/rungerheimdall/rungerheimdall/main/brand/project/strata-128.png" width="64" height="64" alt="RungerHeimdall logo">

> Heimdall watches the bridge. We watch your architecture.

RungerHeimdall is a compiler-level guard for .NET: it turns architecture rules and hidden allocations into diagnostics you see in the IDE and in CI.

**Status: 1.1.0.** Eight architecture rules (ARCH1001–ARCH1008) and twenty performance rules (PERF1001–PERF1012, PERF2001–PERF2008) are implemented and covered by tests, with benchmarks for every measurable rule. See the [rule reference](https://rungerheimdall.github.io/rungerheimdall/rules.html) for exact behaviour and measured impact.

## Install

```bash
dotnet add package RungerHeimdall.Analyzers --version 1.1.0
```

## Quick start

1. Copy [architecture.sample.json](architecture.sample.json) to your project root as `architecture.json`.
2. Build. Diagnostics appear in the IDE and in `dotnet build`.

Without a configuration file the analyzer falls back to convention-based layer inference (`Domain`, `Application`, `Infrastructure`, `Api`, `Web`, `Endpoints`, `Core`, `Shared`, `Modules`, `Host`) and enables a small set of high-confidence rules. Explicit configuration always wins.

## Rules

| Rule | Purpose |
|------|---------|
| ARCH1001 | Layer dependency direction |
| ARCH1002 | Forbidden type reference |
| ARCH1003 | Forbidden namespace reference |
| ARCH1004 | Namespace must match folder |
| ARCH1005 | Repositories and services must be interfaces |
| ARCH1006 | Layer must not reference disallowed frameworks |
| ARCH1007 | Avoid static mutable state |
| ARCH1008 | Endpoint layer must not create infrastructure types |
| PERF1001 | Avoid empty `params` array allocation |
| PERF1002 | Avoid `Enumerable.Count()` on a countable collection |
| PERF1003 | Avoid `ToList()` immediately followed by `foreach` |
| PERF1004 | Avoid string concatenation inside loops |
| PERF1005 | Avoid capturing the loop variable in a closure |
| PERF1006 | Avoid reading the system clock on a hot path |
| PERF1007 | Avoid `Enum.HasFlag` boxing |
| PERF1008 | Avoid the default comparer for a struct without a generic interface |
| PERF1009 | Avoid `Substring` allocations for parsing and comparison |
| PERF1010 | Prefer `[GeneratedRegex]` over `new Regex` |
| PERF1011 | Avoid `foreach` over a non-generic collection of value types |
| PERF1012 | Avoid `ToLower`/`ToUpper` for comparison |
| PERF2001 | Avoid LINQ inside loops on hot paths |
| PERF2002 | Avoid `Task.Run` inside `async` methods |
| PERF2003 | Avoid `async void` |
| PERF2004 | Avoid `Task.Result` and `Task.Wait()` |
| PERF2005 | Avoid enumerating an `IEnumerable<T>` twice |
| PERF2006 | Avoid awaiting a `ValueTask` twice |
| PERF2007 | Avoid `Task.FromResult` on hot paths |
| PERF2008 | Avoid `Array.Resize` inside loops |

Architecture rules and most allocation rules default to `warning`. `PERF1001` and `PERF1007` default to `info` because the .NET 9 JIT already removes the allocation, and `PERF1006` defaults to `info` because its benefit is testability rather than speed. The context-aware rules (`PERF2001`–`PERF2003`, `PERF2005`–`PERF2008`) also default to `info`. Change severity per rule in `architecture.json`, or globally with `.editorconfig`:

```ini
dotnet_diagnostic.ARCH1001.severity = error
```

## CLI

```bash
dotnet tool install -g RungerHeimdall.Cli --version 1.1.0

heimdall init                      # create architecture.json
heimdall init --strict             # create it with error severity
heimdall analyze [path]            # print findings, --format console|json|markdown|sarif
heimdall baseline create [path]    # freeze existing violations
heimdall report [path]             # write hotpath-report.md grouped by method
heimdall doctor                    # inspect environment and configuration
```

## Configuration

`architecture.json` is validated against [architecture.v1.json](schemas/architecture.v1.json) and is picked up automatically when it sits in the project directory.

Existing codebases can freeze current violations with `heimdall-baseline.json` ([schema](schemas/heimdall-baseline.v1.json)):

```json
{
  "version": 1,
  "entries": [
    { "ruleId": "ARCH1001", "containingType": "MyApp.Domain.OrderService" },
    { "ruleId": "ARCH1002", "file": "src/Api/OrdersEndpoint.cs" }
  ]
}
```

Temporary exceptions belong in `architecture.json`. They require a `reason` and may carry an `expiresOn` date:

```json
{
  "exemptions": [
    { "scope": "type", "target": "MyApp.Infrastructure.LegacyOrderAdapter", "reason": "migration in progress", "expiresOn": "2027-06-30" }
  ]
}
```

Expired exemptions stop suppressing diagnostics and are reported as `HDL9001`.

If automatic `AdditionalFiles` injection is disabled (`RungerHeimdallAdditionalFiles=false`), add the files manually:

```xml
<ItemGroup>
  <AdditionalFiles Include="architecture.json" Condition="Exists('architecture.json')" />
  <AdditionalFiles Include="heimdall-baseline.json" Condition="Exists('heimdall-baseline.json')" />
</ItemGroup>
```

## Design

- [Technical design document](docs/design/TDD-v1.1.md)
- [ADRs](docs/adr/)

## Repository layout

```
src/
  RungerHeimdall.Engine/                 host-independent rule engine
  RungerHeimdall.Analyzers.Architecture/ architecture diagnostics
  RungerHeimdall.Analyzers.Performance/  performance diagnostics
  RungerHeimdall.Abstractions/           public attributes
  RungerHeimdall.Cli/                    heimdall command
schemas/                                 JSON schemas for configuration and baseline
tests/
  RungerHeimdall.Engine.Tests/
  RungerHeimdall.Analyzers.Architecture.Tests/
  RungerHeimdall.Analyzers.Performance.Tests/
```

## Building

```bash
dotnet build
dotnet test
```

## Compatibility

- Analyzers target `netstandard2.0` and work with Visual Studio 2022 17.8+, Rider 2024.1+, and .NET SDK 8.0+.
- Zero telemetry. The analyzers never read or transmit your code.

## License

MIT. See [LICENSE](LICENSE).