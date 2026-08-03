<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-geopolitics">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Geopolitics.Scenarios

Original fiction world generation and seed loading for `Novolis.Geopolitics.Core`.

| Type | Role |
|------|------|
| `ProceduralWorldGenerator` | Deterministic ~200-polity, ~1k-province world: continent grids, cross-polity borders, land bridges, resource-weight rolls |
| `WorldSeedDto` / `WorldSeedLoader` | JSON seed schema and `WorldState` round-trip (`ToWorldState` / `FromWorldState`) |
| `DefaultWorld` | Loads the embedded default seed, falling back to procedural generation if absent |
| `InstitutionSeeder` | Deterministic post-load setup of continental forums, defence pacts, FTAs, customs unions, and research councils |

The generator produces **original fiction geography** — not derived from any proprietary map or
dataset. See [docs/seed-attribution.md](../../docs/seed-attribution.md).

Depends on Core and Diplomacy (`InstitutionSeeder` calls `DiplomaticInstruments.CreateOrg`).

PackageId: `Novolis.Geopolitics.Scenarios` (`2026.1.*` on GitHub Packages).

## Install

```bash
dotnet add package Novolis.Geopolitics.Scenarios
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (net10.0).
