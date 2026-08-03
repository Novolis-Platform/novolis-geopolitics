using Novolis.Geopolitics.Core;

namespace Novolis.Geopolitics.Conflict;

/// <summary>Resolves one battle attempt per active war per day: frontiers, staging, and captures.</summary>
public sealed class ConflictResolver(Random rng)
{
    public bool TryResolveFront(WorldState world, War war)
    {
        var fronts = FindFrontiers(world, war.Attacker, war.Defender).ToList();
        if (fronts.Count == 0)
        {
            // Naval raid: coastal provinces if either side has naval power.
            fronts = FindCoastalTargets(world, war.Attacker, war.Defender).ToList();
            if (fronts.Count == 0)
            {
                return false;
            }
        }

        // One battle attempt per day per war.
        var (from, to) = fronts[rng.Next(fronts.Count)];
        var attacker = world.Polity(war.Attacker);
        var defender = world.Polity(war.Defender);
        var target = world.Province(to);

        var atkForce = attacker.Military.Land * 0.7
                       + attacker.Military.Air * 0.4
                       + (target.Coastal ? attacker.Military.Naval * 0.5 : 0);
        var defForce = defender.Military.Land * 0.7
                       + defender.Military.Air * 0.35
                       + (target.Coastal ? defender.Military.Naval * 0.45 : 0);
        atkForce *= 1.0 + attacker.TechLevel * 0.1;
        defForce *= 1.0 + defender.TechLevel * 0.1;
        defForce *= 1.15; // defender terrain bonus

        // Military access / alliance staging bonus when attacking from a host province owner ≠ attacker.
        var stagingOwner = world.Province(from).OwnerId;
        if (stagingOwner != war.Attacker && world.HasMilitaryAccess(war.Attacker, stagingOwner))
        {
            atkForce *= 1.08;
        }

        var roll = rng.NextDouble();
        var atkScore = atkForce * (0.85 + roll * 0.3);
        var defScore = defForce * (0.85 + rng.NextDouble() * 0.3);

        // Casualties always.
        var lossFactor = 0.004 + rng.NextDouble() * 0.006;
        attacker.Military.Land = Math.Max(1, attacker.Military.Land * (1.0 - lossFactor));
        defender.Military.Land = Math.Max(1, defender.Military.Land * (1.0 - lossFactor * 0.9));

        if (atkScore <= defScore)
        {
            return false;
        }

        // Capture.
        var previousOwner = target.OwnerId;
        target.OwnerId = war.Attacker;
        war.ProvincesTakenByAttacker++;
        attacker.Stability = Math.Min(1.0, attacker.Stability + 0.01);
        defender.Stability = Math.Max(0.05, defender.Stability - 0.03);
        world.Relations.Adjust(war.Attacker, war.Defender, -2);
        world.AddEvent(
            GeoEventKind.ProvinceCaptured,
            $"{attacker.Name} took {target.Name} from {defender.Name}",
            war.Attacker,
            war.Defender,
            target.Id);

        // Flip initiative sometimes: defender counter-capture chance next — handled by ongoing fronts.
        _ = previousOwner;
        _ = from;
        return true;
    }

    private static IEnumerable<(ProvinceId From, ProvinceId To)> FindFrontiers(
        WorldState world,
        PolityId attacker,
        PolityId defender)
    {
        foreach (var pr in world.Provinces)
        {
            if (pr.OwnerId != attacker)
            {
                continue;
            }

            foreach (var n in pr.Neighbors)
            {
                if (world.Province(n).OwnerId == defender)
                {
                    yield return (pr.Id, n);
                }
            }
        }
    }

    private static IEnumerable<(ProvinceId From, ProvinceId To)> FindCoastalTargets(
        WorldState world,
        PolityId attacker,
        PolityId defender)
    {
        if (world.Polity(attacker).Military.Naval < 30)
        {
            yield break;
        }

        var staging = world.Provinces.FirstOrDefault(p => p.OwnerId == attacker && p.Coastal);
        if (staging is null)
        {
            yield break;
        }

        foreach (var pr in world.Provinces)
        {
            if (pr.OwnerId == defender && pr.Coastal)
            {
                yield return (staging.Id, pr.Id);
            }
        }
    }
}
