namespace Novolis.Geopolitics.Core;

/// <summary>Named multilateral organization archetypes (forums through political unions).</summary>
public enum SupranationalKind
{
    /// <summary>Talk shop — cultural ties, easy entry, no mutual defense.</summary>
    Forum = 0,
    /// <summary>Mutual defense + staging access.</summary>
    DefenceAlliance = 1,
    /// <summary>Preferential trade (economic partnership), not full resource pool.</summary>
    FreeTradeArea = 2,
    /// <summary>Common market — members clear resources first.</summary>
    CustomsUnion = 3,
    /// <summary>Shared research collaboration.</summary>
    ResearchForum = 4,
    /// <summary>Deep integration: defense + customs + research.</summary>
    PoliticalUnion = 5,
}
