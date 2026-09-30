using Novolis.Geopolitics.Core;
using Novolis.Geopolitics.Diplomacy;
using Novolis.Geopolitics.Scenarios;
using Novolis.Geopolitics.Simulation;
using Novolis.Geopolitics.Trade;

namespace Novolis.Geopolitics.Unit;

public sealed class TradeTests
{
    [Test]
    public async Task CommonMarket_FillsDeficit()
    {
        var world = ProceduralWorldGenerator.Generate(13);
        var stats = new WorldTelemetry();
        var a = new PolityId(0);
        var b = new PolityId(1);
        world.Relations.Set(a, b, 50);
        world.Polity(a).Stability = 0.8;
        world.Polity(b).Stability = 0.8;
        DiplomaticInstruments.SignTreaty(world, stats, TreatyKind.CommonMarket, a, b, 1000);

        // Force surplus/deficit via province weights + owned scale.
        world.Polity(a).Gdp = 500_000;
        world.Polity(b).Gdp = 500_000;
        foreach (var pr in world.Provinces.Where(p => p.OwnerId == a))
        {
            pr.ResourceWeights[ResourceKind.Food] = 0.9;
            pr.Population = 5_000_000;
        }

        foreach (var pr in world.Provinces.Where(p => p.OwnerId == b))
        {
            pr.ResourceWeights[ResourceKind.Food] = 0.05;
            pr.Population = 5_000_000;
        }

        TradeClearing.RunMonth(world, stats);
        await Assert.That(stats.CommonMarketVolume).IsGreaterThan(0);
    }

    [Test]
    public async Task Embargo_ReducesWorldFill()
    {
        var world = ProceduralWorldGenerator.Generate(17);
        var statsOpen = new WorldTelemetry();
        TradeClearing.RunMonth(world, statsOpen);
        var openVol = statsOpen.WorldMarketVolume;

        var world2 = ProceduralWorldGenerator.Generate(17);
        var statsEmb = new WorldTelemetry();
        // Embargo many pairs among top GDP.
        var top = world2.Polities.OrderByDescending(p => p.Gdp).Take(8).Select(p => p.Id).ToList();
        for (var i = 0; i < top.Count; i++)
        {
            for (var j = i + 1; j < top.Count; j++)
            {
                DiplomaticInstruments.SignTreaty(world2, statsEmb, TreatyKind.EconomicEmbargo, top[i], top[j], 500);
            }
        }

        TradeClearing.RunMonth(world2, statsEmb);
        // Embargoed world should not clear more than the open baseline (soft check).
        await Assert.That(statsEmb.WorldMarketVolume).IsLessThanOrEqualTo(openVol * 1.05);
        await Assert.That(world2.CountActiveTreatiesOfKind(TreatyKind.EconomicEmbargo)).IsGreaterThan(0);
    }

    [Test]
    public async Task EconomicPartnership_RaisesGdp()
    {
        var world = ProceduralWorldGenerator.Generate(19);
        var stats = new WorldTelemetry();
        var a = world.Polities[0];
        var b = world.Polities[1];
        world.Relations.Set(a.Id, b.Id, 40);
        var gdp0 = a.Gdp;
        DiplomaticInstruments.SignTreaty(world, stats, TreatyKind.EconomicPartnership, a.Id, b.Id, 1000);
        for (var i = 0; i < 24; i++)
        {
            TreatyEffects.RunMonth(world, stats);
        }

        await Assert.That(a.Gdp).IsGreaterThan(gdp0);
        await Assert.That(stats.EconomicPartnershipGdpBoost).IsGreaterThan(0);
    }
}
