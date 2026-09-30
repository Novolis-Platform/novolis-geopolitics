namespace Novolis.Geopolitics.Core;

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

    /// <summary>Last Civics emigration pressure [0, 1].</summary>
    public double EmigrationPressure { get; set; }

    /// <summary>Last Civics immigration attractiveness [0, 1].</summary>
    public double ImmigrationAttractiveness { get; set; }

    /// <summary>Last period net migration into this polity (people).</summary>
    public double LastNetMigration { get; set; }

    public CivicState Clone() => new()
    {
        Legitimacy = Legitimacy,
        Approval = Approval,
        Corruption = Corruption,
        HumanDevelopment = HumanDevelopment,
        WarFatigue = WarFatigue,
        LastTaxCollected = LastTaxCollected,
        LastTransfersPaid = LastTransfersPaid,
        EmigrationPressure = EmigrationPressure,
        ImmigrationAttractiveness = ImmigrationAttractiveness,
        LastNetMigration = LastNetMigration,
    };
}
