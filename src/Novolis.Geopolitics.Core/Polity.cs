namespace Novolis.Geopolitics.Core;

/// <summary>Nation-state: fiscal State actor + civic stocks + military (Core fundamentals).</summary>
public sealed class Polity
{
    public required PolityId Id { get; init; }
    public required string Name { get; init; }
    public required string Continent { get; init; }

    public GovernmentType Government { get; set; } = GovernmentType.Democracy;

    /// <summary>Fiscal policy knobs (Economy StatePolicy analogue).</summary>
    public StateFiscalPolicy Policy { get; init; } = new();

    /// <summary>Civic stocks settled by <see cref="CivicEngine"/>.</summary>
    public CivicState Civic { get; init; } = new();

    /// <summary>Annual GDP proxy used for tax base.</summary>
    public double Gdp { get; set; }

    /// <summary>Liquid treasury (State cash).</summary>
    public double Treasury { get; set; }

    /// <summary>Legacy mirror of <see cref="StateFiscalPolicy.HouseholdTaxRate"/>.</summary>
    public double TaxRate { get; set; } = 0.22;

    /// <summary>Legacy mirror of <see cref="StateFiscalPolicy.MilitaryShare"/>.</summary>
    public double MilitaryBudgetShare { get; set; } = 0.28;

    /// <summary>Internal stability [0, 1] — synthesized from civic stocks each period.</summary>
    public double Stability { get; set; } = 0.75;

    /// <summary>Single research / tech level.</summary>
    public double TechLevel { get; set; } = 1.0;

    /// <summary>Accumulated research progress toward next tech level.</summary>
    public double TechProgress { get; set; }

    /// <summary>Last monthly resource production (after domestic).</summary>
    public ResourceVector Production { get; init; } = new();

    /// <summary>Last monthly resource consumption demand.</summary>
    public ResourceVector Consumption { get; init; } = new();

    /// <summary>Net after trade this month (surplus positive).</summary>
    public ResourceVector Balance { get; init; } = new();

    public MilitaryForce Military { get; init; } = new();

    public double PowerScore =>
        Gdp * 0.001
        + Military.Total * (1.0 + TechLevel * 0.15)
        + Stability * 40.0
        + Civic.Legitimacy * 25.0
        + Civic.HumanDevelopment * 20.0;
}
