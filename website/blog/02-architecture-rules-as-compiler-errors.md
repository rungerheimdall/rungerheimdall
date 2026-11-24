---
title: We Turned Architecture Rules into Compiler Errors
date: 2026-11-24
description: How the analyzer reads layers, resolves symbols and avoids the false positives that get tools uninstalled.
---

RungerHeimdall is a Roslyn analyzer. It does not run a separate scan, does not need a test project, and does not wait for CI. The rules are evaluated while the compiler is already building the code.

This post is about the parts that were harder than expected.

## Layers are inferred or declared

The simplest useful behavior is zero configuration. If a namespace contains a segment named `Domain`, `Application`, `Infrastructure` or `Api`, the analyzer assigns it a rank. Lower rank means closer to the core. A reference from a lower rank to a higher rank is reported.

That covers most projects. For everything else there is `architecture.json`, which supports exact namespace patterns, explicit ranks and per-rule severity. When the file exists, it wins.

## Detection is not one callback

The analyzer checks two places:

- **Declarations.** Base types, interfaces, fields, properties, event types, method signatures, generic arguments and array element types.
- **Method bodies.** Object creation, invocations, field and property references, method groups, events and `typeof`.

Both feed the same rule engine, and both run through the same deduplication key so a target is reported once per containing type.

## The edge cases that decide whether the tool is trusted

A rule that misses an alias is annoying. A rule that reports an alias twice is worse. The cases that needed explicit handling:

- `using X = MyApp.Infrastructure.Foo;` must resolve through the alias.
- `global using` must not produce duplicate diagnostics.
- Generic types must compare through `OriginalDefinition`, otherwise `IRepository<Order>` and `IRepository<Customer>` look unrelated.
- `nameof(InfraType)` should not count as a dependency by default; `typeof(InfraType)` should.
- Generated code, `obj/` and designer files are skipped.
- `partial` types are aggregated by symbol, not by syntax node.
- Extension methods resolve through the declaring type, not the receiver.

Each of these has a test. None of them are interesting in a demo, and all of them are the reason a real team keeps the tool installed.

## False positives are the product risk

The first version of a rule is easy. The twentieth version is a decision about what not to report.

Two rules were downgraded after benchmarking on .NET 9:

- The empty `params` array rule measured zero allocation, because the JIT removes the array when it does not escape.
- `Enum.HasFlag` measured zero allocation and no time difference, because .NET 5+ treats it as a JIT intrinsic.

Both rules still exist. Both are now `Info`. Reporting an allocation that the runtime removed would have been worse than not reporting it at all.

## What the tool is not

It is not a security scanner, not a linter for style, and not a profiler. It does not claim to find every allocation. It finds the ones that can be proven from the compilation, and it stays quiet about the rest.