# RungerHeimdall

> Heimdall watches the bridge. We watch your architecture.

RungerHeimdall is a compiler-level guard for .NET: it turns architecture rules and hidden allocations into diagnostics you see in the IDE and in CI.

**Status: pre-alpha (M0).** The repository currently contains the solution skeleton, the host-independent rule engine, and the first architecture rule. Nothing is published to NuGet yet.

## What it will do

| Capability | Status | Target |
|------------|--------|--------|
| Architecture guard (layer dependencies, forbidden types) | In progress | M1 |
| Hidden allocation detection (D-class rules) | Skeleton | M1 |
| Hot-path aware performance rules | Planned | M2 |
| CLI: analyze, baseline, doctor, report | Planned | M2 |
| Vertical slice source generator | Planned | M3 |
| MCP server for AI tooling | Planned | M3 |

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

## License

MIT. See [LICENSE](LICENSE).