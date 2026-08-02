namespace Novolis.Geopolitics.Core;

/// <summary>Abstract force strength by military domain.</summary>
public sealed class MilitaryForce
{
    public double Land { get; set; }
    public double Air { get; set; }
    public double Naval { get; set; }

    public double Total => Land + Air + Naval;

    public MilitaryForce Clone() => new()
    {
        Land = Land,
        Air = Air,
        Naval = Naval,
    };

    public void Scale(double factor)
    {
        Land *= factor;
        Air *= factor;
        Naval *= factor;
    }

    public void Add(MilitaryForce other)
    {
        Land += other.Land;
        Air += other.Air;
        Naval += other.Naval;
    }
}

/// <summary>Per-kind resource quantities (production, consumption, or stock).</summary>
public sealed class ResourceVector
{
    private readonly double[] _v = new double[ResourceKinds.Count];

    public double this[ResourceKind kind]
    {
        get => _v[(int)kind];
        set => _v[(int)kind] = value;
    }

    public double Sum => _v.Sum();

    public ResourceVector Clone()
    {
        var c = new ResourceVector();
        Array.Copy(_v, c._v, _v.Length);
        return c;
    }

    public void Add(ResourceVector other)
    {
        for (var i = 0; i < _v.Length; i++)
        {
            _v[i] += other._v[i];
        }
    }

    public void Scale(double factor)
    {
        for (var i = 0; i < _v.Length; i++)
        {
            _v[i] *= factor;
        }
    }

    public double[] ToArray() => (double[])_v.Clone();

    public static ResourceVector FromArray(double[]? values)
    {
        var v = new ResourceVector();
        if (values is null)
        {
            return v;
        }

        var n = Math.Min(values.Length, ResourceKinds.Count);
        for (var i = 0; i < n; i++)
        {
            v._v[i] = values[i];
        }

        return v;
    }
}

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

/// <summary>Owned territory cell with adjacency and resource weights.</summary>
public sealed class Province
{
    public required ProvinceId Id { get; init; }
    public required string Name { get; init; }
    public required PolityId OwnerId { get; set; }
    public required PolityId HomePolityId { get; init; }
    public double Population { get; set; }
    public double Wealth { get; set; }
    public bool Coastal { get; set; }
    public List<ProvinceId> Neighbors { get; init; } = [];

    /// <summary>Relative productivity weights per resource (typically sum ≈ 1).</summary>
    public ResourceVector ResourceWeights { get; init; } = new();
}

/// <summary>
/// Multilateral or directed diplomatic instrument (SP2 GTreaty homage).
/// Single-side kinds use <see cref="Members"/>; directed kinds use SideA/SideB.
/// </summary>
public sealed class Treaty
{
    public required long Id { get; init; }
    public required string Name { get; set; }
    public required TreatyKind Kind { get; init; }
    public required PolityId Creator { get; init; }
    public HashSet<PolityId> Members { get; init; } = [];
    public HashSet<PolityId> SideA { get; init; } = [];
    public HashSet<PolityId> SideB { get; init; } = [];
    public int SignedDay { get; init; }
    public int ExpiresDay { get; set; } = -1;
    public bool Active { get; set; } = true;
    public SupranationalId? LinkedOrgId { get; set; }

    public static bool IsDirected(TreatyKind kind) =>
        kind is TreatyKind.Peace or TreatyKind.EconomicAid or TreatyKind.EconomicEmbargo
            or TreatyKind.WeaponTradeEmbargo;

    public bool Contains(PolityId id) =>
        Members.Contains(id) || SideA.Contains(id) || SideB.Contains(id);

    public IEnumerable<PolityId> AllParticipants()
    {
        foreach (var m in Members)
        {
            yield return m;
        }

        foreach (var a in SideA)
        {
            yield return a;
        }

        foreach (var b in SideB)
        {
            yield return b;
        }
    }

    public bool SharesMembership(PolityId a, PolityId b) =>
        Members.Contains(a) && Members.Contains(b);

    public bool AreOpposed(PolityId a, PolityId b) =>
        (SideA.Contains(a) && SideB.Contains(b)) || (SideA.Contains(b) && SideB.Contains(a));
}

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

/// <summary>Named continental / regional bloc with kind-driven linked treaties.</summary>
public sealed class Supranational
{
    public required SupranationalId Id { get; init; }
    public required string Name { get; init; }
    public required SupranationalKind Kind { get; init; }
    public string? ContinentHint { get; init; }
    public HashSet<PolityId> MemberIds { get; init; } = [];
    public SupranationalCharter Charter { get; init; } = new();
    public List<long> LinkedTreatyIds { get; init; } = [];
    public bool Active { get; set; } = true;
}

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

    /// <summary>Minimum median relation with members to join (SP2-ish barriers).</summary>
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

public sealed class War
{
    public required long Id { get; init; }
    public required PolityId Attacker { get; init; }
    public required PolityId Defender { get; init; }
    public int StartedDay { get; init; }
    public bool Active { get; set; } = true;
    public int EndedDay { get; set; } = -1;
    public int ProvincesTakenByAttacker { get; set; }
    public int ProvincesTakenByDefender { get; set; }
}

public sealed class GeoEvent
{
    public required int Day { get; init; }
    public required GeoEventKind Kind { get; init; }
    public required string Message { get; init; }
    public PolityId? PolityA { get; init; }
    public PolityId? PolityB { get; init; }
    public ProvinceId? Province { get; init; }
}
