namespace Novolis.Geopolitics.Core;

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
