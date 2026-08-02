namespace Novolis.Geopolitics.Core;

/// <summary>Domestic regime (SP2 government-type homage; affects fiscal and diplomacy modifiers).</summary>
public enum GovernmentType
{
    Democracy = 0,
    Multiparty = 1,
    SingleParty = 2,
    Autocracy = 3,
    MilitaryJunta = 4,
    Monarchy = 5,
}

/// <summary>
/// Fiscal policy knobs for the polity-as-State actor (mirrors Economy <c>StatePolicy</c> spirit:
/// tax / transfer / spend shares — not UI).
/// </summary>
public sealed class StateFiscalPolicy
{
    /// <summary>Household/income tax rate in [0, 0.6].</summary>
    public double HouseholdTaxRate { get; set; } = 0.22;

    /// <summary>Share of tax revenue paid as transfers (welfare) in [0, 0.8].</summary>
    public double TransferShare { get; set; } = 0.25;

    /// <summary>Share of remaining civ budget to infrastructure (HD) in [0, 1].</summary>
    public double InfrastructureShare { get; set; } = 0.45;

    /// <summary>Share of remaining civ budget to propaganda/approval in [0, 1].</summary>
    public double PropagandaShare { get; set; } = 0.20;

    /// <summary>Military budget share of tax revenue in [0, 0.7].</summary>
    public double MilitaryShare { get; set; } = 0.28;

    public static StateFiscalPolicy Default { get; } = new();

    public StateFiscalPolicy Clone() => new()
    {
        HouseholdTaxRate = HouseholdTaxRate,
        TransferShare = TransferShare,
        InfrastructureShare = InfrastructureShare,
        PropagandaShare = PropagandaShare,
        MilitaryShare = MilitaryShare,
    };
}

/// <summary>Civic stocks (period-settled; not free-floating UI meters).</summary>
public sealed class CivicState
{
    /// <summary>Regime legitimacy stock [0, 1].</summary>
    public double Legitimacy { get; set; } = 0.65;

    /// <summary>Popular approval stock [0, 1].</summary>
    public double Approval { get; set; } = 0.55;

    /// <summary>Corruption stock [0, 1] — leaks treasury and legitimacy.</summary>
    public double Corruption { get; set; } = 0.15;

    /// <summary>Human development [0, 2] — soft capacity for growth/tech.</summary>
    public double HumanDevelopment { get; set; } = 0.55;

    /// <summary>War fatigue [0, 1].</summary>
    public double WarFatigue { get; set; }

    /// <summary>Last period tax collected (flow scratch).</summary>
    public double LastTaxCollected { get; set; }

    /// <summary>Last period transfers paid (flow scratch).</summary>
    public double LastTransfersPaid { get; set; }

    public CivicState Clone() => new()
    {
        Legitimacy = Legitimacy,
        Approval = Approval,
        Corruption = Corruption,
        HumanDevelopment = HumanDevelopment,
        WarFatigue = WarFatigue,
        LastTaxCollected = LastTaxCollected,
        LastTransfersPaid = LastTransfersPaid,
    };
}

/// <summary>Regime modifiers (homage to SP2 government constants — original numbers).</summary>
public static class GovernmentRules
{
    public static double MilitaryUpkeepFactor(GovernmentType g) => g switch
    {
        GovernmentType.MilitaryJunta => 0.78,
        GovernmentType.Autocracy => 0.92,
        GovernmentType.Democracy or GovernmentType.Multiparty => 1.05,
        _ => 1.0,
    };

    public static double PropagandaEffectiveness(GovernmentType g) => g switch
    {
        GovernmentType.Autocracy or GovernmentType.SingleParty => 1.35,
        GovernmentType.MilitaryJunta => 1.15,
        GovernmentType.Democracy => 0.75,
        _ => 1.0,
    };

    public static double TaxApprovalSensitivity(GovernmentType g) => g switch
    {
        GovernmentType.Democracy or GovernmentType.Multiparty => 1.4,
        GovernmentType.Autocracy or GovernmentType.MilitaryJunta => 0.7,
        _ => 1.0,
    };

    public static double AllianceRelationBonus(GovernmentType g) => g switch
    {
        GovernmentType.Democracy or GovernmentType.Multiparty => 0,
        GovernmentType.MilitaryJunta => -8,
        GovernmentType.Autocracy => -5,
        _ => -2,
    };

    public static GovernmentType Roll(Random rng)
    {
        var r = rng.NextDouble();
        if (r < 0.28)
        {
            return GovernmentType.Democracy;
        }

        if (r < 0.48)
        {
            return GovernmentType.Multiparty;
        }

        if (r < 0.62)
        {
            return GovernmentType.SingleParty;
        }

        if (r < 0.78)
        {
            return GovernmentType.Autocracy;
        }

        if (r < 0.90)
        {
            return GovernmentType.MilitaryJunta;
        }

        return GovernmentType.Monarchy;
    }
}
