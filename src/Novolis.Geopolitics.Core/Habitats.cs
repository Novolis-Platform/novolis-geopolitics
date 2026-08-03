namespace Novolis.Geopolitics.Core;

/// <summary>Province habitat role (UI: habitat; Core still uses Province).</summary>
public enum HabitatKind
{
    World = 0,
    Orbital = 1,
    Outpost = 2,
    Industrial = 3,
    Agri = 4,
}

public static class HabitatRules
{
    public static HabitatKind Roll(Random rng, bool coastal) =>
        (rng.NextDouble(), coastal) switch
        {
            (< 0.35, _) => HabitatKind.World,
            (< 0.55, true) => HabitatKind.Orbital,
            (< 0.55, false) => HabitatKind.Outpost,
            (< 0.75, _) => HabitatKind.Industrial,
            _ => HabitatKind.Agri,
        };

    public static string ShortLabel(HabitatKind kind) => kind switch
    {
        HabitatKind.World => "World",
        HabitatKind.Orbital => "Orbital",
        HabitatKind.Outpost => "Outpost",
        HabitatKind.Industrial => "Industrial",
        HabitatKind.Agri => "Agri",
        _ => kind.ToString(),
    };
}
