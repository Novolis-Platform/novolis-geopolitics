using System.Text.Json;
using System.Text.Json.Serialization;
using Novolis.Geopolitics.Core;

namespace Novolis.Geopolitics.Scenarios;

public sealed class WorldSeedDto
{
    public int Seed { get; set; }
    public string Name { get; set; } = "procedural";
    public string Attribution { get; set; } = "";
    public List<PolitySeedDto> Polities { get; set; } = [];
    public List<ProvinceSeedDto> Provinces { get; set; } = [];
    public List<RelationSeedDto> Relations { get; set; } = [];
}
