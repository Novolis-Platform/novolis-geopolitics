using System.Text.Json;
using System.Text.Json.Serialization;
using Novolis.Geopolitics.Core;

namespace Novolis.Geopolitics.Scenarios;

public sealed class RelationSeedDto
{
    public int A { get; set; }
    public int B { get; set; }
    public double Score { get; set; }
}
