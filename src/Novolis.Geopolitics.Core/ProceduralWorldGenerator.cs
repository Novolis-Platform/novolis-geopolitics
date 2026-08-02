namespace Novolis.Geopolitics.Core;

/// <summary>
/// Deterministic procedural world: ~200 polities, ~1k provinces, continent grids + bridges.
/// Original fiction geography — not derived from SuperPower 2 or proprietary datasets.
/// </summary>
public static class ProceduralWorldGenerator
{
    private static readonly string[] Continents =
    [
        "Aurora", "Boreal", "Cinder", "Delta", "Ember", "Frost", "Gale", "Haven",
    ];

    private static readonly string[] Prefixes =
    [
        "Val", "Nor", "Kas", "Mir", "Tor", "Bel", "Zen", "Ara", "Sol", "Kin",
        "Dar", "Lun", "Rav", "Osh", "Pel", "Qui", "Umb", "Wyr", "Xor", "Yul",
    ];

    private static readonly string[] Suffixes =
    [
        "ia", "or", "an", "um", "is", "ara", "enn", "ost", "heim", "gard",
        "mark", "land", "stan", "ovia", "ria", "eth", "une", "arae", "esk", "yr",
    ];

    public const int DefaultSeed = 20260802;
    public const int TargetPolities = 200;
    public const int ProvincesPerPolityMin = 4;
    public const int ProvincesPerPolityMax = 6;

    public static WorldState Generate(int seed = DefaultSeed)
    {
        var rng = new Random(seed);
        var world = new WorldState { Seed = seed, SeedName = "procedural-v2", Day = 0 };

        var politiesPerContinent = TargetPolities / Continents.Length;
        var remainder = TargetPolities % Continents.Length;
        var polityId = 0;
        var provinceId = 0;

        // Contiguous province id ranges per continent for bridge wiring.
        var continentProvinceRanges = new List<(int Start, int End)>();

        for (var c = 0; c < Continents.Length; c++)
        {
            var continent = Continents[c];
            var count = politiesPerContinent + (c < remainder ? 1 : 0);
            var continentStartProvince = provinceId;

            // Lay polities in a roughly square grid for neighbor discovery.
            var cols = (int)Math.Ceiling(Math.Sqrt(count));
            var rows = (int)Math.Ceiling(count / (double)cols);
            var cellPolities = new PolityId?[rows, cols];
            var cellProvinceLists = new List<ProvinceId>[rows, cols];

            for (var i = 0; i < count; i++)
            {
                var row = i / cols;
                var col = i % cols;
                var name = MakeName(rng, polityId);
                var gdp = 40_000 + rng.NextDouble() * 960_000;
                var coastalBias = row == 0 || row == rows - 1 || col == 0 || col == cols - 1;
                var tech = 0.8 + rng.NextDouble() * 1.4;
                var land = 80 + rng.NextDouble() * 420;
                var air = 20 + rng.NextDouble() * 180;
                var naval = coastalBias ? 40 + rng.NextDouble() * 220 : 5 + rng.NextDouble() * 40;

                var gov = GovernmentRules.Roll(rng);
                var tax = 0.15 + rng.NextDouble() * 0.2;
                var milShare = 0.18 + rng.NextDouble() * 0.25;
                var polity = new Polity
                {
                    Id = new PolityId(polityId),
                    Name = name,
                    Continent = continent,
                    Government = gov,
                    Gdp = gdp,
                    Treasury = gdp * (0.08 + rng.NextDouble() * 0.12),
                    TaxRate = tax,
                    MilitaryBudgetShare = milShare,
                    Stability = 0.45 + rng.NextDouble() * 0.5,
                    TechLevel = tech,
                    Military = new MilitaryForce { Land = land, Air = air, Naval = naval },
                    Policy =
                    {
                        HouseholdTaxRate = tax,
                        MilitaryShare = milShare,
                        TransferShare = 0.15 + rng.NextDouble() * 0.25,
                        InfrastructureShare = 0.35 + rng.NextDouble() * 0.3,
                        PropagandaShare = 0.1 + rng.NextDouble() * 0.25,
                    },
                    Civic =
                    {
                        Legitimacy = 0.4 + rng.NextDouble() * 0.45,
                        Approval = 0.35 + rng.NextDouble() * 0.45,
                        Corruption = 0.05 + rng.NextDouble() * 0.35,
                        HumanDevelopment = 0.35 + rng.NextDouble() * 0.5,
                    },
                };
                world.Polities.Add(polity);
                cellPolities[row, col] = polity.Id;

                var provinceCount = rng.Next(ProvincesPerPolityMin, ProvincesPerPolityMax + 1);
                var local = new List<ProvinceId>(provinceCount);
                for (var p = 0; p < provinceCount; p++)
                {
                    var coastal = coastalBias && (p == 0 || rng.NextDouble() < 0.45);
                    var pr = new Province
                    {
                        Id = new ProvinceId(provinceId),
                        Name = $"{name}-{p + 1}",
                        OwnerId = polity.Id,
                        HomePolityId = polity.Id,
                        Population = 200_000 + rng.NextDouble() * 4_800_000,
                        Wealth = gdp / provinceCount * (0.7 + rng.NextDouble() * 0.6),
                        Coastal = coastal,
                        ResourceWeights = RollResourceWeights(rng, coastal, c),
                    };
                    world.Provinces.Add(pr);
                    local.Add(pr.Id);
                    provinceId++;
                }

                // Ring adjacency inside polity.
                for (var p = 0; p < local.Count; p++)
                {
                    var next = local[(p + 1) % local.Count];
                    Link(world, local[p], next);
                    if (local.Count > 3 && p + 2 < local.Count)
                    {
                        Link(world, local[p], local[p + 2]);
                    }
                }

                cellProvinceLists[row, col] = local;
                polityId++;
            }

            // Cross-polity borders from grid neighbors.
            for (var row = 0; row < rows; row++)
            {
                for (var col = 0; col < cols; col++)
                {
                    if (cellPolities[row, col] is null)
                    {
                        continue;
                    }

                    var here = cellProvinceLists[row, col]!;
                    TryBridgeCells(world, rng, here, cellProvinceLists, row, col + 1, rows, cols);
                    TryBridgeCells(world, rng, here, cellProvinceLists, row + 1, col, rows, cols);
                }
            }

            continentProvinceRanges.Add((continentStartProvince, provinceId));
        }

        // Land bridges between sequential continents (and wrap last→first lightly).
        for (var c = 0; c < continentProvinceRanges.Count; c++)
        {
            var next = (c + 1) % continentProvinceRanges.Count;
            var (a0, a1) = continentProvinceRanges[c];
            var (b0, b1) = continentProvinceRanges[next];
            if (a1 <= a0 || b1 <= b0)
            {
                continue;
            }

            var bridges = c == continentProvinceRanges.Count - 1 ? 2 : 4;
            for (var i = 0; i < bridges; i++)
            {
                var pa = new ProvinceId(rng.Next(a0, a1));
                var pb = new ProvinceId(rng.Next(b0, b1));
                Link(world, pa, pb);
                world.Provinces[pa.Value].Coastal = true;
                world.Provinces[pb.Value].Coastal = true;
            }
        }

        // Neighbor relations: friendly if same continent, cooler across water.
        for (var i = 0; i < world.Polities.Count; i++)
        {
            for (var j = i + 1; j < world.Polities.Count; j++)
            {
                var a = world.Polities[i];
                var b = world.Polities[j];
                var shareBorder = SharesBorder(world, a.Id, b.Id);
                double score;
                if (a.Continent == b.Continent)
                {
                    score = shareBorder ? 10 + rng.NextDouble() * 40 : -5 + rng.NextDouble() * 25;
                }
                else
                {
                    score = shareBorder ? -10 + rng.NextDouble() * 20 : -30 + rng.NextDouble() * 35;
                }

                // Great-power rivalry: top GDP pairs slightly colder.
                if (a.Gdp > 700_000 && b.Gdp > 700_000)
                {
                    score -= 15;
                }

                world.Relations.Set(a.Id, b.Id, score);
            }
        }

        return world;
    }

    public static WorldSeedDto GenerateDto(int seed = DefaultSeed, string attribution = "")
    {
        var world = Generate(seed);
        return WorldSeedLoader.FromWorldState(world, attribution);
    }

    private static void TryBridgeCells(
        WorldState world,
        Random rng,
        List<ProvinceId> here,
        List<ProvinceId>?[,] cells,
        int row,
        int col,
        int rows,
        int cols)
    {
        if (row < 0 || col < 0 || row >= rows || col >= cols)
        {
            return;
        }

        var there = cells[row, col];
        if (there is null || there.Count == 0 || here.Count == 0)
        {
            return;
        }

        var links = 1 + rng.Next(0, 2);
        for (var i = 0; i < links; i++)
        {
            Link(world, here[rng.Next(here.Count)], there[rng.Next(there.Count)]);
        }
    }

    private static void Link(WorldState world, ProvinceId a, ProvinceId b)
    {
        if (a == b)
        {
            return;
        }

        var pa = world.Provinces[a.Value];
        var pb = world.Provinces[b.Value];
        if (!pa.Neighbors.Contains(b))
        {
            pa.Neighbors.Add(b);
        }

        if (!pb.Neighbors.Contains(a))
        {
            pb.Neighbors.Add(a);
        }
    }

    private static bool SharesBorder(WorldState world, PolityId a, PolityId b)
    {
        foreach (var pr in world.Provinces)
        {
            if (pr.OwnerId != a)
            {
                continue;
            }

            foreach (var n in pr.Neighbors)
            {
                if (world.Provinces[n.Value].OwnerId == b)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static string MakeName(Random rng, int id)
    {
        var name = Prefixes[rng.Next(Prefixes.Length)] + Suffixes[rng.Next(Suffixes.Length)];
        // Ensure uniqueness-ish with id salt when collisions happen in practice.
        if (rng.NextDouble() < 0.15)
        {
            name += Suffixes[rng.Next(Suffixes.Length)];
        }

        return char.ToUpperInvariant(name[0]) + name[1..] + (id % 17 == 0 ? id.ToString() : "");
    }

    /// <summary>Continent-biased resource mix (fiction; not real Earth geology).</summary>
    private static ResourceVector RollResourceWeights(Random rng, bool coastal, int continentIndex)
    {
        var raw = new double[ResourceKinds.Count];
        for (var i = 0; i < raw.Length; i++)
        {
            raw[i] = 0.4 + rng.NextDouble();
        }

        // Bias by continent index and coast.
        raw[(int)ResourceKind.Food] += continentIndex is 0 or 3 or 7 ? 0.6 : 0.1;
        raw[(int)ResourceKind.Energy] += continentIndex is 2 or 4 ? 0.8 : 0.15;
        raw[(int)ResourceKind.Materials] += continentIndex is 1 or 5 ? 0.7 : 0.2;
        raw[(int)ResourceKind.Rare] += continentIndex is 5 or 6 ? 0.5 : 0.05;
        if (coastal)
        {
            raw[(int)ResourceKind.Goods] += 0.4;
            raw[(int)ResourceKind.Energy] += 0.25;
        }

        raw[(int)ResourceKind.MilitaryGoods] += 0.2 + rng.NextDouble() * 0.3;

        var sum = raw.Sum();
        var v = new ResourceVector();
        for (var i = 0; i < raw.Length; i++)
        {
            v[(ResourceKind)i] = raw[i] / sum;
        }

        return v;
    }
}
