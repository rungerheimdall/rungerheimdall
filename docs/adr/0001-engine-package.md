# ADR 0001: Ship the rule engine as a standalone package

## Status

Accepted (2026-11-23)

## Context

`RungerHeimdall.Analyzers.Architecture` and `RungerHeimdall.Analyzers.Performance` share
the same rule engine: layer indexing, namespace pattern matching, configuration loading,
diagnostic factories and baseline handling.

Three packaging options were considered:

1. Merge all rules into one analyzer assembly.
2. Inline a copy of the engine into each analyzer package under a different assembly name.
3. Publish the engine as its own NuGet package and have both analyzers depend on it.

Option 1 removes the ability to install only architecture rules, and keeps growing one
assembly for every user.

Option 2 duplicates code, which directly conflicts with the single-maintainer constraint.

Option 3 lets NuGet place a single `RungerHeimdall.Engine.dll` under
`analyzers/dotnet/cs/`, shared by both analyzers, with no assembly-name collision. The CLI
can also reference the same package through its `lib/netstandard2.0` asset.

## Decision

Publish `RungerHeimdall.Engine` as a standalone package. Both analyzer packages and the
CLI depend on it.

## Consequences

- Analyzer packages must package the engine DLL under `analyzers/dotnet/cs/`.
- The engine must keep a `netstandard2.0` target and a zero-dependency policy apart from
  `System.Text.Json` (deferred until configuration parsing lands).
- Rule logic stays host-independent: no `AnalysisContext` or `Compilation` types may leak
  into the engine's public surface.