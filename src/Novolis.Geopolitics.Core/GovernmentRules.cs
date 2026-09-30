namespace Novolis.Geopolitics.Core;

/// <summary>Regime modifiers on tax approval, propaganda, and alliance formation (original numbers).</summary>
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
