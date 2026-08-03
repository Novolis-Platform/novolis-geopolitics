using Novolis.Geopolitics.Core;
using Novolis.Geopolitics.Diplomacy;

namespace Novolis.Geopolitics.Scenarios;

/// <summary>Deterministic post-load setup: varied supranationals per continent + a world forum.</summary>
public static class InstitutionSeeder
{
    /// <summary>Seeds forums, defence pacts, FTAs, customs unions, research councils if none exist.</summary>
    public static void EnsureContinentOrgs(WorldState world, WorldTelemetry? telemetry = null)
    {
        if (world.Supranationals.Count > 0)
        {
            return;
        }

        telemetry ??= new WorldTelemetry();
        var continents = world.Polities.Select(p => p.Continent).Distinct().OrderBy(c => c).ToList();
        var assemblySeats = new List<PolityId>();

        for (var ci = 0; ci < continents.Count; ci++)
        {
            var continent = continents[ci];
            var local = world.Polities.Where(p => p.Continent == continent).ToList();
            if (local.Count < 3)
            {
                continue;
            }

            double Pop(Polity p) =>
                world.Provinces.Where(pr => pr.OwnerId == p.Id).Sum(pr => pr.Population);

            // Warm a pool of candidates so founding charters can form.
            void Warm(IReadOnlyList<PolityId> ids, double floor)
            {
                for (var i = 0; i < ids.Count; i++)
                {
                    for (var j = i + 1; j < ids.Count; j++)
                    {
                        if (world.Relations.Get(ids[i], ids[j]) < floor)
                        {
                            world.Relations.Set(ids[i], ids[j], floor + (i + j) % 12);
                        }
                    }
                }
            }

            // Forum — broad, mid barrier.
            var forum = local.OrderByDescending(Pop).Take(Math.Min(10, local.Count)).Select(p => p.Id).ToList();
            Warm(forum, 18);
            DiplomaticInstruments.CreateOrg(world, telemetry, $"{continent} Forum", continent, forum, SupranationalKind.Forum);

            // Defence alliance — top military.
            var defence = local.OrderByDescending(p => p.Military.Total).Take(4).Select(p => p.Id).ToList();
            Warm(defence, 48);
            DiplomaticInstruments.CreateOrg(world, telemetry, $"{continent} Defence Pact", continent, defence,
                SupranationalKind.DefenceAlliance);

            // Free trade — top GDP (may overlap defence).
            var fta = local.OrderByDescending(p => p.Gdp).Take(5).Select(p => p.Id).ToList();
            Warm(fta, 32);
            DiplomaticInstruments.CreateOrg(world, telemetry, $"{continent} Free Trade Area", continent, fta,
                SupranationalKind.FreeTradeArea);

            // Customs union on even continents; research forum on odd.
            if (ci % 2 == 0)
            {
                var customs = local.OrderByDescending(p => p.Gdp).Take(4).Select(p => p.Id).ToList();
                Warm(customs, 38);
                DiplomaticInstruments.CreateOrg(world, telemetry, $"{continent} Customs Union", continent, customs,
                    SupranationalKind.CustomsUnion);
            }
            else
            {
                var research = local.OrderByDescending(p => p.TechLevel).Take(5).Select(p => p.Id).ToList();
                Warm(research, 34);
                DiplomaticInstruments.CreateOrg(world, telemetry, $"{continent} Research Council", continent, research,
                    SupranationalKind.ResearchForum);
            }

            // One deep political union on the first continent only.
            if (ci == 0)
            {
                var union = local.OrderByDescending(p => p.Gdp).Take(3).Select(p => p.Id).ToList();
                Warm(union, 58);
                DiplomaticInstruments.CreateOrg(world, telemetry, $"{continent} Union", continent, union,
                    SupranationalKind.PoliticalUnion);
            }

            assemblySeats.Add(local.OrderByDescending(p => p.Gdp).First().Id);
        }

        // Global talking shop — one seat per continent.
        if (assemblySeats.Count >= 3)
        {
            for (var i = 0; i < assemblySeats.Count; i++)
            {
                for (var j = i + 1; j < assemblySeats.Count; j++)
                {
                    if (world.Relations.Get(assemblySeats[i], assemblySeats[j]) < 15)
                    {
                        world.Relations.Set(assemblySeats[i], assemblySeats[j], 16 + (i + j) % 10);
                    }
                }
            }

            DiplomaticInstruments.CreateOrg(world, telemetry, "World Assembly", continentHint: null, assemblySeats,
                SupranationalKind.Forum);
        }
    }
}
