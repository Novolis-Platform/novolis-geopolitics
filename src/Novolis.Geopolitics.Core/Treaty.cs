namespace Novolis.Geopolitics.Core;

/// <summary>
/// Multilateral or directed diplomatic instrument.
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
