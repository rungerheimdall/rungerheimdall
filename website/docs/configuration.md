---
sidebar_position: 3
title: Configuration
---

# Configuration

`architecture.json` lives next to the project file. The architecture analyzer package injects it automatically through `build/RungerHeimdall.Analyzers.Architecture.props`. Installing the meta package `RungerHeimdall.Analyzers` brings both analyzers and this injection.

If you install `RungerHeimdall.Analyzers.Performance` on its own, no injection happens: add the files manually so that baselines and exemptions reach the performance rules.

## File discovery

Automatic injection can be disabled:

```xml
<PropertyGroup>
  <RungerHeimdallAdditionalFiles>false</RungerHeimdallAdditionalFiles>
</PropertyGroup>
```

Or the files can be added manually:

```xml
<ItemGroup>
  <AdditionalFiles Include="architecture.json" Condition="Exists('architecture.json')" />
  <AdditionalFiles Include="heimdall-baseline.json" Condition="Exists('heimdall-baseline.json')" />
</ItemGroup>
```

## Schema

The configuration is validated against [`architecture.v1.json`](https://github.com/rungerheimdall/rungerheimdall/blob/main/schemas/architecture.v1.json). Add the `$schema` field for editor completion:

```json
{
  "$schema": "https://rungerheimdall.github.io/rungerheimdall/schemas/architecture.v1.json",
  "version": 1
}
```

## Top-level fields

| Field | Required | Description |
|-------|----------|-------------|
| `version` | yes | Must be `1`. Other values are rejected. |
| `layers` | no | Layer definitions used by ARCH1001 and every `in` scope. |
| `rules` | no | Rule configurations. |
| `exemptions` | no | Temporary exceptions with a required reason. |

## Layer fields

| Field | Required | Description |
|-------|----------|-------------|
| `name` | yes | Layer name referenced by `in`, `from` and `deny`. |
| `namespaces` | yes | At least one namespace pattern. A trailing `.*` matches children. |
| `rank` | no | Defaults to `0`. Lower rank is closer to the core. |

## Rule fields

| Field | Used by |
|-------|---------|
| `id`, `kind` | Every rule. `kind` selects the implementation. |
| `severity` | Every rule. |
| `in` | Scope: layer names or namespace patterns. |
| `from`, `deny` | ARCH1001 (layer names), ARCH1003 and ARCH1008 (namespace patterns). |
| `types` | ARCH1002. |
| `suffix`, `mustBeInterface` | ARCH1005. |
| `allow` | ARCH1006. |

`kind` accepts `layerDependency`, `forbiddenType`, `forbiddenNamespace`, `namespaceMatchesFolder`, `requiredInterface`, `frameworkAllowList`, `noStaticMutableState` and `endpointForbiddenCreation`.

## Severity

`severity` accepts `error`, `warning`, `suggestion`, `info` and `hidden`.

`.editorconfig` can override any rule:

```ini
dotnet_diagnostic.ARCH1001.severity = error
dotnet_diagnostic.ARCH1004.severity = none
```

## Hot path settings

The performance analyzer recognises four hot path layers:

| Layer | Source | Default |
|-------|--------|---------|
| L1 explicit | `[HotPath]` / `[ColdPath]` | on |
| L2 framework | controller actions, hosted services, SignalR hubs, Minimal API handlers, Azure Functions, MassTransit consumers | on |
| L3 structural | loops, iterators | on |
| L4 naming | `Process*`, `Handle*`, `Execute*`, `Serialize*`, `Render*` | off |

Two keys in `.editorconfig` control the optional parts:

```ini
[*.cs]
rungerheimdall_hotpath_propagation = true
rungerheimdall_hotpath_heuristic = false
```

`rungerheimdall_hotpath_propagation` treats a method as hot when it calls a hot method inside the same compilation, one layer deep. `rungerheimdall_hotpath_heuristic` enables the low-confidence naming layer. `[ColdPath]` always wins over every layer.

## Exemptions

```json
{
  "exemptions": [
    {
      "scope": "type",
      "target": "MyApp.Infrastructure.LegacyOrderAdapter",
      "reason": "migration in progress",
      "expiresOn": "2027-06-30"
    }
  ]
}
```

| Field | Required | Description |
|-------|----------|-------------|
| `scope` | yes | `type`, `namespace`, `member` or `assembly`. |
| `target` | yes | Fully qualified name, namespace prefix or assembly name. |
| `reason` | yes | Required audit note. Missing reasons make the file invalid. |
| `expiresOn` | no | `yyyy-MM-dd`. Expired exemptions stop suppressing and raise HDL9001. |

## Diagnostics for the configuration itself

| ID | Level | Meaning |
|----|-------|---------|
| HDL9001 | Info | An exemption expired. |
| HDL9002 | Info | `architecture.json` is malformed. |
| HDL9003 | Info | `heimdall-baseline.json` is malformed. |

These are informational. A broken configuration never blocks the build.