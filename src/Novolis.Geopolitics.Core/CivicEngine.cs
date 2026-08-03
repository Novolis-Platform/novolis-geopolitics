using CivicsEngine = Novolis.Civics.Core.CivicEngine;
using CivicsGov = Novolis.Civics.Core.GovernmentType;
using CivicsGovRules = Novolis.Civics.Core.GovernmentRules;
using Novolis.Civics.Core;

namespace Novolis.Geopolitics.Core;

/// <summary>
/// Geopolitics month civic/fiscal settlement over <see cref="Polity"/>.
/// Delegates nation stock–flow to <see cref="CivicsEngine"/>; applies force-domain growth locally.
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

        /// <summary>Optional host net migration (people) applied this civic period.</summary>
        public double? NetMigration { get; init; }
    }

    /// <summary>Apply one month of fiscal + civic dynamics for a single polity.</summary>
    public static void ApplyMonth(Polity polity, MonthContext ctx, WorldState? world = null)
    {
        ArgumentNullException.ThrowIfNull(polity);
        ArgumentNullException.ThrowIfNull(ctx);

        var nation = ToNation(polity, world);
        var period = new PeriodContext
        {
            ControlRatio = ctx.ControlRatio,
            ActiveWars = ctx.ActiveWars,
            ResourceShortage = ctx.ResourceShortage,
            OccupyingForeignLand = ctx.OccupyingForeignLand,
            LostHomeTerritory = ctx.LostHomeProvinces,
            ResearchMultiplier = ctx.ResearchMultiplier,
            NetMigration = ctx.NetMigration ?? (polity.Civic.LastNetMigration != 0
                ? polity.Civic.LastNetMigration
                : null),
        };

        var outcome = CivicsEngine.ApplyPeriod(nation, period);
        CopyBack(polity, nation);
        polity.Civic.EmigrationPressure = outcome.EmigrationPressure;
        polity.Civic.ImmigrationAttractiveness = outcome.ImmigrationAttractiveness;

        var upkeepFactor = CivicsGovRules.MilitaryUpkeepFactor((CivicsGov)(int)polity.Government);
        var demand = outcome.ForceCapabilityDemand;
        polity.Military.Land += demand * 0.55;
        polity.Military.Air += demand * 0.25;
        polity.Military.Naval += demand * 0.20;
        polity.Military.Scale(1.0 - 0.008 * upkeepFactor);

        polity.TaxRate = Math.Clamp(polity.Policy.HouseholdTaxRate, 0, 0.6);
        polity.MilitaryBudgetShare = Math.Clamp(polity.Policy.MilitaryShare, 0, 0.7);
    }

    private static NationState ToNation(Polity polity, WorldState? world)
    {
        var nation = new NationState
        {
            Id = NationIdFor(polity.Id),
            Name = polity.Name,
            Government = (CivicsGov)(int)polity.Government,
            Gdp = polity.Gdp,
            Treasury = polity.Treasury,
            Stability = polity.Stability,
            TechnologyStock = polity.TechLevel,
            TechnologyProgress = polity.TechProgress,
        };

        nation.Policy.HouseholdTaxRate = polity.Policy.HouseholdTaxRate;
        nation.Policy.TransferShare = polity.Policy.TransferShare;
        nation.Policy.InfrastructureShare = polity.Policy.InfrastructureShare;
        nation.Policy.PropagandaShare = polity.Policy.PropagandaShare;
        nation.Policy.MilitaryShare = polity.Policy.MilitaryShare;

        nation.Civic.Legitimacy = polity.Civic.Legitimacy;
        nation.Civic.Approval = polity.Civic.Approval;
        nation.Civic.Corruption = polity.Civic.Corruption;
        nation.Civic.HumanDevelopment = polity.Civic.HumanDevelopment;
        nation.Civic.WarFatigue = polity.Civic.WarFatigue;
        nation.Civic.LastTaxCollected = polity.Civic.LastTaxCollected;
        nation.Civic.LastTransfersPaid = polity.Civic.LastTransfersPaid;

        // Sync demography from owned provinces when world is available.
        if (world is not null)
        {
            nation.Demography.Population = world.OwnedPopulation(polity.Id);
            nation.Demography.LastNetMigration = polity.Civic.LastNetMigration;
            nation.Demography.LastEmigrationPressure = polity.Civic.EmigrationPressure;
        }

        return nation;
    }

    private static void CopyBack(Polity polity, NationState nation)
    {
        polity.Gdp = nation.Gdp;
        polity.Treasury = nation.Treasury;
        polity.Stability = nation.Stability;
        polity.TechLevel = nation.TechnologyStock;
        polity.TechProgress = nation.TechnologyProgress;

        polity.Civic.Legitimacy = nation.Civic.Legitimacy;
        polity.Civic.Approval = nation.Civic.Approval;
        polity.Civic.Corruption = nation.Civic.Corruption;
        polity.Civic.HumanDevelopment = nation.Civic.HumanDevelopment;
        polity.Civic.WarFatigue = nation.Civic.WarFatigue;
        polity.Civic.LastTaxCollected = nation.Civic.LastTaxCollected;
        polity.Civic.LastTransfersPaid = nation.Civic.LastTransfersPaid;
    }

    private static NationId NationIdFor(PolityId id)
    {
        Span<byte> bytes = stackalloc byte[16];
        BitConverter.TryWriteBytes(bytes[..4], id.Value);
        return NationId.From(new Guid(bytes));
    }
}
