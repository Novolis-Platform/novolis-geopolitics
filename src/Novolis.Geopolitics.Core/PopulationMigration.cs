namespace Novolis.Geopolitics.Core;

/// <summary>
/// Spatial population flows driven by Civics pressures, tax differentials, war, and occupation.
/// Mutates <see cref="Province.Population"/> only; nation demography is re-synced by hosts / CivicEngine.
/// </summary>
public static class PopulationMigration
{
    /// <summary>Maximum fraction of a province's population that can leave in one month.</summary>
    public const double MaxOutflowShare = 0.04;

    /// <summary>
    /// Run one month of inter-province migration. Call after civic settlement so pressures are fresh.
    /// Soft-blends polity GDP toward owned population × wealth afterward.
    /// </summary>
    public static void RunMonth(WorldState world, WorldTelemetry telemetry)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(telemetry);

        var netByPolity = world.Polities.ToDictionary(p => p.Id, _ => 0.0);
        var migrated = 0.0;
        var displaced = 0.0;

        // Snapshot populations for stable scoring
        var pops = world.Provinces.ToDictionary(p => p.Id, p => p.Population);

        foreach (var origin in world.Provinces)
        {
            if (pops[origin.Id] < 100)
                continue;

            var originPolity = world.Polity(origin.OwnerId);
            var push = OriginPush(world, origin, originPolity);
            if (push < 0.08)
                continue;

            var candidates = DestinationCandidates(world, origin);
            if (candidates.Count == 0)
                continue;

            var outflow = pops[origin.Id] * MaxOutflowShare * Math.Clamp(push, 0, 1);
            if (outflow < 1)
                continue;

            var pullSum = candidates.Sum(c => c.Pull);
            if (pullSum <= 1e-9)
                continue;

            var left = outflow;
            foreach (var (dest, pull) in candidates)
            {
                var share = pull / pullSum;
                var move = outflow * share;
                if (move < 0.5)
                    continue;
                move = Math.Min(move, left);
                origin.Population = Math.Max(0, origin.Population - move);
                dest.Population += move;
                left -= move;
                migrated += move;

                netByPolity[origin.OwnerId] = netByPolity.GetValueOrDefault(origin.OwnerId) - move;
                netByPolity[dest.OwnerId] = netByPolity.GetValueOrDefault(dest.OwnerId) + move;

                if (origin.OwnerId != origin.HomePolityId || world.AreAtWar(origin.OwnerId, dest.OwnerId))
                    displaced += move;
            }
        }

        foreach (var polity in world.Polities)
        {
            polity.Civic.LastNetMigration = netByPolity.GetValueOrDefault(polity.Id);
            SoftBlendGdp(world, polity);
        }

        telemetry.PopulationMigrated += migrated;
        telemetry.RefugeesDisplaced += displaced;
    }

    static double OriginPush(WorldState world, Province origin, Polity polity)
    {
        var tax = polity.Policy.HouseholdTaxRate;
        var pressure = polity.Civic.EmigrationPressure;
        var war = world.ActiveWars.Count(w => w.Attacker == polity.Id || w.Defender == polity.Id);
        var occupied = origin.OwnerId != origin.HomePolityId;
        var hdGap = Math.Clamp(0.55 - polity.Civic.HumanDevelopment, 0, 0.55);

        return Math.Clamp(
            0.15 * pressure
            + 0.9 * Math.Max(0, tax - 0.22)
            + 0.12 * war
            + 0.35 * (occupied ? 1.0 : 0)
            + 0.25 * hdGap
            + 0.1 * polity.Civic.WarFatigue,
            0,
            1.5);
    }

    static List<(Province Dest, double Pull)> DestinationCandidates(WorldState world, Province origin)
    {
        var list = new List<(Province, double)>();
        var neighborIds = origin.Neighbors.ToHashSet();

        foreach (var dest in world.Provinces)
        {
            if (dest.Id == origin.Id)
                continue;

            var destPolity = world.Polity(dest.OwnerId);
            var adjacent = neighborIds.Contains(dest.Id);
            var sameContinent = world.Polity(origin.OwnerId).Continent == destPolity.Continent;
            if (!adjacent && !sameContinent)
                continue;

            // Prefer not fleeing into active war against origin owner (unless occupied escape to home)
            if (world.AreAtWar(origin.OwnerId, dest.OwnerId) && dest.OwnerId != origin.HomePolityId)
                continue;

            var pull =
                0.25
                + 0.45 * destPolity.Civic.ImmigrationAttractiveness
                + 0.35 * (1.0 - destPolity.Policy.HouseholdTaxRate)
                + 0.25 * destPolity.Civic.HumanDevelopment
                + (adjacent ? 0.2 : 0.05)
                - 0.15 * destPolity.Civic.WarFatigue;

            if (dest.OwnerId == origin.HomePolityId && origin.OwnerId != origin.HomePolityId)
                pull += 0.35; // return home / escape occupation toward homeland-held land

            if (pull > 0.15)
                list.Add((dest, pull));
        }

        return list.OrderByDescending(x => x.Item2).Take(4).ToList();
    }

    static void SoftBlendGdp(WorldState world, Polity polity)
    {
        var ownedPop = world.OwnedPopulation(polity.Id);
        var wealth = world.Provinces.Where(p => p.OwnerId == polity.Id).Sum(p => p.Wealth);
        if (ownedPop <= 0)
            return;

        // Stylized: GDP ~ pop × wealth-intensity; blend 5% toward pop-wealth anchor.
        var anchor = ownedPop * Math.Max(0.01, wealth / Math.Max(1.0, ownedPop)) * 0.08;
        polity.Gdp = polity.Gdp * 0.95 + anchor * 0.05;
    }
}
