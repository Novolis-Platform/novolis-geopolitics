using Novolis.Geopolitics.Core;

namespace Novolis.Geopolitics.Trade;

/// <summary>Monthly resource production, common-market clear, world market, embargo gates.</summary>
public static class TradeClearing
{
    public static void RunMonth(WorldState world, WorldTelemetry telemetry)
    {
        foreach (var polity in world.Polities)
        {
            ComputeDomestic(world, polity);
        }

        // Working surplus/deficit buffers.
        var surplus = world.Polities.Select(p => p.Balance.Clone()).ToArray();

        ClearCommonMarkets(world, surplus, telemetry);
        ClearWorldMarket(world, surplus, telemetry);
        ApplyShortageAndSurplus(world, surplus, telemetry);
    }

    private static void ComputeDomestic(WorldState world, Polity polity)
    {
        var prod = new ResourceVector();
        var cons = new ResourceVector();
        var owned = world.Provinces.Where(p => p.OwnerId == polity.Id).ToList();
        var pop = owned.Sum(p => p.Population);
        var wealth = owned.Sum(p => p.Wealth);
        var tech = polity.TechLevel;

        foreach (var pr in owned)
        {
            var baseOut = (pr.Population / 1_000_000.0) * (0.6 + pr.Wealth / Math.Max(1, wealth) * 0.8) * (0.8 + tech * 0.15);
            foreach (var kind in ResourceKinds.All)
            {
                var w = pr.ResourceWeights[kind];
                if (w <= 0)
                {
                    w = 1.0 / ResourceKinds.Count;
                }

                prod[kind] += baseOut * w * 28.0;
            }

            if (pr.Coastal)
            {
                prod[ResourceKind.Energy] += baseOut * 0.35;
                prod[ResourceKind.Goods] += baseOut * 0.25;
            }
        }

        // Consumption scales with population and military (tuned under typical production).
        var popFactor = Math.Max(0.2, pop / 1_000_000.0);
        cons[ResourceKind.Food] = popFactor * 5.5;
        cons[ResourceKind.Energy] = popFactor * 4.0 + polity.Military.Total * 0.004;
        cons[ResourceKind.Materials] = popFactor * 3.5 + wealth / 400_000.0;
        cons[ResourceKind.Goods] = popFactor * 4.5;
        cons[ResourceKind.MilitaryGoods] = polity.Military.Total * 0.035 + 1.0;
        cons[ResourceKind.Rare] = popFactor * 0.7 + tech * 0.25;

        for (var i = 0; i < ResourceKinds.Count; i++)
        {
            var k = (ResourceKind)i;
            polity.Production[k] = prod[k];
            polity.Consumption[k] = cons[k];
            polity.Balance[k] = prod[k] - cons[k];
        }
    }

    private static void ClearCommonMarkets(WorldState world, ResourceVector[] surplus, WorldTelemetry telemetry)
    {
        foreach (var cm in world.ActiveTreaties(TreatyKind.CommonMarket))
        {
            var members = cm.Members.Select(m => m.Value).ToArray();
            if (members.Length < 2)
            {
                continue;
            }

            foreach (var kind in ResourceKinds.All)
            {
                double pool = 0;
                var needers = new List<int>();
                foreach (var idx in members)
                {
                    if (surplus[idx][kind] > 0)
                    {
                        pool += surplus[idx][kind];
                    }
                    else if (surplus[idx][kind] < 0)
                    {
                        needers.Add(idx);
                    }
                }

                if (pool <= 0 || needers.Count == 0)
                {
                    continue;
                }

                var totalNeed = needers.Sum(i => -surplus[i][kind]);
                var filled = Math.Min(pool, totalNeed);
                telemetry.CommonMarketVolume += filled;

                // Draw from surplus members proportional to surplus.
                foreach (var idx in members)
                {
                    if (surplus[idx][kind] <= 0)
                    {
                        continue;
                    }

                    var share = surplus[idx][kind] / pool;
                    surplus[idx][kind] -= filled * share;
                }

                foreach (var idx in needers)
                {
                    var need = -surplus[idx][kind];
                    var get = filled * (need / totalNeed);
                    surplus[idx][kind] += get;
                }
            }
        }
    }

    private static void ClearWorldMarket(WorldState world, ResourceVector[] surplus, WorldTelemetry telemetry)
    {
        foreach (var kind in ResourceKinds.All)
        {
            var sellers = new List<(int Idx, double Amt, double Gdp)>();
            var buyers = new List<(int Idx, double Need, double Gdp)>();
            for (var i = 0; i < world.Polities.Count; i++)
            {
                if (surplus[i][kind] > 0.01)
                {
                    sellers.Add((i, surplus[i][kind], world.Polities[i].Gdp));
                }
                else if (surplus[i][kind] < -0.01)
                {
                    buyers.Add((i, -surplus[i][kind], world.Polities[i].Gdp));
                }
            }

            if (sellers.Count == 0 || buyers.Count == 0)
            {
                continue;
            }

            var totalSell = sellers.Sum(s => s.Amt * (0.5 + s.Gdp / (sellers.Sum(x => x.Gdp) + 1)));
            var totalBuy = buyers.Sum(b => b.Need);
            var volume = Math.Min(sellers.Sum(s => s.Amt), totalBuy) * 0.65; // partial clear
            if (volume <= 0)
            {
                continue;
            }

            telemetry.WorldMarketVolume += volume;

            // GDP-weighted allocation with embargo filter.
            var sellPool = sellers.Sum(s => s.Amt);
            foreach (var (idx, amt, gdp) in sellers)
            {
                var contrib = volume * (amt / sellPool);
                surplus[idx][kind] -= contrib;
                // Treasury crumb for exports.
                world.Polities[idx].Treasury += contrib * 0.5;
            }

            var buyNeed = buyers.Sum(b => b.Need);
            foreach (var (idx, need, gdp) in buyers)
            {
                // Skip if embargoed against all major sellers (simplified: reduce fill).
                var blocked = sellers.Count(s => world.AreEmbargoed(new PolityId(idx), new PolityId(s.Idx)));
                var blockFactor = sellers.Count == 0 ? 1 : 1.0 - blocked / (double)sellers.Count * 0.85;
                var get = volume * (need / buyNeed) * blockFactor;
                surplus[idx][kind] += get;
                world.Polities[idx].Treasury -= get * 0.55;
                _ = gdp;
                _ = totalSell;
            }
        }
    }

    private static void ApplyShortageAndSurplus(WorldState world, ResourceVector[] surplus, WorldTelemetry telemetry)
    {
        for (var i = 0; i < world.Polities.Count; i++)
        {
            var p = world.Polities[i];
            var shortage = 0.0;
            foreach (var kind in ResourceKinds.All)
            {
                p.Balance[kind] = surplus[i][kind];
                if (surplus[i][kind] < 0)
                {
                    shortage += -surplus[i][kind];
                }
                else if (surplus[i][kind] > 0)
                {
                    p.Treasury += surplus[i][kind] * 0.05;
                }
            }

            if (shortage > 12)
            {
                telemetry.ResourceShortageEvents++;
                p.Stability = Math.Max(0.05, p.Stability - Math.Min(0.02, shortage * 0.0008));
                p.Gdp *= 1.0 - Math.Min(0.002, shortage * 0.00005);
                if (world.Day % (WorldState.DaysPerMonth * 6) == 0 && shortage > 40)
                {
                    world.AddEvent(
                        GeoEventKind.ResourceShortage,
                        $"{p.Name} resource shortage ({shortage:0.0})",
                        p.Id);
                }
            }
        }
    }
}
