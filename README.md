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
