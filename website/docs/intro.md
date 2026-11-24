---
sidebar_position: 1
title: Overview
---

# RungerHeimdall

> Heimdall watches the bridge. We watch your architecture.

RungerHeimdall is a compiler-level guard for .NET. It turns architecture rules and hidden allocations into diagnostics that appear in your IDE and in `dotnet build`.

The analyzers target `netstandard2.0`, work with Visual Studio 2022 17.8+, Rider 2024.1+ and .NET SDK 8.0+, and never read or transmit your source code.

## What it does

**Architecture guard.** Declare layers in `architecture.json` and the analyzer enforces them. Domain cannot reference infrastructure. Endpoints cannot construct a `DbContext`. Repositories must be interfaces. The rules run while you type, so a boundary violation never reaches a pull request.

**Allocation detection.** A set of deterministic rules catches allocations the compiler will not warn about: empty `params` arrays, `ToList()` immediately followed by `foreach`, string concatenation inside loops, `Enum.HasFlag` boxing, and more.

**Incremental adoption.** Existing codebases can freeze current violations in `heimdall-baseline.json`. The analyzer then reports only new violations, and temporary exceptions can be declared with a required reason and an optional expiry date.

## Rule families

| Family | Rules | Purpose |
|--------|-------|---------|
| Architecture | ARCH1001–ARCH1008 | Layer boundaries, forbidden types and namespaces, folder/namespace consistency, interface requirements |
| Allocation | PERF1001, PERF1002, PERF1003, PERF1004, PERF1007, PERF1009, PERF1010, PERF1012 | Deterministic allocation patterns |
| Context-aware | PERF2001–PERF2008 | LINQ in loops, `async` misuse, blocking on tasks, repeated enumeration |

Architecture rules and most allocation rules default to `warning`. `PERF1001` and `PERF1007` default to `info` because the .NET 9 JIT already removes the allocation in the common case; the context-aware rules also default to `info` because they depend on call-site context. Severity is configurable per rule or through `.editorconfig`.

## Next steps

- [Get started](getting-started.md) — install and see your first diagnostic in under a minute.
- [Architecture rules](rules/architecture.md) — the full rule reference.
- [CLI](cli.md) — `heimdall init`, `baseline create`, `doctor`.