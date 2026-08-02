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

## Out of boundary

Diplomacy acceptance RNG, combat, trade clearing, AI agendas — see `Novolis.Geopolitics.Simulation` and [docs/layering.md](../../docs/layering.md).

## Economy kinship

Same spirit as `Novolis.Economy.Core`: State policy knobs → period settlement → stocks. Geo Core does **not** import Economy packages; relationships are conceptual (tax/transfer → welfare capacity → growth).
