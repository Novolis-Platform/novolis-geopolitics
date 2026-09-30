using Novolis.Geopolitics.Core;
using Novolis.Geopolitics.Diplomacy;
using Novolis.Geopolitics.Scenarios;
using Novolis.Geopolitics.Simulation;
using Novolis.Geopolitics.Trade;

namespace Novolis.Geopolitics.Unit;

public sealed class HeadlessSmokeTests
{
    /// <summary>Full-world 10y advance (~55s). Opt-in only so Platform.slnx stays fast.</summary>
    [Test]
    [Explicit]
    public async Task TenYear_Advance_Completes()
    {
        var world = DefaultWorld.Load();
        var opening = world.Provinces.ToDictionary(p => p.Id.Value, p => p.OwnerId.Value);
        var sim = new WorldSimulation(world, rngSeed: world.Seed);
        await Assert.That(world.ActiveOrgs.Count()).IsGreaterThanOrEqualTo(20);
        sim.AdvanceYears(10);

        await Assert.That(world.Day).IsEqualTo(10 * WorldState.DaysPerYear);
        await Assert.That(world.Events.Count).IsLessThanOrEqualTo(50_000);
        await Assert.That(world.Polities.Count).IsEqualTo(200);

        var churn = world.Provinces.Count(p => opening[p.Id.Value] != p.OwnerId.Value);
        var alive = sim.Telemetry.WarsStarted + sim.Telemetry.TreatiesSigned + sim.Telemetry.TechAdvances + churn
                    + (int)sim.Telemetry.CommonMarketVolume;
        await Assert.That(alive).IsGreaterThan(0);
        await Assert.That(sim.Telemetry.CommonMarketVolume + sim.Telemetry.WorldMarketVolume).IsGreaterThan(0);
    }
}
