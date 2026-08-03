using System.Text.Json;
using Novolis.Geopolitics.Scenarios;

var seed = ProceduralWorldGenerator.DefaultSeed;
var attribution =
    "Original procedural fiction geography (novolis-geopolitics). Not derived from SuperPower 2 or proprietary map data. See docs/seed-attribution.md.";

var dto = ProceduralWorldGenerator.GenerateDto(seed, attribution);
// Keep JSON smaller: drop near-neutral relations (regenerated feel still present via remaining pairs + runtime drift).
dto.Relations = dto.Relations.Where(r => Math.Abs(r.Score) >= 8).ToList();

var outPath = args.Length > 0
    ? args[0]
    : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
        "src", "Novolis.Geopolitics.Scenarios", "data", "world-seed.json"));

Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = false });
await File.WriteAllTextAsync(outPath, json);

var world = WorldSeedLoader.ToWorldState(dto);
Console.WriteLine($"Wrote {outPath}");
Console.WriteLine($"Polities={world.Polities.Count} Provinces={world.Provinces.Count} Relations={dto.Relations.Count}");
