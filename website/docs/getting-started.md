---
sidebar_position: 2
title: Getting started
---

# Getting started

## Install

```bash
dotnet add package RungerHeimdall.Analyzers --version 1.1.1
```

The meta package brings both the architecture and performance analyzers. If you only want one of them:

```bash
dotnet add package RungerHeimdall.Analyzers.Architecture --version 1.1.1
dotnet add package RungerHeimdall.Analyzers.Performance --version 1.1.1
```

## Create a configuration

The CLI generates a configuration from your project name:

```bash
dotnet tool install -g RungerHeimdall.Cli --version 1.1.1
heimdall init
```

Or copy [`architecture.sample.json`](https://github.com/rungerheimdall/rungerheimdall/blob/main/architecture.sample.json) into your project root as `architecture.json`.

The analyzer picks the file up automatically when it sits next to the project file. No MSBuild changes are required.

## Build

```bash
dotnet build
```

Diagnostics appear in the IDE error list and in the build output:

```
ARCH1001: 'OrderService' is in layer 'Domain' but depends on 'SqlRepository' in layer 'Infrastructure'.
Dependencies must flow inward.
```

## Without a configuration file

If no `architecture.json` exists, the analyzer falls back to convention-based layer inference. It recognises these namespace segments:

`Domain`, `Core`, `Shared` → rank 0
`Application`, `Modules` → rank 1
`Infrastructure` → rank 2
`Api`, `Web`, `Endpoints` → rank 3
`Host` → rank 4

Only ARCH1001 runs in this mode; the other architecture rules need explicit configuration. The performance rules run without configuration, but a baseline or an exemption declared in `architecture.json` still applies to them. Explicit configuration always wins.

## Adopting on an existing codebase

50 000 violations on day one will get the analyzer uninstalled. Freeze them instead:

```bash
heimdall baseline create .
```

This writes `heimdall-baseline.json`, which the analyzer loads automatically. Existing violations stop being reported; new ones still appear.

See [Baseline](baseline.md) for details.