<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-geopolitics">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Geopolitics.Simulation

Composition root over `WorldState`: `WorldSimulation` runs the day tick and wires the engine
packages together at each month boundary.

Depends on Core, Conflict, Diplomacy, Trade, PolicyAgents, and Scenarios.

## Month pipeline

1. `TradeClearing.RunMonth` (Trade) — domestic → common market → world market → shortages on `Polity.Balance`
2. `CivicPipeline.RunMonth` (this package) — builds `CivicEngine.MonthContext` and calls Core's `CivicEngine.ApplyMonth`
3. `TreatyEffects.RunMonth` (Diplomacy) — economic partnership GDP, research catch-up, aid, alliance relation floors
4. `HeuristicPolicyAgent.RunMonth` (PolicyAgents) — mutates `StateFiscalPolicy` and proposes diplomacy only

Every day, `WorldSimulation` also expires treaties, drifts bilateral relations, and resolves one
war-front battle per active war through `ConflictResolver` (Conflict).

## What's here vs elsewhere

| Module | Role |
|--------|------|
| `WorldSimulation` | Day tick, month-boundary orchestration, `Telemetry` (`WorldTelemetry`) |
| `CivicPipeline` | Builds per-Polity context and calls Core's `CivicEngine.ApplyMonth` — the only orchestration logic that lives in this package rather than an engine package |

Everything else — trade clearing, diplomacy, conflict, policy agendas, world generation — lives
in its own package. See [docs/layering.md](../../docs/layering.md).

## Install

```bash
dotnet add package Novolis.Geopolitics.Simulation
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`).


