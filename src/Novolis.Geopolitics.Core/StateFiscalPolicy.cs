namespace Novolis.Geopolitics.Core;

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
