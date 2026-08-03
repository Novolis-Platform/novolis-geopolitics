using System.Reflection;
using Novolis.Geopolitics.Core;

namespace Novolis.Geopolitics.Scenarios;

/// <summary>Loads the embedded default world seed, with procedural fallback.</summary>
public static class DefaultWorld
{
    public const string EmbeddedResourceName = "Novolis.Geopolitics.Scenarios.data.world-seed.json";

    public static WorldState Load()
    {
        var asm = typeof(DefaultWorld).Assembly;
        using var stream = asm.GetManifestResourceStream(EmbeddedResourceName);
        if (stream is null)
        {
            return ProceduralWorldGenerator.Generate();
        }

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        var dto = WorldSeedLoader.Parse(json);
        return WorldSeedLoader.ToWorldState(dto);
    }

    public static IReadOnlyList<string> ListEmbeddedResources() =>
        typeof(DefaultWorld).Assembly.GetManifestResourceNames();
}
