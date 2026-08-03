<!-- novolis-marketing:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-brand-transparent.svg" width="360" alt="Novolis"/>
  </a>
</p>

<p align="center">
  <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/banners/novolis-geopolitics.svg" width="100%" alt="novolis-geopolitics"/>
</p>

<p align="center">
  <strong>Full-world geopolitics engines</strong><br/>
  Homage geopolitics simulation libraries; GeoPolity hosts live in novolis-apps.
</p>

<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-geopolitics/actions"><img src="https://img.shields.io/github/actions/workflow/status/Novolis-Platform/novolis-geopolitics/merge.yml?branch=main&label=merge&logo=github" alt="merge"/></a>
  <a href="https://github.com/orgs/Novolis-Platform/packages?repo_name=novolis-geopolitics"><img src="https://img.shields.io/badge/packages-GitHub%20Packages-0a7ea3?logo=nuget" alt="packages"/></a>
  <a href="https://github.com/Novolis-Platform"><img src="https://img.shields.io/badge/org-Novolis--Platform-111827" alt="org"/></a>
</p>

<p align="center">
  <a href="https://nuget.pkg.github.com/Novolis-Platform/index.json"><code>https://nuget.pkg.github.com/Novolis-Platform/index.json</code></a>
  ·
  <a href="https://github.com/Novolis-Platform/.github/blob/main/profile/README.md">Org landing</a>
  ·
  <a href="https://github.com/Novolis-Platform/novolis-governance">Governance</a>
</p>

---
<!-- novolis-marketing:end -->
<!-- novolis-package-index:start -->
> **GitHub Packages shows this repository README on every package page** (upstream limitation).
> Open the **package README** for install and quick start — embedded in each .nupkg and linked below.

## Published packages

| Package | Install | Package README |
|---------|---------|----------------|
| `Novolis.Geopolitics.Conflict` | `dotnet add package Novolis.Geopolitics.Conflict` | [README](https://github.com/Novolis-Platform/novolis-geopolitics/blob/main/src/Novolis.Geopolitics.Conflict/README.md) |
| `Novolis.Geopolitics.Core` | `dotnet add package Novolis.Geopolitics.Core` | [README](https://github.com/Novolis-Platform/novolis-geopolitics/blob/main/src/Novolis.Geopolitics.Core/README.md) |
| `Novolis.Geopolitics.Diplomacy` | `dotnet add package Novolis.Geopolitics.Diplomacy` | [README](https://github.com/Novolis-Platform/novolis-geopolitics/blob/main/src/Novolis.Geopolitics.Diplomacy/README.md) |
| `Novolis.Geopolitics.PolicyAgents` | `dotnet add package Novolis.Geopolitics.PolicyAgents` | [README](https://github.com/Novolis-Platform/novolis-geopolitics/blob/main/src/Novolis.Geopolitics.PolicyAgents/README.md) |
| `Novolis.Geopolitics.Scenarios` | `dotnet add package Novolis.Geopolitics.Scenarios` | [README](https://github.com/Novolis-Platform/novolis-geopolitics/blob/main/src/Novolis.Geopolitics.Scenarios/README.md) |
| `Novolis.Geopolitics.Simulation` | `dotnet add package Novolis.Geopolitics.Simulation` | [README](https://github.com/Novolis-Platform/novolis-geopolitics/blob/main/src/Novolis.Geopolitics.Simulation/README.md) |
| `Novolis.Geopolitics.Trade` | `dotnet add package Novolis.Geopolitics.Trade` | [README](https://github.com/Novolis-Platform/novolis-geopolitics/blob/main/src/Novolis.Geopolitics.Trade/README.md) |

For NuGet.org and Visual Studio, the **embedded** README.md inside each package is authoritative.

<!-- novolis-package-index:end -->
# Novolis.Geopolitics

Original, open-source full-world geopolitics simulation inspired by classic grand-strategy pillars (countries, provinces, military, diplomacy, budgets).

**Spiritual homage only.** Not affiliated with GolemLabs, THQ Nordic, or SuperPower. No SuperPower 2 SDK, database, assets, or string tables are used or redistributed.

## Packages

| Package | Role |
|--------|------|
| `Novolis.Geopolitics.Core` | Fundamentals: polities, civic/fiscal engine, resources, treaties, wars, seed |
| `Novolis.Geopolitics.Simulation` | Middle layer: trade → civic → treaty effects → AI; diplomatic rules; combat |

Layering (Economy-aligned): [docs/layering.md](docs/layering.md). Supranationals: **Forum**, **DefenceAlliance**, **FreeTradeArea**, **CustomsUnion**, **ResearchForum**, **PoliticalUnion**.

See [docs/diplomacy-homage.md](docs/diplomacy-homage.md) for SP2-inspired pillar mapping (homage only).

## Product host (GeoPolity)

Interactive Avalonia / Spectre / headless session lives in **novolis-apps** (not this library repo):

```powershell
dotnet run --project d:\novolis\novolis-apps\src\GeoPolity -p:NovolisUseProjectReferences=true
dotnet run --project d:\novolis\novolis-apps\src\GeoPolity -p:NovolisUseProjectReferences=true -- --mode spectre
dotnet run --project d:\novolis\novolis-apps\src\GeoPolity -p:NovolisUseProjectReferences=true -- --headless --years 50
dotnet run --project d:\novolis\novolis-apps\src\GeoPolity -p:NovolisUseProjectReferences=true -- --headless --years 10 --agent
```

Keys (both UIs): **Space** run/pause · **1–5** speed · **Q** quit. Human session starts **paused**.

### Agent Surface

Interactive Avalonia / Spectre attach session surface `geopolity`:

- HTTP: `http://127.0.0.1:18857`
- TCP: `18858`
- Env gate: `NOVOLIS_GEOPOLITY_SESSION`
- Document: `GET http://127.0.0.1:18857/agent/document`

Actions: `pause`, `resume`, `toggle`, `setspeed` (preset 1–5), `step` (days), `advanceyears`.

Avalonia also attaches UI agent pipe `novolis-avalonia-agent-geopolity` (`Novolis.Avalonia.Agent`).

## World seed

Embedded `world-seed.json` is an original procedural layout (~200 polities, ~1k provinces) with public-domain–style continent naming. See [docs/seed-attribution.md](docs/seed-attribution.md).

## License

MIT — see package metadata.

