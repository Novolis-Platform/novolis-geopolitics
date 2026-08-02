using Novolis.Geopolitics.Core;

namespace Novolis.Geopolitics.Simulation;

/// <summary>
/// Middle-layer civic period: builds world context and calls Core <see cref="CivicEngine"/>.
/// Apps / AI only enqueue policy changes — they do not settle stocks.
/// </summary>
public static class CivicPipeline
{
    public static void RunMonth(WorldState world, GeoSimulationStats stats)
    {
        var techAdvances = 0;
        var crises = 0;
        double legitSum = 0;
        double approvalSum = 0;

        foreach (var polity in world.Polities)
        {
            var owned = world.CountOwnedProvinces(polity.Id);
            var home = world.Provinces.Count(p => p.HomePolityId == polity.Id);
            var control = home == 0 ? 1.0 : owned / (double)home;
            var wars = world.ActiveWars.Count(w => w.Attacker == polity.Id || w.Defender == polity.Id);
            var shortage = ResourceKinds.All.Sum(k => polity.Balance[k] < 0 ? -polity.Balance[k] : 0);
            var occupying = world.Provinces.Any(p => p.OwnerId == polity.Id && p.HomePolityId != polity.Id);
            var lostHome = world.Provinces.Any(p => p.HomePolityId == polity.Id && p.OwnerId != polity.Id);
            var researchMult = world.TreatiesContaining(polity.Id, TreatyKind.ResearchPartnership).Any()
                ? 1.15
                : 1.0;

            var beforeTech = polity.TechLevel;
            var beforeTreasury = polity.Treasury;

            CivicEngine.ApplyMonth(polity, new CivicEngine.MonthContext
            {
                ControlRatio = control,
                ActiveWars = wars,
                ResourceShortage = shortage,
                OccupyingForeignLand = occupying,
                LostHomeProvinces = lostHome,
                ResearchMultiplier = researchMult,
            });

            if (polity.TechLevel > beforeTech + 0.05)
            {
                techAdvances++;
                world.AddEvent(
                    GeoEventKind.TechAdvance,
                    $"{polity.Name} tech → {polity.TechLevel:0.0}",
                    polity.Id);
            }

            if (polity.Treasury < 0)
            {
                crises++;
                if (beforeTreasury >= 0 || world.Day % (WorldState.DaysPerMonth * 3) == 0)
                {
                    world.AddEvent(
                        GeoEventKind.BudgetCrisis,
                        $"{polity.Name} treasury crisis ({polity.Treasury:0})",
                        polity.Id);
                }
            }

            if (polity.Civic.Legitimacy < 0.22 && world.Day % (WorldState.DaysPerMonth * 4) == 0)
            {
                world.AddEvent(
                    GeoEventKind.BudgetCrisis,
                    $"{polity.Name} legitimacy crisis ({polity.Civic.Legitimacy:0.00})",
                    polity.Id);
            }

            legitSum += polity.Civic.Legitimacy;
            approvalSum += polity.Civic.Approval;
        }

        stats.TechAdvances += techAdvances;
        stats.BudgetCrises += crises;
        var n = Math.Max(1, world.Polities.Count);
        stats.MeanLegitimacy = legitSum / n;
        stats.MeanApproval = approvalSum / n;
    }
}
