<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-geopolitics/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-geopolitics/) · [Source](https://github.com/Novolis-Platform/novolis-geopolitics)
<!-- novolis-pkg-brand:end -->

# Novolis.Geopolitics.PolicyAgents

Deterministic heuristic policy agent over a `WorldState` from `Novolis.Geopolitics.Core`.

`HeuristicPolicyAgent.RunMonth` adjusts each Polity's `StateFiscalPolicy` (tax, transfer,
infrastructure, propaganda, and military shares) in response to treasury, approval, legitimacy,
and war fatigue, proposes diplomacy through `Novolis.Geopolitics.Diplomacy`, and may declare war
through the same. It never writes `CivicState` or `Military` directly — `CivicEngine` in Core is
the only settlement authority.

Depends on Core and Diplomacy.

PackageId: `Novolis.Geopolitics.PolicyAgents` (`2026.1.*` on GitHub Packages).

## Install

```bash
dotnet add package Novolis.Geopolitics.PolicyAgents
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`).


