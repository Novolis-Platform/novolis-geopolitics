namespace Novolis.Geopolitics.Core;

/// <summary>
/// Fundamental civic / fiscal period settlement (Economy-style policy step).
/// Pure stock–flow updates over <see cref="Polity"/> — no AI, no UI.
/// </summary>
public static class CivicEngine
{
    public sealed class MonthContext
    {
        public required double ControlRatio { get; init; }
        public required int ActiveWars { get; init; }
        public required double ResourceShortage { get; init; }
        public required bool OccupyingForeignLand { get; init; }
        public required bool LostHomeProvinces { get; init; }

        /// <summary>External research multiplier (e.g. ResearchPartnership treaty) ≥ 1.</summary>
        public double ResearchMultiplier { get; init; } = 1.0;
    }

    /// <summary>Apply one month of fiscal + civic dynamics for a single polity.</summary>
    public static void ApplyMonth(Polity polity, MonthContext ctx)
    {
        var policy = polity.Policy;
        var civic = polity.Civic;
        var g = polity.Government;

        var taxRate = Math.Clamp(policy.HouseholdTaxRate, 0, 0.6);
        var milShare = Math.Clamp(policy.MilitaryShare, 0, 0.7);
        var transferShare = Math.Clamp(policy.TransferShare, 0, 0.8);
        var control = Math.Clamp(ctx.ControlRatio, 0.25, 1.25);

        // Effective tax base: HD raises collection capacity; corruption / fatigue erode it
        // (Economy State tax step — capacity before cash moves).
        var collectionCapacity = Math.Clamp(
            0.55 + civic.HumanDevelopment * 0.35 - civic.Corruption * 0.25 - civic.WarFatigue * 0.15,
            0.35,
            1.15);

        // --- Fiscal flows (State cash: tax → transfers → civ/mil outlays → corruption leak) ---
        var taxIncome = polity.Gdp * taxRate / 12.0 * control * collectionCapacity;
        civic.LastTaxCollected = taxIncome;

        var transfersWanted = taxIncome * transferShare;
        var transfersPaid = Math.Min(transfersWanted, Math.Max(0, polity.Treasury + taxIncome));
        civic.LastTransfersPaid = transfersPaid;

        polity.Treasury += taxIncome;
        polity.Treasury -= transfersPaid;

        var afterTransfers = Math.Max(0, taxIncome - transfersPaid);
        var milSpend = afterTransfers * milShare;
        var civPool = Math.Max(0, afterTransfers - milSpend);

        var infraShare = Math.Clamp(policy.InfrastructureShare, 0, 1);
        var propShare = Math.Clamp(policy.PropagandaShare, 0, 1);
        var shareSum = Math.Max(0.01, infraShare + propShare);
        var infra = civPool * (infraShare / shareSum);
        var propagandaBudget = civPool * (propShare / shareSum);
        var propagandaEffect = propagandaBudget * GovernmentRules.PropagandaEffectiveness(g);
        polity.Treasury -= infra + propagandaBudget;

        // Capability accumulation from spend — a stock–flow rate, not an instant "build".
        var upkeepFactor = GovernmentRules.MilitaryUpkeepFactor(g);
        var forceGrowth = milSpend * 0.65 / 100.0 * (1.0 + polity.TechLevel * 0.05) / upkeepFactor;
        polity.Military.Land += forceGrowth * 0.55;
        polity.Military.Air += forceGrowth * 0.25;
        polity.Military.Naval += forceGrowth * 0.20;
        polity.Treasury -= milSpend * 0.35 * upkeepFactor;
        polity.Military.Scale(1.0 - 0.008 * upkeepFactor);

        // Corruption leak (fiscal).
        var leak = Math.Min(Math.Max(0, polity.Treasury) * 0.02, taxIncome * civic.Corruption * 0.15);
        polity.Treasury -= leak;

        // Sync legacy TaxRate / MilitaryBudgetShare mirrors for older callers.
        polity.TaxRate = taxRate;
        polity.MilitaryBudgetShare = milShare;

        // --- Civic stocks ---
        var transferDelivery = transfersWanted <= 0 ? 0.5 : transfersPaid / transfersWanted;
        var taxPressure = taxRate * GovernmentRules.TaxApprovalSensitivity(g);
        var gdp = Math.Max(1, polity.Gdp);

        civic.HumanDevelopment = Clamp01to2(
            civic.HumanDevelopment
            + infra / gdp * 2.0
            + transfersPaid / gdp * 0.4
            - civic.Corruption * 0.002
            - Math.Min(0.01, ctx.ResourceShortage * 0.00008));

        civic.WarFatigue = ctx.ActiveWars > 0
            ? Math.Min(1, civic.WarFatigue + 0.03 * ctx.ActiveWars + (ctx.OccupyingForeignLand ? 0.01 : 0))
            : Math.Max(0, civic.WarFatigue - 0.025);

        var legitimacyDelta =
            0.02 * (transferDelivery - 0.5)
            + 0.015 * (civic.Approval - 0.5)
            - 0.04 * civic.Corruption
            - 0.03 * civic.WarFatigue
            - (ctx.LostHomeProvinces ? 0.04 : 0)
            - (ctx.OccupyingForeignLand ? 0.01 : 0)
            + propagandaEffect / gdp * 0.8
            + (control < 0.7 ? -0.025 : 0.005);
        civic.Legitimacy = Clamp01(civic.Legitimacy + legitimacyDelta);

        var approvalDelta =
            -0.05 * (taxPressure - 0.22)
            + 0.03 * (transferDelivery - 0.4)
            + 0.02 * (civic.Legitimacy - 0.5)
            + propagandaEffect / gdp * 1.2
            - Math.Min(0.05, ctx.ResourceShortage * 0.0004)
            - 0.04 * civic.WarFatigue;
        civic.Approval = Clamp01(civic.Approval + approvalDelta);

        // Corruption drifts with low HD / high mil share / occupation.
        civic.Corruption = Clamp01(
            civic.Corruption
            + (milShare > 0.4 ? 0.004 : -0.001)
            + (civic.HumanDevelopment < 0.4 ? 0.003 : -0.002)
            + (ctx.OccupyingForeignLand ? 0.002 : 0)
            - propagandaEffect / gdp * 0.3);

        // Stability = operational synthesis (Economy solvency analogue for the State).
        var targetStability = civic.Legitimacy * 0.45 + civic.Approval * 0.40 + (1.0 - civic.WarFatigue) * 0.15;
        targetStability *= 0.7 + 0.3 * control;
        polity.Stability = Clamp01(polity.Stability * 0.7 + targetStability * 0.3);

        // Research / growth couple to HD + treaty research multiplier.
        var researchMult = (0.85 + civic.HumanDevelopment * 0.35) * Math.Max(1.0, ctx.ResearchMultiplier);
        polity.TechProgress += (infra * 0.02 + polity.Gdp * 0.00001) * polity.Stability * researchMult;
        if (polity.TechProgress >= 100.0 * polity.TechLevel)
        {
            polity.TechProgress = 0;
            polity.TechLevel += 0.1;
        }

        var growth = 0.001 * polity.Stability * polity.TechLevel * (0.8 + civic.HumanDevelopment * 0.4) / 12.0;
        growth -= Math.Min(0.002, ctx.ResourceShortage * 0.00005);
        growth -= civic.Corruption * 0.0003;
        polity.Gdp *= 1.0 + growth;

        if (polity.Treasury < 0)
        {
            polity.Stability = Math.Max(0.05, polity.Stability - 0.04);
            civic.Legitimacy = Math.Max(0.05, civic.Legitimacy - 0.03);
            civic.Approval = Math.Max(0.05, civic.Approval - 0.02);
            polity.Treasury *= 0.95;
        }
    }

    private static double Clamp01(double x) => Math.Clamp(x, 0, 1);
    private static double Clamp01to2(double x) => Math.Clamp(x, 0, 2);
}
