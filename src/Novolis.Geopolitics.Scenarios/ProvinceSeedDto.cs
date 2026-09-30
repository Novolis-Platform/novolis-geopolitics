using System.Text.Json;
using System.Text.Json.Serialization;
using Novolis.Geopolitics.Core;

namespace Novolis.Geopolitics.Scenarios;

public sealed class ProvinceSeedDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int OwnerId { get; set; }
    public int HomePolityId { get; set; }
    public double Population { get; set; }
    public double Wealth { get; set; }
    public bool Coastal { get; set; }
    public List<int> Neighbors { get; set; } = [];
    /// <summary>Per-resource productivity weights (length 6); optional for older seeds.</summary>
    public double[]? ResourceWeights { get; set; }
}
