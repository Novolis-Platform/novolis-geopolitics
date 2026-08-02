# Novolis.Geopolitics.Simulation

Middle layer over `WorldState`: day tick, monthly composition, rules, AI.

Depends on `Novolis.Geopolitics.Core`.

## Month pipeline

1. `TradeResolver` — CM → world market → shortages on `Polity.Balance`
2. `CivicPipeline` — context + `CivicEngine.ApplyMonth`
3. `TreatyEffects` — EP, research, aid, relation floors
4. `PolityAi` — mutates `StateFiscalPolicy` and diplomacy only

## Rules vs fundamentals

| Module | Role |
|--------|------|
| `DiplomaticRules` | Acceptance thresholds (uses Core civic/gov stocks) |
| `CivicPipeline` | Orchestrates Core civic settlement |
| `CombatResolver` | Daily war fronts |
| `PolityAi` | Policy enqueue — never invents legitimacy directly |

See [docs/layering.md](../../docs/layering.md).
