using Novolis.Geopolitics.Core;
using Novolis.Geopolitics.Diplomacy;
using Novolis.Geopolitics.Scenarios;
using Novolis.Geopolitics.Simulation;
using Novolis.Geopolitics.Trade;

namespace Novolis.Geopolitics.Unit;

public sealed class DiplomacyCombatTests
{
    [Test]
    public async Task DeclareWar_AndCapture_FlipsOwnership()
    {
        var world = ProceduralWorldGenerator.Generate(99);
        var sim = new WorldSimulation(world, rngSeed: 99);

        PolityId? a = null;
        PolityId? b = null;
        foreach (var pr in world.Provinces)
        {
            foreach (var n in pr.Neighbors)
            {
                var other = world.Province(n).OwnerId;
                if (other != pr.OwnerId)
                {
                    a = pr.OwnerId;
                    b = other;
                    break;
                }
            }

            if (a is not null)
            {
                break;
            }
        }

        await Assert.That(a.HasValue).IsTrue();
        await Assert.That(b.HasValue).IsTrue();

        var attacker = a!.Value;
        var defender = b!.Value;
        world.Polity(attacker).Military.Land = 5000;
        world.Polity(attacker).Military.Air = 2000;
        world.Polity(attacker).Military.Naval = 500;
        world.Polity(defender).Military.Land = 10;
        world.Polity(defender).Military.Air = 5;
        world.Polity(defender).Military.Naval = 1;

        var war = DiplomaticInstruments.DeclareWar(world, sim.Telemetry, attacker, defender);
        await Assert.That(war).IsNotNull();
        await Assert.That(world.AreAtWar(attacker, defender)).IsTrue();

        var before = world.CountOwnedProvinces(attacker);
        sim.Advance(180);
        var after = world.CountOwnedProvinces(attacker);
        await Assert.That(after).IsGreaterThan(before);
        await Assert.That(sim.Telemetry.ProvincesCaptured).IsGreaterThan(0);
    }

    [Test]
    public async Task Alliance_BlocksDeclareWar()
    {
        var world = ProceduralWorldGenerator.Generate(7);
        var stats = new WorldTelemetry();
        var a = new PolityId(0);
        var b = new PolityId(1);
        world.Relations.Set(a, b, 60);
        world.Polity(a).Stability = 0.8;
        world.Polity(b).Stability = 0.8;
        var treaty = DiplomaticInstruments.SignTreaty(world, stats, TreatyKind.Alliance, a, b, 1000);
        await Assert.That(treaty).IsNotNull();
        var war = DiplomaticInstruments.DeclareWar(world, stats, a, b);
        await Assert.That(war).IsNull();
        await Assert.That(world.AreAtWar(a, b)).IsFalse();
    }

    [Test]
    public async Task PeaceTreaty_BlocksEarlyRedeclaration()
    {
        var world = ProceduralWorldGenerator.Generate(11);
        var stats = new WorldTelemetry();
        var a = new PolityId(0);
        var b = new PolityId(1);
        world.Day = 100;
        DiplomaticInstruments.SignTreaty(world, stats, TreatyKind.Peace, a, b, 360);
        var war = DiplomaticInstruments.DeclareWar(world, stats, a, b);
        await Assert.That(war).IsNull();
    }

    [Test]
    public async Task Multilateral_Join_AddsMember()
    {
        var world = ProceduralWorldGenerator.Generate(3);
        var stats = new WorldTelemetry();
        world.Relations.Set(new PolityId(0), new PolityId(1), 50);
        world.Relations.Set(new PolityId(0), new PolityId(2), 50);
        world.Relations.Set(new PolityId(1), new PolityId(2), 50);
        world.Polity(new PolityId(0)).Stability = 0.8;
        world.Polity(new PolityId(1)).Stability = 0.8;
        var t = DiplomaticInstruments.SignTreaty(world, stats, TreatyKind.CommonMarket, new PolityId(0), new PolityId(1), 500);
        await Assert.That(t).IsNotNull();
        var joined = DiplomaticInstruments.JoinTreaty(world, stats, t!, new PolityId(2));
        await Assert.That(joined).IsTrue();
        await Assert.That(t!.Members.Count).IsEqualTo(3);
        await Assert.That(world.HaveTreaty(new PolityId(0), new PolityId(2), TreatyKind.CommonMarket)).IsTrue();
    }

    [Test]
    public async Task OrgLeave_DropsMembership()
    {
        var world = ProceduralWorldGenerator.Generate(5);
        var stats = new WorldTelemetry();
        InstitutionSeeder.EnsureContinentOrgs(world, stats);
        var org = world.ActiveOrgs.First();
        var leaver = org.MemberIds.First();
        var before = org.MemberIds.Count;
        var ok = DiplomaticInstruments.LeaveOrg(world, stats, org, leaver);
        await Assert.That(ok).IsTrue();
        await Assert.That(org.MemberIds.Contains(leaver)).IsFalse();
        await Assert.That(org.MemberIds.Count).IsEqualTo(before - 1);
    }
}
