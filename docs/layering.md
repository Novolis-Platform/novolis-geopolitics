# Layering (Economy-aligned academic split)

`novolis-geopolitics` follows the same layering discipline as
[`Novolis.Economy`](../../novolis-economy/src/Novolis.Economy.Core/README.md): a pure kernel that
settles stocks from policy, a set of independent engine packages that each own one causal domain,
and a composition root that wires them together into a day/month tick.

```text
┌───────────────────────────────────────────────────────────────────────┐
│  novolis-apps/src/GeoPolity  — UI / headless (presentation)           │
└──────────────────────────────────▲────────────────────────────────────┘
                                    │ observes WorldState + WorldSimulation.Telemetry
┌──────────────────────────────────┴────────────────────────────────────┐
│  Novolis.Geopolitics.Simulation  — composition root                   │
│  Day tick → month: Trade → CivicPipeline → TreatyEffects → PolicyAgent │
└──────┬───────────┬───────────┬───────────┬───────────┬────────────────┘
       │            │           │           │           │
       ▼            ▼           ▼           ▼           ▼
┌───────────┐ ┌───────────┐ ┌────────┐ ┌────────────┐ ┌───────────┐
│ Conflict  │ │ Diplomacy │ │ Trade  │ │PolicyAgents│ │ Scenarios │
│ (battles) │ │(treaties, │ │(clear- │ │(heuristic  │ │(procedural│
│           │ │ orgs, war)│ │  ing)  │ │ policy)    │ │ world, seed)│
└─────┬─────┘ └─────┬─────┘ └───┬────┘ └──────┬─────┘ └─────┬─────┘
      │             │           │             │             │ (also → Diplomacy)
      └─────────────┴───────────┴─────────────┴─────────────┘
                                 │ calls pure settlement
                    ┌────────────┴────────────┐
                    │  Novolis.Geopolitics.Core │
                    │  Polity + StateFiscalPolicy + CivicState │
                    │  CivicEngine.ApplyMonth (the only settlement) │
                    │  GovernmentRules, WorldState, WorldTelemetry, │
                    │  Provinces, relations, treaties, wars (records) │
                    └───────────────────────────┘
```

## Why five engine packages instead of one

Each engine package owns exactly one causal domain and depends on nothing but Core (PolicyAgents
also depends on Diplomacy, to propose treaties and wars):

| Package | Causal domain | Depends on |
|---------|---------------|------------|
| `Novolis.Geopolitics.Conflict` | Daily battle resolution over war fronts | Core |
| `Novolis.Geopolitics.Diplomacy` | Treaty/org lifecycle, acceptance rules, monthly treaty effects | Core |
| `Novolis.Geopolitics.Trade` | Domestic production, common-market and world-market clearing | Core |
| `Novolis.Geopolitics.PolicyAgents` | Heuristic fiscal-policy adjustment and diplomacy proposals | Core, Diplomacy |
| `Novolis.Geopolitics.Scenarios` | Procedural world generation, seed schema, institution seeding | Core, Diplomacy |

None of these packages depends on `Simulation`. `Simulation` depends on all of them and on Core,
and its only original logic is `CivicPipeline` (context-building for `CivicEngine`) plus the day
tick that decides *when* each engine runs.

This means any engine package can be unit-tested, versioned, or replaced independently, exactly
as `Novolis.Economy.Production` or `Novolis.Economy.Logistics` can be tested independently of
`Novolis.Economy.Simulation`.

## Economy relationships (mapped)

| Economy Core | GeoPolity Core |
|--------------|----------------|
| State + `StatePolicy` (tax / transfer) | `Polity` + `StateFiscalPolicy` |
| Period fiscal settlement | `CivicEngine.ApplyMonth` |
| Household welfare / capacity | Transfers → approval; infrastructure → `HumanDevelopment` |
| Solvency / liquidity stress | `Treasury` < 0 → legitimacy/stability penalty |
| Resource holdings / shortages | `ResourceVector` `Balance` → approval and growth drag |
| Out of Core: production ops, logistics, markets | Out of Core: conflict, diplomacy, trade, policy agendas |

## Month order (why)

1. **Trade** (`TradeClearing.RunMonth`) — domestic + common-market + world-market write `Balance`
   so shortages are visible before civic settlement.
2. **Civic** (`CivicPipeline.RunMonth` → `CivicEngine.ApplyMonth`) — tax collection capacity,
   transfers, spend, civic stocks, GDP growth.
3. **Treaty effects** (`TreatyEffects.RunMonth`) — economic-partnership GDP, research catch-up,
   aid transfers, alliance relation floor. These nudge civic stocks lightly but do not replace
   settlement.
4. **Policy agents** (`HeuristicPolicyAgent.RunMonth`) — adjusts `StateFiscalPolicy` and proposes
   diplomacy for next period; does not recompute this period's legitimacy.

Daily (not monthly): treaty expiry, bilateral-relation drift, and one `ConflictResolver` battle
attempt per active war.

## Non-goals in Core

- UI meters that bypass stock–flow settlement
- Diplomatic acceptance heuristics or war-declaration chain reactions
- Combat resolution
- Resource-market clearing
- Policy heuristics or interactive control
- Procedural generation or seed data

Each of these lives in exactly one package above Core, and only one.
