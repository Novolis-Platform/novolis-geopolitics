# Bounded Minimum Geopolitical Model

A **bounded minimum geopolitical model** is the smallest model that preserves the causal
relationships needed for meaningful state-actor behavior — fiscal capacity, territorial control,
diplomatic standing, civic legitimacy — while excluding operational detail that does not affect
those questions.

It is **bounded** because it declares what exists, what does not exist, and where settlement
occurs. It is **minimum** because each concept must explain a distinct phenomenon in state
behavior. A concept is not included merely because a grand-strategy game happens to have it.

It remains rigorous by preserving:

- ownership of territory
- fiscal stocks and flows
- civic legitimacy as a settled stock, not a free-floating meter
- regime-conditioned policy response
- resource holdings and shortages
- symmetric bilateral relations
- multilateral commitments (treaties, organizations)

The model does not need map coordinates, unit sprites, city names beyond identifiers, individual
soldiers, or UI presentation concerns. Those are composed above Core, in Simulation and in
product hosts.

---

## 1. The geopolitical boundary

The minimum model contains:

```text
Polities (State actors)
Provinces (territory)
Fiscal policy (tax / transfer / infrastructure / propaganda / military shares)
Civic stocks (legitimacy, approval, corruption, human development, war fatigue)
Military force (abstract land / air / naval magnitudes)
Resource holdings (production, consumption, balance)
Bilateral relations
Treaties and supranational organizations (structural records)
Wars (structural records)
World state (day clock, collections, invariant queries)
```

The central relationships are:

```text
A Polity is a State actor: it taxes, spends, and holds civic legitimacy.

StateFiscalPolicy defines tax/transfer/spend shares — knobs, not outcomes.

CivicEngine.ApplyMonth is the sole settlement of fiscal and civic stocks from those knobs.

Provinces are owned territory; ownership constrains a Polity's tax base and resource output.

RelationMatrix holds symmetric bilateral standing used by diplomacy and conflict.

Treaty and Supranational are structural records of commitments — Core does not decide whether
one forms; it only represents one once formed.
```

This is enough to produce budget crises, legitimacy collapse, civic-development divergence,
territorial contraction, and militarization pressure — without inventing a diplomacy AI, a
combat resolver, or a map renderer inside Core.

---

## 2. Polity — the State actor

```csharp
public sealed class Polity
{
    public required PolityId Id { get; init; }
    public required string Name { get; init; }
    public required string Continent { get; init; }

    public GovernmentType Government { get; set; }
    public StateFiscalPolicy Policy { get; init; }
    public CivicState Civic { get; init; }

    public double Gdp { get; set; }
    public double Treasury { get; set; }
    public double Stability { get; set; }
    public double TechLevel { get; set; }
    public double TechProgress { get; set; }

    public ResourceVector Production { get; init; }
    public ResourceVector Consumption { get; init; }
    public ResourceVector Balance { get; init; }
    public MilitaryForce Military { get; init; }

    public double PowerScore { get; }
}
```

`Gdp` is an annual proxy used as the tax base. `Treasury` is liquid State cash. `Stability` is a
synthesized index (not independently settable) reflecting how well the regime currently holds
together — see §5.

Core deliberately excludes presentation fields. A Polity has no screen position, no color, no
sprite, and no habitat classification. Those belong to product hosts.

---

## 3. StateFiscalPolicy — knobs, not outcomes

```csharp
public sealed class StateFiscalPolicy
{
    public double HouseholdTaxRate { get; set; }
    public double TransferShare { get; set; }
    public double InfrastructureShare { get; set; }
    public double PropagandaShare { get; set; }
    public double MilitaryShare { get; set; }
}
```

Every field here is a **share or rate**, never a settled amount. Anything that wants to change
State behavior — a heuristic policy agent, an interactive session, a scripted scenario — mutates
these knobs. It never writes directly to `Treasury`, `Civic`, or `Military`. Only
`CivicEngine.ApplyMonth` reads the knobs and settles stocks.

This mirrors `Novolis.Economy.Core`'s `StatePolicy`: policy expresses intent, the period engine
expresses consequence.

---

## 4. CivicState — settled stocks

```csharp
public sealed class CivicState
{
    public double Legitimacy { get; set; }
    public double Approval { get; set; }
    public double Corruption { get; set; }
    public double HumanDevelopment { get; set; }
    public double WarFatigue { get; set; }
    public double LastTaxCollected { get; set; }
    public double LastTransfersPaid { get; set; }
}
```

`Legitimacy`, `Approval`, `Corruption`, and `HumanDevelopment` are **stocks**: they carry forward
period to period and only move through `CivicEngine.ApplyMonth`. `WarFatigue` accumulates while a
Polity has active wars and decays otherwise. The `Last*` fields are flow scratch, retained for
observability, not fed back as authoritative inputs.

Nothing outside Core computes legitimacy or approval directly. A UI panel may display them; it
may not invent them.

---

## 5. CivicEngine — the period settlement

```csharp
public static class CivicEngine
{
    public sealed class MonthContext
    {
        public required double ControlRatio { get; init; }
        public required int ActiveWars { get; init; }
        public required double ResourceShortage { get; init; }
        public required bool OccupyingForeignLand { get; init; }
        public required bool LostHomeProvinces { get; init; }
        public double ResearchMultiplier { get; init; }
    }

    public static void ApplyMonth(Polity polity, MonthContext ctx);
}
```

`ApplyMonth` is the only place fiscal and civic stocks move. Its inputs are the Polity's own
policy knobs plus a small, explicit `MonthContext` describing facts about the wider world
(territorial control ratio, war count, shortage magnitude) that Core itself cannot know — those
facts are computed by Simulation, which owns the world-level view.

The settlement order is:

```text
1. Clamp policy knobs to valid ranges.
2. Collect tax from GDP × tax rate × control ratio × collection capacity.
3. Pay transfers up to available cash.
4. Split remaining budget into military spend and a civic pool.
5. Split the civic pool into infrastructure and propaganda.
6. Grow military capability from spend (a rate, not an instant unit purchase).
7. Apply corruption leak.
8. Settle HumanDevelopment, WarFatigue, Legitimacy, Approval, Corruption.
9. Synthesize Stability from Legitimacy, Approval, and WarFatigue.
10. Advance TechProgress/TechLevel and grow GDP from Stability × TechLevel × HD.
11. Apply an insolvency penalty if Treasury is negative.
```

Every simulated month should be reproducible from this single method plus its `MonthContext` —
there is no hidden state elsewhere in Core that participates in settlement.

---

## 6. GovernmentRules — regime-conditioned response

```csharp
public static class GovernmentRules
{
    public static double MilitaryUpkeepFactor(GovernmentType g);
    public static double PropagandaEffectiveness(GovernmentType g);
    public static double TaxApprovalSensitivity(GovernmentType g);
    public static double AllianceRelationBonus(GovernmentType g);
    public static GovernmentType Roll(Random rng);
}
```

Regime type does not gate what a Polity *may* do — it changes the *magnitude* of settlement.
Democracies are more tax-sensitive and less propaganda-effective; juntas run cheaper militaries
and colder diplomacy. `Roll` is retained here (not in Scenarios) because it is a small,
parameterless distribution over a Core enum, not a world-generation concern.

---

## 7. Provinces and resources

```csharp
public sealed class Province
{
    public required ProvinceId Id { get; init; }
    public required string Name { get; init; }
    public required PolityId OwnerId { get; set; }
    public required PolityId HomePolityId { get; init; }
    public double Population { get; set; }
    public double Wealth { get; set; }
    public bool Coastal { get; set; }
    public List<ProvinceId> Neighbors { get; init; }
    public ResourceVector ResourceWeights { get; init; }
}
```

A Province is the minimum territorial unit needed to answer: who owns this land, how much does
it produce, and what is it adjacent to. `Neighbors` supports frontier discovery for conflict
without Core knowing anything about combat. `OwnerId` may drift from `HomePolityId` under
occupation — the difference is itself a fact Simulation-level civic settlement reads through
`MonthContext`.

Core does not store a screen position, a terrain sprite, or a habitat classification for a
Province. Territory adjacency is structural, not visual.

---

## 8. Relations, treaties, wars — structural records

```csharp
public sealed class RelationMatrix { /* symmetric [-100, 100] per unordered pair */ }
public sealed class Treaty { /* structural: kind, members/sides, day range, active flag */ }
public sealed class Supranational { /* structural: kind, member set, linked treaties */ }
public sealed class War { /* structural: attacker, defender, day range, captures */ }
```

Core stores these as **records of commitments**, not as the logic that forms, evaluates, or
dissolves them. Whether a bilateral proposal should be accepted (`DiplomaticRules`), how an
alliance chain-reacts to a war (`DiplomaticInstruments.DeclareWar`), and how a battle resolves
(`ConflictResolver`) all live above Core, in `Novolis.Geopolitics.Diplomacy` and
`Novolis.Geopolitics.Conflict`. Core exposes only the query surface those engines need:
`AreAtWar`, `HaveTreaty`, `AreAllied`, `AreEmbargoed`, `HasMilitaryAccess`.

---

## 9. WorldState — the aggregate

```csharp
public sealed class WorldState
{
    public const int DaysPerMonth = 30;
    public const int DaysPerYear = 360;

    public int Day { get; set; }
    public List<Polity> Polities { get; }
    public List<Province> Provinces { get; }
    public RelationMatrix Relations { get; }
    public List<Treaty> Treaties { get; }
    public List<Supranational> Supranationals { get; }
    public List<War> Wars { get; }
    public List<GeoEvent> Events { get; }
}
```

`WorldState` is the entire simulated world at a point in time. It carries a calendar (`Day`,
`DaysPerMonth`, `DaysPerYear`) and invariant-preserving query helpers, but no month-tick logic —
advancing the day and deciding what happens at a month boundary is `WorldSimulation`'s job in the
Simulation package.

---

## 10. WorldTelemetry — the observation surface

```csharp
public sealed class WorldTelemetry
{
    public int DaysAdvanced { get; set; }
    public int WarsStarted { get; set; }
    public int TreatiesSigned { get; set; }
    public double CommonMarketVolume { get; set; }
    public double MeanLegitimacy { get; set; }
    // ...
}
```

`WorldTelemetry` is a plain accumulator, written by the engines that produce the events it
counts (Diplomacy, Conflict, Trade, PolicyAgents) and read by `Simulation` and observers. It is
not itself a simulation step; it has no behavior beyond incrementing counters. It lives in Core
so every engine package can depend on the same telemetry type without depending on each other or
on Simulation.

---

## 11. Core invariants

### Fiscal conservation

```text
Treasury after
= Treasury before
+ tax collected
- transfers paid
- infrastructure spend
- propaganda spend
- military spend
- corruption leak
```

### Stock settlement exclusivity

```text
Only CivicEngine.ApplyMonth writes:
  Polity.Treasury, Polity.Stability, Polity.Gdp, Polity.TechLevel, Polity.TechProgress,
  Polity.Military (growth/decay), Polity.Civic.*
```

Any other writer of these fields (an AI, an app, a test fixture) is a boundary violation.

### Policy-only external mutation

```text
Everything outside CivicEngine may mutate StateFiscalPolicy.
Nothing outside CivicEngine may mutate CivicState directly.
```

### Relation symmetry

```text
Relations.Get(a, b) == Relations.Get(b, a)
```

### Territorial ownership

```text
Every Province has exactly one OwnerId at any time.
HomePolityId is immutable; OwnerId may diverge from it under occupation.
```

---

## 12. What Core deliberately excludes

The bounded minimum excludes:

- map coordinates, screen positions, or any presentation geometry
- habitat/terrain classification for UI theming
- procedural world generation and seed data (→ `Novolis.Geopolitics.Scenarios`)
- diplomatic acceptance heuristics and treaty formation logic (→ `.Diplomacy`)
- combat resolution (→ `.Conflict`)
- resource clearing and market logic (→ `.Trade`)
- heuristic or interactive policy-setting behavior (→ `.PolicyAgents`)
- day-tick orchestration and month-boundary composition (→ `.Simulation`)

These are not omissions — they are boundary decisions. A concept only belongs in Core if every
package built on top of Core would otherwise have to reinvent it identically.

---

## 13. Resulting grammar

> A Polity is a State actor whose fiscal policy is a set of knobs, not outcomes. CivicEngine is
> the sole settlement of those knobs into fiscal and civic stocks each period. Provinces are
> owned territory that bound the tax base and resource output. Relations, treaties, wars, and
> organizations are structural records; the logic that forms and resolves them lives above Core.
> WorldState aggregates all of this at a point in time; WorldTelemetry observes what happened
> without altering it.

Its rigor comes from preserving the relationships that cannot be removed without changing the
meaning of state behavior:

```text
policy → settlement → stock
ownership
legitimacy as consequence, not input
symmetry of relations
```

Everything else — presentation, generation, heuristics, orchestration — is composition above
Core.
