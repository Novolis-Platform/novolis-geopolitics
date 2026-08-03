# GeoPolity layering (Economy-aligned)

Mirror of [Novolis.Economy.Core](../../novolis-economy/src/Novolis.Economy.Core/README.md) boundaries: **fundamentals settle stocks**; **simulation composes**; **AI/apps enqueue policy only**.

```text
┌─────────────────────────────────────────────────────────┐
│  novolis-apps/src/GeoPolity  — UI / headless (presentation) │
└────────────────────────────▲────────────────────────────┘
                             │ observes WorldState + Stats
┌────────────────────────────┴────────────────────────────┐
│  Novolis.Geopolitics.Simulation  — middle layer          │
│  Day tick → month: Trade → CivicPipeline → TreatyEffects │
│            → PolityAi (policy knobs only)                │
│  DiplomaticRules, CombatResolver, WorldBootstrap         │
└────────────────────────────▲────────────────────────────┘
                             │ calls pure engines
┌────────────────────────────┴────────────────────────────┐
│  Novolis.Geopolitics.Core  — fundamentals                │
│  Polity (State) + StateFiscalPolicy + CivicState         │
│  CivicEngine.ApplyMonth (tax/transfers/infra/mil/stocks) │
│  GovernmentRules, WorldState, seed, resources, treaties  │
└─────────────────────────────────────────────────────────┘
```

## Economy relationships (mapped)

| Economy Core | GeoPolity Core |
|--------------|----------------|
| State + `StatePolicy` (tax / transfer) | Polity + `StateFiscalPolicy` |
| Period fiscal settlement | `CivicEngine.ApplyMonth` |
| Household welfare / capacity | Transfers → approval; infra → `HumanDevelopment` |
| Solvency / liquidity stress | Treasury &lt; 0 → legitimacy/stability hit |
| Resource holdings / shortages | `ResourceVector` Balance → approval & growth drag |
| Out of Core: diplomacy, combat | Stay in Simulation |

## Month order (why)

1. **Trade** — domestic + CM + world market write `Balance` (shortages visible).
2. **Civic** — tax collection capacity, transfers, spend, civic stocks, GDP growth.
3. **Treaty effects** — EP GDP, research catch-up, aid cash (nudges civic stocks lightly).
4. **AI** — adjusts `StateFiscalPolicy` / diplomacy; does **not** recompute legitimacy.

## Non-goals in Core

- UI meters that bypass stock–flow
- AI agendas or acceptance RNG
- Combat resolution
