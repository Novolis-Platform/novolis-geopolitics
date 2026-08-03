namespace Novolis.Geopolitics.Core;

/// <summary>Stable polity identifier (0-based index into WorldState.Polities).</summary>
public readonly record struct PolityId(int Value) : IComparable<PolityId>
{
    public int CompareTo(PolityId other) => Value.CompareTo(other.Value);
    public override string ToString() => Value.ToString();
}

/// <summary>Stable province identifier (0-based index into WorldState.Provinces).</summary>
public readonly record struct ProvinceId(int Value) : IComparable<ProvinceId>
{
    public int CompareTo(ProvinceId other) => Value.CompareTo(other.Value);
    public override string ToString() => Value.ToString();
}

/// <summary>Stable supranational organization identifier.</summary>
public readonly record struct SupranationalId(long Value)
{
    public override string ToString() => Value.ToString();
}

public enum MilitaryDomain
{
    Land = 0,
    Air = 1,
    Naval = 2,
}

/// <summary>Treaty instrument kinds.</summary>
public enum TreatyKind
{
    Peace = 0,
    Alliance = 1,
    MilitaryAccess = 2,
    EconomicPartnership = 3,
    CommonMarket = 4,
    EconomicAid = 5,
    EconomicEmbargo = 6,
    ResearchPartnership = 7,
    WeaponTradeEmbargo = 8,
    CulturalExchanges = 9,
    WeaponTrade = 10,
}

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

/// <summary>Why a polity refuses a treaty or org join.</summary>
public enum TreatyRefusal
{
    Accepted = 0,
    RelationsTooLow,
    AtWar,
    HostileWithFriend,
    CannotAfford,
    PowerImbalance,
    AlreadyBound,
    Unstable,
    WrongProfile,
}

public enum ResourceKind
{
    Food = 0,
    Energy = 1,
    Materials = 2,
    Goods = 3,
    MilitaryGoods = 4,
    Rare = 5,
}

public static class ResourceKinds
{
    public const int Count = 6;

    public static readonly ResourceKind[] All =
    [
        ResourceKind.Food,
        ResourceKind.Energy,
        ResourceKind.Materials,
        ResourceKind.Goods,
        ResourceKind.MilitaryGoods,
        ResourceKind.Rare,
    ];
}

public enum GeoEventKind
{
    WarDeclared = 0,
    PeaceSigned = 1,
    AllianceFormed = 2,
    AllianceBroken = 3,
    TradeSigned = 4,
    ProvinceCaptured = 5,
    BudgetCrisis = 6,
    TechAdvance = 7,
    ForceExpansion = 8,
    RelationShift = 9,
    TreatyFormed = 10,
    TreatyJoined = 11,
    TreatyLeft = 12,
    OrgJoined = 13,
    OrgLeft = 14,
    EmbargoImposed = 15,
    ResourceShortage = 16,
    EconomicAidPaid = 17,
}
