namespace Novolis.Geopolitics.Core;

/// <summary>Maps org archetypes → linked treaties and join thresholds.</summary>
public static class SupranationalCatalog
{
    public static SupranationalCharter CharterFor(SupranationalKind kind) => kind switch
    {
        SupranationalKind.Forum => new() { HasCulturalExchanges = true },
        SupranationalKind.DefenceAlliance => new() { HasAlliance = true, HasMilitaryAccess = true },
        SupranationalKind.FreeTradeArea => new() { HasFreeTrade = true },
        SupranationalKind.CustomsUnion => new() { HasCommonMarket = true, HasFreeTrade = true },
        SupranationalKind.ResearchForum => new() { HasResearchPact = true },
        SupranationalKind.PoliticalUnion => new()
        {
            HasAlliance = true,
            HasMilitaryAccess = true,
            HasCommonMarket = true,
            HasFreeTrade = true,
            HasResearchPact = true,
            HasCulturalExchanges = true,
        },
        _ => new(),
    };

    public static IEnumerable<(TreatyKind Kind, string Suffix)> LinkedInstruments(SupranationalKind kind)
    {
        var c = CharterFor(kind);
        if (c.HasAlliance)
        {
            yield return (TreatyKind.Alliance, "Mutual Defense");
        }

        if (c.HasMilitaryAccess)
        {
            yield return (TreatyKind.MilitaryAccess, "Access Accord");
        }

        if (c.HasCommonMarket)
        {
            yield return (TreatyKind.CommonMarket, "Customs Area");
        }
        else if (c.HasFreeTrade)
        {
            yield return (TreatyKind.EconomicPartnership, "Free Trade");
        }

        if (c.HasResearchPact)
        {
            yield return (TreatyKind.ResearchPartnership, "Research Council");
        }

        if (c.HasCulturalExchanges)
        {
            yield return (TreatyKind.CulturalExchanges, "Cultural Forum");
        }
    }

    /// <summary>Minimum median relation with members required to join.</summary>
    public static double MinJoinRelation(SupranationalKind kind) => kind switch
    {
        SupranationalKind.Forum => 12,
        SupranationalKind.FreeTradeArea => 28,
        SupranationalKind.CustomsUnion => 35,
        SupranationalKind.ResearchForum => 30,
        SupranationalKind.DefenceAlliance => 45,
        SupranationalKind.PoliticalUnion => 55,
        _ => 25,
    };

    public static string ShortLabel(SupranationalKind kind) => kind switch
    {
        SupranationalKind.Forum => "Forum",
        SupranationalKind.DefenceAlliance => "Defence",
        SupranationalKind.FreeTradeArea => "FTA",
        SupranationalKind.CustomsUnion => "Customs",
        SupranationalKind.ResearchForum => "Research",
        SupranationalKind.PoliticalUnion => "Union",
        _ => kind.ToString(),
    };
}
