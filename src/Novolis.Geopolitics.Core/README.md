<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-geopolitics">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Geopolitics.Core

Fundamental world model + civic/fiscal period engine for full-world geopolitics.

PackageId: `Novolis.Geopolitics.Core` (`2026.1.*` on GitHub Packages after first publish).

## In boundary

| Kind | Notes |
|------|--------|
| Polity (State actor) | GDP, treasury, military, resources |
| `StateFiscalPolicy` | Tax / transfer / infra / propaganda / military shares |
| `CivicState` | Legitimacy, approval, corruption, HD, war fatigue |
| `CivicEngine` | Monthly stock–flow settlement (no AI, no UI) |
| `GovernmentRules` | Regime modifiers on tax, propaganda, alliances |
| Provinces, relations, treaties, wars, seed | Structural world data |

See [SPEC.md](SPEC.md) for the full bounded-minimum model: entities, invariants, and settlement order.

## Out of boundary

Diplomacy acceptance rules (`.Diplomacy`), combat (`.Conflict`), trade clearing (`.Trade`),
policy agents (`.PolicyAgents`), procedural generation (`.Scenarios`), and day-tick composition
(`.Simulation`) — see [docs/layering.md](../../docs/layering.md).

## Economy kinship

Same spirit as `Novolis.Economy.Core`: State policy knobs → period settlement → stocks. Geo Core does **not** import Economy packages; relationships are conceptual (tax/transfer → welfare capacity → growth).

## Install

```bash
dotnet add package Novolis.Geopolitics.Core
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`).


