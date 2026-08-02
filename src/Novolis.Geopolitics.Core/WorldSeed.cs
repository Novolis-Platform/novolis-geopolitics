using System.Text.Json;
using System.Text.Json.Serialization;

namespace Novolis.Geopolitics.Core;

public sealed class WorldSeedDto
{
    public int Seed { get; set; }
    public string Name { get; set; } = "procedural";
    public string Attribution { get; set; } = "";
    public List<PolitySeedDto> Polities { get; set; } = [];
    public List<ProvinceSeedDto> Provinces { get; set; } = [];
    public List<RelationSeedDto> Relations { get; set; } = [];
}

public sealed class PolitySeedDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Continent { get; set; } = "";
    public double Gdp { get; set; }
    public double Treasury { get; set; }
    public double TaxRate { get; set; }
    public double MilitaryBudgetShare { get; set; }
    public double Stability { get; set; }
    public double TechLevel { get; set; }
    public double Land { get; set; }
    public double Air { get; set; }
    public double Naval { get; set; }

    /// <summary>Optional; defaults to Democracy when missing from older seeds.</summary>
    public string? Government { get; set; }

    public double? TransferShare { get; set; }
    public double? InfrastructureShare { get; set; }
    public double? PropagandaShare { get; set; }
    public double? Legitimacy { get; set; }
    public double? Approval { get; set; }
    public double? Corruption { get; set; }
    public double? HumanDevelopment { get; set; }
}

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

public sealed class RelationSeedDto
{
    public int A { get; set; }
    public int B { get; set; }
    public double Score { get; set; }
}

public static class WorldSeedLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    public static WorldSeedDto Parse(string json) =>
        JsonSerializer.Deserialize<WorldSeedDto>(json, JsonOptions)
        ?? throw new InvalidOperationException("World seed JSON deserialized to null.");

    public static string Serialize(WorldSeedDto dto) =>
        JsonSerializer.Serialize(dto, JsonOptions);

    public static WorldState ToWorldState(WorldSeedDto dto)
    {
        var world = new WorldState
        {
            Seed = dto.Seed,
            SeedName = dto.Name,
            Day = 0,
        };

        foreach (var p in dto.Polities.OrderBy(x => x.Id))
        {
            if (p.Id != world.Polities.Count)
            {
                throw new InvalidOperationException($"Polity ids must be dense from 0; expected {world.Polities.Count}, got {p.Id}.");
            }

            var gov = ParseGovernment(p.Government);
            var stability = p.Stability;
            world.Polities.Add(new Polity
            {
                Id = new PolityId(p.Id),
                Name = p.Name,
                Continent = p.Continent,
                Government = gov,
                Gdp = p.Gdp,
                Treasury = p.Treasury,
                TaxRate = p.TaxRate,
                MilitaryBudgetShare = p.MilitaryBudgetShare,
                Stability = stability,
                TechLevel = p.TechLevel,
                Military = new MilitaryForce
                {
                    Land = p.Land,
                    Air = p.Air,
                    Naval = p.Naval,
                },
                Policy =
                {
                    HouseholdTaxRate = p.TaxRate,
                    MilitaryShare = p.MilitaryBudgetShare,
                    TransferShare = p.TransferShare ?? 0.25,
                    InfrastructureShare = p.InfrastructureShare ?? 0.45,
                    PropagandaShare = p.PropagandaShare ?? 0.20,
                },
                Civic =
                {
                    Legitimacy = p.Legitimacy ?? Math.Clamp(stability * 0.95, 0.2, 0.95),
                    Approval = p.Approval ?? Math.Clamp(stability * 0.85, 0.2, 0.9),
                    Corruption = p.Corruption ?? Math.Clamp(0.35 - stability * 0.25, 0.05, 0.45),
                    HumanDevelopment = p.HumanDevelopment ?? Math.Clamp(0.3 + stability * 0.4, 0.25, 1.2),
                },
            });
        }

        foreach (var pr in dto.Provinces.OrderBy(x => x.Id))
        {
            if (pr.Id != world.Provinces.Count)
            {
                throw new InvalidOperationException($"Province ids must be dense from 0; expected {world.Provinces.Count}, got {pr.Id}.");
            }

            var weights = ResourceVector.FromArray(pr.ResourceWeights);
            if (weights.Sum <= 0)
            {
                // Flat fallback for legacy seeds.
                foreach (var k in ResourceKinds.All)
                {
                    weights[k] = 1.0 / ResourceKinds.Count;
                }
            }

            world.Provinces.Add(new Province
            {
                Id = new ProvinceId(pr.Id),
                Name = pr.Name,
                OwnerId = new PolityId(pr.OwnerId),
                HomePolityId = new PolityId(pr.HomePolityId),
                Population = pr.Population,
                Wealth = pr.Wealth,
                Coastal = pr.Coastal,
                Neighbors = pr.Neighbors.Select(n => new ProvinceId(n)).ToList(),
                ResourceWeights = weights,
            });
        }

        foreach (var r in dto.Relations)
        {
            world.Relations.Set(new PolityId(r.A), new PolityId(r.B), r.Score);
        }

        return world;
    }

    public static WorldSeedDto FromWorldState(WorldState world, string attribution = "")
    {
        var dto = new WorldSeedDto
        {
            Seed = world.Seed,
            Name = world.SeedName,
            Attribution = attribution,
        };

        foreach (var p in world.Polities)
        {
            dto.Polities.Add(new PolitySeedDto
            {
                Id = p.Id.Value,
                Name = p.Name,
                Continent = p.Continent,
                Gdp = p.Gdp,
                Treasury = p.Treasury,
                TaxRate = p.Policy.HouseholdTaxRate,
                MilitaryBudgetShare = p.Policy.MilitaryShare,
                Stability = p.Stability,
                TechLevel = p.TechLevel,
                Land = p.Military.Land,
                Air = p.Military.Air,
                Naval = p.Military.Naval,
                Government = p.Government.ToString(),
                TransferShare = p.Policy.TransferShare,
                InfrastructureShare = p.Policy.InfrastructureShare,
                PropagandaShare = p.Policy.PropagandaShare,
                Legitimacy = p.Civic.Legitimacy,
                Approval = p.Civic.Approval,
                Corruption = p.Civic.Corruption,
                HumanDevelopment = p.Civic.HumanDevelopment,
            });
        }

        foreach (var pr in world.Provinces)
        {
            dto.Provinces.Add(new ProvinceSeedDto
            {
                Id = pr.Id.Value,
                Name = pr.Name,
                OwnerId = pr.OwnerId.Value,
                HomePolityId = pr.HomePolityId.Value,
                Population = pr.Population,
                Wealth = pr.Wealth,
                Coastal = pr.Coastal,
                Neighbors = pr.Neighbors.Select(n => n.Value).OrderBy(x => x).ToList(),
                ResourceWeights = pr.ResourceWeights.ToArray(),
            });
        }

        foreach (var (key, score) in world.Relations.Snapshot)
        {
            var a = (int)(key >> 32);
            var b = (int)(key & 0xFFFFFFFF);
            dto.Relations.Add(new RelationSeedDto { A = a, B = b, Score = score });
        }

        return dto;
    }

    private static GovernmentType ParseGovernment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return GovernmentType.Democracy;
        }

        return Enum.TryParse<GovernmentType>(value, ignoreCase: true, out var g)
            ? g
            : GovernmentType.Democracy;
    }
}
