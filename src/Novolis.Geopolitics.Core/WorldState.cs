namespace Novolis.Geopolitics.Core;

/// <summary>Symmetric relation scores in [-100, 100] keyed by unordered polity pair.</summary>
public sealed class RelationMatrix
{
    private readonly Dictionary<long, double> _scores = new();

    public static long PairKey(PolityId a, PolityId b)
    {
        var x = a.Value;
        var y = b.Value;
        if (x > y)
        {
            (x, y) = (y, x);
        }

        return ((long)x << 32) | (uint)y;
    }

    public double Get(PolityId a, PolityId b)
    {
        if (a.Value == b.Value)
        {
            return 100.0;
        }

        return _scores.TryGetValue(PairKey(a, b), out var v) ? v : 0.0;
    }

    public void Set(PolityId a, PolityId b, double value)
    {
        if (a.Value == b.Value)
        {
            return;
        }

        _scores[PairKey(a, b)] = Math.Clamp(value, -100.0, 100.0);
    }

    public void Adjust(PolityId a, PolityId b, double delta) =>
        Set(a, b, Get(a, b) + delta);

    public IReadOnlyDictionary<long, double> Snapshot => _scores;
}

/// <summary>Mutable full-world geopolitics state.</summary>
public sealed class WorldState
{
    public const int DaysPerMonth = 30;
    public const int DaysPerYear = 360;

    public int Day { get; set; }
    public int Seed { get; init; }
    public string SeedName { get; init; } = "procedural";
    public List<Polity> Polities { get; } = [];
    public List<Province> Provinces { get; } = [];
    public RelationMatrix Relations { get; } = new();
    public List<Treaty> Treaties { get; } = [];
    public List<Supranational> Supranationals { get; } = [];
    public List<War> Wars { get; } = [];
    public List<GeoEvent> Events { get; } = [];
    public long NextTreatyId { get; set; } = 1;
    public long NextWarId { get; set; } = 1;
    public long NextOrgId { get; set; } = 1;

    public int Year => Day / DaysPerYear;
    public int DayOfYear => Day % DaysPerYear;
    public int Month => DayOfYear / DaysPerMonth;

    public Polity Polity(PolityId id) => Polities[id.Value];
    public Province Province(ProvinceId id) => Provinces[id.Value];

    public IEnumerable<War> ActiveWars => Wars.Where(w => w.Active);

    public IEnumerable<Treaty> ActiveTreaties(TreatyKind? kind = null) =>
        Treaties.Where(t => t.Active && (kind is null || t.Kind == kind));

    public IEnumerable<Supranational> ActiveOrgs => Supranationals.Where(o => o.Active);

    public bool AreAtWar(PolityId a, PolityId b) =>
        ActiveWars.Any(w =>
            (w.Attacker == a && w.Defender == b) ||
            (w.Attacker == b && w.Defender == a));

    /// <summary>True if both share a multilateral treaty of <paramref name="kind"/>, or are on opposite sides of a directed one.</summary>
    public bool HaveTreaty(PolityId a, PolityId b, TreatyKind kind)
    {
        foreach (var t in ActiveTreaties(kind))
        {
            if (Treaty.IsDirected(kind))
            {
                if (t.AreOpposed(a, b))
                {
                    return true;
                }
            }
            else if (t.SharesMembership(a, b))
            {
                return true;
            }
        }

        return false;
    }

    public bool AreAllied(PolityId a, PolityId b) => HaveTreaty(a, b, TreatyKind.Alliance);

    public bool AreEmbargoed(PolityId a, PolityId b) =>
        HaveTreaty(a, b, TreatyKind.EconomicEmbargo) || HaveTreaty(a, b, TreatyKind.WeaponTradeEmbargo);

    public bool HasMilitaryAccess(PolityId traveler, PolityId host) =>
        traveler == host
        || AreAllied(traveler, host)
        || HaveTreaty(traveler, host, TreatyKind.MilitaryAccess);

    public Treaty? FindSharedTreaty(PolityId a, PolityId b, TreatyKind kind) =>
        ActiveTreaties(kind).FirstOrDefault(t =>
            Treaty.IsDirected(kind) ? t.AreOpposed(a, b) : t.SharesMembership(a, b));

    public IEnumerable<Treaty> TreatiesContaining(PolityId id, TreatyKind? kind = null) =>
        ActiveTreaties(kind).Where(t => t.Contains(id));

    public void AddEvent(GeoEventKind kind, string message, PolityId? a = null, PolityId? b = null, ProvinceId? province = null)
    {
        Events.Add(new GeoEvent
        {
            Day = Day,
            Kind = kind,
            Message = message,
            PolityA = a,
            PolityB = b,
            Province = province,
        });

        const int maxEvents = 50_000;
        if (Events.Count > maxEvents)
        {
            Events.RemoveRange(0, Events.Count - maxEvents);
        }
    }

    public int CountOwnedProvinces(PolityId polity) =>
        Provinces.Count(p => p.OwnerId == polity);

    public double TotalPower() => Polities.Sum(p => p.PowerScore);

    public int CountActiveTreatiesOfKind(TreatyKind kind) => ActiveTreaties(kind).Count();
}
