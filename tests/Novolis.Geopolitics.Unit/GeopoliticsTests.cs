using Novolis.Geopolitics.Core;
using Novolis.Geopolitics.Diplomacy;
using Novolis.Geopolitics.Scenarios;
using Novolis.Geopolitics.Simulation;
using Novolis.Geopolitics.Trade;

namespace Novolis.Geopolitics.Unit;

public sealed class WorldSeedTests
{
    [Test]
    public async Task DefaultWorld_Loads_ExpectedScale()
    {
        var world = DefaultWorld.Load();
        await Assert.That(world.Polities.Count).IsEqualTo(200);
        await Assert.That(world.Provinces.Count).IsGreaterThanOrEqualTo(800);
        await Assert.That(world.Provinces.Count).IsLessThanOrEqualTo(1200);
    }

    [Test]
    public async Task Adjacency_IsSymmetric()
    {
        var world = DefaultWorld.Load();
        foreach (var pr in world.Provinces)
        {
            foreach (var n in pr.Neighbors)
            {
                var other = world.Province(n);
                await Assert.That(other.Neighbors.Contains(pr.Id)).IsTrue();
            }
        }
    }

    [Test]
    public async Task EveryProvince_HasOwnerInRange()
    {
        var world = DefaultWorld.Load();
        foreach (var pr in world.Provinces)
        {
            await Assert.That(pr.OwnerId.Value).IsGreaterThanOrEqualTo(0);
            await Assert.That(pr.OwnerId.Value).IsLessThan(world.Polities.Count);
            await Assert.That(pr.Neighbors.Count).IsGreaterThan(0);
            await Assert.That(pr.ResourceWeights.Sum).IsGreaterThan(0.5);
        }
    }

    [Test]
    public async Task Bootstrap_CreatesVariedOrgKinds()
    {
        var world = ProceduralWorldGenerator.Generate(42);
        var stats = new WorldTelemetry();
        InstitutionSeeder.EnsureContinentOrgs(world, stats);
        await Assert.That(world.ActiveOrgs.Count()).IsGreaterThanOrEqualTo(20);
        await Assert.That(world.ActiveOrgs.Any(o => o.Kind == SupranationalKind.Forum)).IsTrue();
        await Assert.That(world.ActiveOrgs.Any(o => o.Kind == SupranationalKind.DefenceAlliance)).IsTrue();
        await Assert.That(world.ActiveOrgs.Any(o => o.Kind == SupranationalKind.FreeTradeArea)).IsTrue();
        await Assert.That(world.ActiveOrgs.Any(o => o.Kind == SupranationalKind.CustomsUnion)).IsTrue();
        await Assert.That(world.ActiveOrgs.Any(o => o.Kind == SupranationalKind.ResearchForum)).IsTrue();
        await Assert.That(world.ActiveOrgs.Any(o => o.Kind == SupranationalKind.PoliticalUnion)).IsTrue();
        await Assert.That(world.CountActiveTreatiesOfKind(TreatyKind.Alliance)).IsGreaterThanOrEqualTo(8);
        await Assert.That(world.CountActiveTreatiesOfKind(TreatyKind.CulturalExchanges)).IsGreaterThanOrEqualTo(8);
    }

    [Test]
    public async Task DiplomaticRules_RejectsLowRelationAlliance()
    {
        var world = ProceduralWorldGenerator.Generate(8);
        world.Relations.Set(new PolityId(0), new PolityId(1), 10);
        var refusal = DiplomaticRules.EvaluateBilateral(
            world, TreatyKind.Alliance, new PolityId(0), new PolityId(1));
        await Assert.That(refusal).IsEqualTo(TreatyRefusal.RelationsTooLow);
    }
}
