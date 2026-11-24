# RungerHeimdall

> Heimdall watches the bridge. We watch your architecture.

RungerHeimdall is a compiler-level guard for .NET: it turns architecture rules and hidden allocations into diagnostics you see in the IDE and in CI.

**Status: pre-alpha.** Architecture rules ARCH1001–ARCH1008 are implemented. The performance analyzer currently ships PERF1001, PERF1002, PERF1007 and PERF1012. Nothing is stable yet.

## Install

```bash
dotnet add package RungerHeimdall.Analyzers --version 0.1.0-preview.2
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
| PERF1002 | Avoid `Enumerable.Count()` on a countable collection |

All rules default to `warning`. Change severity per rule in `architecture.json`, or globally with `.editorconfig`:

```ini
dotnet_diagnostic.ARCH1001.severity = error
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