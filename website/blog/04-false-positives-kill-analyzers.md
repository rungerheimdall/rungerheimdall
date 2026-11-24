---
title: Why False Positives Kill Analyzers
date: 2026-11-24
description: The failure mode is not missing a bug. It is reporting something that is not one.
---

Most analyzers do not die because they miss violations. They die because they report things that are fine.

The sequence is predictable. A rule ships with a few false positives. Someone adds `#pragma warning disable` around the call site. A month later the suppression is everywhere and the rule is noise. Then the whole package gets removed.

## Three mechanisms that keep it honest

**Severity by confidence.** Rules backed by a benchmark and a deterministic pattern default to `Warning`. Rules that depend on runtime context default to `Info`. Rules that need a hot-path declaration only run when the declaration exists.

**Baseline.** Existing violations are frozen in `heimdall-baseline.json`. The analyzer reports only new ones. Nobody has to fix 4 000 findings on day one, and nobody gets to add a 4 001st.

**Exemptions with a reason and an expiry date.** A team can accept a specific violation, but the file requires a reason, and the entry stops suppressing when it expires. The exemption is visible in review, unlike a pragma buried in a generated file.

## The corpus test

Every rule is run against a set of real repositories before it ships. The diagnostic count is compared with the previous release. A change above five percent requires a manual review, which usually means the rule got broader by accident.

The corpus is not a benchmark. It is a false-positive detector with a large enough surface to catch the cases the unit tests missed.

## The downgrade we made

Two rules were shipped as warnings and then downgraded after measurement:

- The empty `params` array rule reported an allocation that .NET 9 removes.
- The `Enum.HasFlag` rule reported boxing that .NET 5+ eliminates as an intrinsic.

Both were correct on the IL and wrong on the machine. Keeping them as warnings would have cost more trust than the rules were worth. They are now `Info`, with the reason written in the rule description.

## What we ask of a new rule

1. A real code sample where the rule fires.
2. At least three samples where it must not fire.
3. A benchmark if the rule claims a performance cost.
4. A severity that matches the confidence, not the enthusiasm.

A rule that fails the third check is a style opinion, not a performance rule. A rule that fails the second is a liability.