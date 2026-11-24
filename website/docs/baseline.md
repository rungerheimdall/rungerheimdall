---
sidebar_position: 4
title: Baseline
---

# Baseline

A baseline freezes existing violations so that only new code is checked.

## Generate

```bash
heimdall baseline create .
```

The command accepts a directory, a `.sln` file or a `.csproj` file. It runs the architecture and performance analyzers over every project and writes `heimdall-baseline.json` to the current directory.

> **Note.** Roslyn 4.8 cannot open `.slnx` solutions. When the target contains only `.slnx`, the command scans every `.csproj` instead.

## Format

```json
{
  "$schema": "https://rungerheimdall.github.io/rungerheimdall/schemas/heimdall-baseline.v1.json",
  "version": 1,
  "entries": [
    { "ruleId": "ARCH1001", "containingType": "MyApp.Domain.OrderService" },
    { "ruleId": "ARCH1002", "file": "src/Api/OrdersEndpoint.cs" }
  ]
}
```

| Field | Description |
|-------|-------------|
| `ruleId` | Required. |
| `containingType` | Fully qualified containing type. Suppresses every diagnostic of that rule inside the type. |
| `file` | Path relative to the project directory. Suppresses every diagnostic of that rule in the file. |

At least one of `containingType` or `file` is required.

## Current behaviour

`heimdall baseline create` writes **file-level** entries. The analyzer also supports type-level entries; they can be added by hand for finer control.

## Keeping the baseline honest

- Commit the file. It is part of the repository state, not a local cache.
- Regenerate it after large refactors. File paths change, entries go stale.
- Treat growth of the baseline as a review signal, not as a fix.
- Prefer exemptions for temporary exceptions that should expire. A baseline is for legacy code with no owner, an exemption is for code that is being migrated.

## What the baseline does not do

It does not silence a rule for new files, and it does not affect rules that were added after the baseline was generated. Run `heimdall baseline create` again after adding a new rule.