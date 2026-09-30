namespace Novolis.Geopolitics.Core;

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
