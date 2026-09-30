namespace Novolis.Geopolitics.Core;

/// <summary>Legacy flag bag; prefer <see cref="SupranationalKind"/> + <see cref="SupranationalCatalog"/>.</summary>
public sealed class SupranationalCharter
{
    public bool HasCommonMarket { get; set; }
    public bool HasAlliance { get; set; }
    public bool HasResearchPact { get; set; }
    public bool HasFreeTrade { get; set; }
    public bool HasMilitaryAccess { get; set; }
    public bool HasCulturalExchanges { get; set; }
}
