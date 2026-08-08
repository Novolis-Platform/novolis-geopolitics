using Novolis.Geopolitics.Core;
using Novolis.Geopolitics.Diplomacy;
using Novolis.Geopolitics.Scenarios;

namespace Novolis.Geopolitics.Unit;

public sealed class PublicApiCoverageTests
{
    [Test]
    public async Task GovernmentRules_CoversAllRegimeSwitches()
    {
        await Assert.That(GovernmentRules.MilitaryUpkeepFactor(GovernmentType.MilitaryJunta)).IsEqualTo(0.78);
        await Assert.That(GovernmentRules.MilitaryUpkeepFactor(GovernmentType.Autocracy)).IsEqualTo(0.92);
        await Assert.That(GovernmentRules.MilitaryUpkeepFactor(GovernmentType.Democracy)).IsEqualTo(1.05);
        await Assert.That(GovernmentRules.MilitaryUpkeepFactor(GovernmentType.Multiparty)).IsEqualTo(1.05);
        await Assert.That(GovernmentRules.MilitaryUpkeepFactor(GovernmentType.SingleParty)).IsEqualTo(1.0);

        await Assert.That(GovernmentRules.PropagandaEffectiveness(GovernmentType.Autocracy)).IsEqualTo(1.35);
        await Assert.That(GovernmentRules.PropagandaEffectiveness(GovernmentType.SingleParty)).IsEqualTo(1.35);
        await Assert.That(GovernmentRules.PropagandaEffectiveness(GovernmentType.MilitaryJunta)).IsEqualTo(1.15);
        await Assert.That(GovernmentRules.PropagandaEffectiveness(GovernmentType.Democracy)).IsEqualTo(0.75);
        await Assert.That(GovernmentRules.PropagandaEffectiveness(GovernmentType.Multiparty)).IsEqualTo(1.0);

        await Assert.That(GovernmentRules.TaxApprovalSensitivity(GovernmentType.Democracy)).IsEqualTo(1.4);
        await Assert.That(GovernmentRules.TaxApprovalSensitivity(GovernmentType.Multiparty)).IsEqualTo(1.4);
        await Assert.That(GovernmentRules.TaxApprovalSensitivity(GovernmentType.Autocracy)).IsEqualTo(0.7);
        await Assert.That(GovernmentRules.TaxApprovalSensitivity(GovernmentType.MilitaryJunta)).IsEqualTo(0.7);
        await Assert.That(GovernmentRules.TaxApprovalSensitivity(GovernmentType.SingleParty)).IsEqualTo(1.0);
    }

    [Test]
    public async Task ResourceVector_AddScaleCloneAndNullFromArray()
    {
        var a = ResourceVector.FromArray([1, 2, 3, 4, 5, 6, 7, 8]);
        var b = ResourceVector.FromArray(null);
        await Assert.That(b.Sum).IsEqualTo(0);

        a.Add(ResourceVector.FromArray([1, 0, 0, 0, 0, 0, 0, 0]));
        a.Scale(2);
        var clone = a.Clone();
        var arr = a.ToArray();

        await Assert.That(arr[0]).IsEqualTo(4);
        await Assert.That(clone[ResourceKind.Food]).IsEqualTo(arr[0]);
        await Assert.That(arr.Length).IsEqualTo(ResourceKinds.Count);
    }

    [Test]
    public async Task Treaty_AllParticipants_AndDirectedHelpers()
    {
        await Assert.That(Treaty.IsDirected(TreatyKind.Peace)).IsTrue();
        await Assert.That(Treaty.IsDirected(TreatyKind.Alliance)).IsFalse();
        await Assert.That(DiplomaticInstruments.IsMultilateral(TreatyKind.Alliance)).IsTrue();
        await Assert.That(DiplomaticInstruments.IsMultilateral(TreatyKind.EconomicEmbargo)).IsFalse();

        var multi = new Treaty
        {
            Id = 1,
            Name = "M",
            Kind = TreatyKind.Alliance,
            Creator = new PolityId(0),
            Members = [new PolityId(0), new PolityId(1)],
        };
        var directed = new Treaty
        {
            Id = 2,
            Name = "E",
            Kind = TreatyKind.EconomicEmbargo,
            Creator = new PolityId(0),
            SideA = [new PolityId(0)],
            SideB = [new PolityId(1)],
        };

        await Assert.That(multi.AllParticipants().Count()).IsEqualTo(2);
        await Assert.That(directed.AllParticipants().Count()).IsEqualTo(2);
        await Assert.That(directed.Contains(new PolityId(1))).IsTrue();
        await Assert.That(directed.AreOpposed(new PolityId(0), new PolityId(1))).IsTrue();
        await Assert.That(multi.SharesMembership(new PolityId(0), new PolityId(1))).IsTrue();
    }

    [Test]
    public async Task WorldState_CalendarTravelAccessAndEventTrim()
    {
        var world = ProceduralWorldGenerator.Generate(21);
        world.Day = 400;
        await Assert.That(world.Year).IsEqualTo(1);
        await Assert.That(world.DayOfYear).IsEqualTo(40);
        await Assert.That(world.Month).IsEqualTo(40 / WorldState.DaysPerMonth);
        await Assert.That(world.TotalPower()).IsGreaterThan(0);
        await Assert.That(world.ActiveTreaties(null).Count()).IsGreaterThanOrEqualTo(0);

        var a = new PolityId(0);
        await Assert.That(world.HasMilitaryAccess(a, a)).IsTrue();

        for (var i = 0; i < 50_002; i++)
            world.AddEvent(GeoEventKind.TreatyFormed, $"e{i}");
        await Assert.That(world.Events.Count).IsEqualTo(50_000);
    }

    [Test]
    public async Task DiplomaticRules_MinRelationAndRefusalBranches()
    {
        await Assert.That(DiplomaticRules.MinRelationFor(TreatyKind.Peace)).IsEqualTo(-100);
        await Assert.That(DiplomaticRules.MinRelationFor(TreatyKind.EconomicEmbargo)).IsEqualTo(-100);
        await Assert.That(DiplomaticRules.MinRelationFor(TreatyKind.WeaponTradeEmbargo)).IsEqualTo(-100);
        await Assert.That(DiplomaticRules.MinRelationFor(TreatyKind.Alliance)).IsEqualTo(50);
        await Assert.That(DiplomaticRules.MinRelationFor((TreatyKind)999)).IsEqualTo(20);

        var world = ProceduralWorldGenerator.Generate(23);
        var a = new PolityId(0);
        var b = new PolityId(1);

        await Assert.That(DiplomaticRules.EvaluateBilateral(world, TreatyKind.Alliance, a, a))
            .IsEqualTo(TreatyRefusal.WrongProfile);
        await Assert.That(DiplomaticRules.EvaluateBilateral(world, TreatyKind.EconomicEmbargo, a, b))
            .IsEqualTo(TreatyRefusal.Accepted);
        await Assert.That(DiplomaticRules.EvaluateBilateral(world, TreatyKind.WeaponTradeEmbargo, a, b))
            .IsEqualTo(TreatyRefusal.Accepted);
        await Assert.That(DiplomaticRules.EvaluateBilateral(world, TreatyKind.Peace, a, b))
            .IsEqualTo(TreatyRefusal.WrongProfile);

        var stats = new WorldTelemetry();
        DiplomaticInstruments.DeclareWar(world, stats, a, b);
        await Assert.That(DiplomaticRules.EvaluateBilateral(world, TreatyKind.Peace, a, b))
            .IsEqualTo(TreatyRefusal.Accepted);
        await Assert.That(DiplomaticRules.EvaluateBilateral(world, TreatyKind.Alliance, a, b))
            .IsEqualTo(TreatyRefusal.AtWar);

        // Fresh world for remaining bilateral gates
        world = ProceduralWorldGenerator.Generate(29);
        a = new PolityId(0);
        b = new PolityId(1);
        world.Relations.Set(a, b, 80);
        world.Polity(a).Stability = 0.9;
        world.Polity(b).Stability = 0.1;
        world.Polity(b).Civic.Legitimacy = 0.1;
        await Assert.That(DiplomaticRules.EvaluateBilateral(world, TreatyKind.Alliance, a, b))
            .IsEqualTo(TreatyRefusal.Unstable);

        world = ProceduralWorldGenerator.Generate(31);
        a = new PolityId(0);
        b = new PolityId(1);
        var c = new PolityId(2);
        world.Relations.Set(a, b, 80);
        world.Relations.Set(a, c, 80);
        world.Relations.Set(b, c, -50);
        world.Polity(a).Stability = 0.9;
        world.Polity(b).Stability = 0.9;
        world.Polity(c).Stability = 0.9;
        DiplomaticInstruments.SignTreaty(world, stats, TreatyKind.Alliance, a, c, 1000);
        await Assert.That(DiplomaticRules.EvaluateBilateral(world, TreatyKind.Alliance, a, b))
            .IsEqualTo(TreatyRefusal.HostileWithFriend);

        world = ProceduralWorldGenerator.Generate(37);
        a = new PolityId(0);
        b = new PolityId(1);
        world.Relations.Set(a, b, 80);
        world.Polity(a).Gdp = 100_000;
        world.Polity(a).Treasury = 100; // < 2% GDP
        world.Polity(a).Stability = 0.9;
        world.Polity(b).Stability = 0.9;
        await Assert.That(DiplomaticRules.EvaluateBilateral(world, TreatyKind.EconomicAid, a, b))
            .IsEqualTo(TreatyRefusal.CannotAfford);

        world = ProceduralWorldGenerator.Generate(41);
        a = new PolityId(0);
        b = new PolityId(1);
        world.Relations.Set(a, b, 80);
        world.Polity(a).Stability = 0.9;
        world.Polity(b).Stability = 0.9;
        // Power imbalance via military totals feeding PowerScore
        world.Polity(a).Military.Land = 1_000_000;
        world.Polity(b).Military.Land = 1;
        await Assert.That(DiplomaticRules.EvaluateBilateral(world, TreatyKind.Alliance, a, b))
            .IsEqualTo(TreatyRefusal.PowerImbalance);
    }

    [Test]
    public async Task DiplomaticRules_OrgJoinRefusalBranches()
    {
        var world = ProceduralWorldGenerator.Generate(43);
        var stats = new WorldTelemetry();
        var members = new[] { new PolityId(0), new PolityId(1), new PolityId(2) };
        foreach (var x in members)
        {
            foreach (var y in members)
            {
                if (x.Value < y.Value)
                    world.Relations.Set(x, y, 60);
            }

            world.Polity(x).Stability = 0.9;
            world.Polity(x).Civic.Legitimacy = 0.9;
        }

        var org = DiplomaticInstruments.CreateOrg(
            world, stats, "Bloc", "Test", members, SupranationalKind.DefenceAlliance);

        await Assert.That(DiplomaticRules.EvaluateOrgJoin(world, org, new PolityId(0)))
            .IsEqualTo(TreatyRefusal.AlreadyBound);

        org.Active = false;
        await Assert.That(DiplomaticRules.EvaluateOrgJoin(world, org, new PolityId(3)))
            .IsEqualTo(TreatyRefusal.AlreadyBound);
        org.Active = true;

        var empty = DiplomaticInstruments.CreateOrg(
            world, stats, "Empty", null, [], SupranationalKind.Forum);
        empty.MemberIds.Clear();
        await Assert.That(DiplomaticRules.EvaluateOrgJoin(world, empty, new PolityId(3)))
            .IsEqualTo(TreatyRefusal.WrongProfile);

        DiplomaticInstruments.DeclareWar(world, stats, new PolityId(0), new PolityId(3));
        await Assert.That(DiplomaticRules.EvaluateOrgJoin(world, org, new PolityId(3)))
            .IsEqualTo(TreatyRefusal.AtWar);

        world = ProceduralWorldGenerator.Generate(47);
        stats = new WorldTelemetry();
        members = [new PolityId(0), new PolityId(1)];
        world.Relations.Set(members[0], members[1], 60);
        world.Polity(members[0]).Stability = 0.9;
        world.Polity(members[1]).Stability = 0.9;
        org = DiplomaticInstruments.CreateOrg(
            world, stats, "Union", "Test", members, SupranationalKind.PoliticalUnion);
        var poor = new PolityId(2);
        world.Relations.Set(poor, members[0], 60);
        world.Relations.Set(poor, members[1], 60);
        world.Polity(poor).Stability = 0.1;
        world.Polity(poor).Civic.Legitimacy = 0.1;
        await Assert.That(DiplomaticRules.EvaluateOrgJoin(world, org, poor))
            .IsEqualTo(TreatyRefusal.Unstable);

        world.Polity(poor).Stability = 0.9;
        world.Polity(poor).Civic.Legitimacy = 0.9;
        world.Polity(poor).Gdp = 1;
        foreach (var m in members)
            world.Polity(m).Gdp = 1_000_000;
        await Assert.That(DiplomaticRules.EvaluateOrgJoin(world, org, poor))
            .IsEqualTo(TreatyRefusal.PowerImbalance);
    }

    [Test]
    public async Task DiplomaticInstruments_RejectsAndDirectedLifecycle()
    {
        var world = ProceduralWorldGenerator.Generate(53);
        var stats = new WorldTelemetry();
        var a = new PolityId(0);
        var b = new PolityId(1);

        await Assert.That(DiplomaticInstruments.SignTreaty(world, stats, TreatyKind.Alliance, a, a, 100))
            .IsNull();

        DiplomaticInstruments.DeclareWar(world, stats, a, b);
        await Assert.That(DiplomaticInstruments.SignTreaty(world, stats, TreatyKind.Alliance, a, b, 100))
            .IsNull();
        await Assert.That(DiplomaticInstruments.SignTreaty(world, stats, TreatyKind.EconomicEmbargo, a, b, 100))
            .IsNotNull();
        await Assert.That(DiplomaticInstruments.SignTreaty(world, stats, TreatyKind.EconomicEmbargo, a, b, 100))
            .IsNull(); // already bound

        await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            DiplomaticInstruments.CreateMultilateral(
                world, stats, TreatyKind.EconomicEmbargo, a, [a, b], 100);
            return Task.CompletedTask;
        });
        await Assert.That(DiplomaticInstruments.CreateMultilateral(
            world, stats, TreatyKind.Alliance, a, [a], 100)).IsNull();
        await Assert.That(DiplomaticInstruments.CreateDirected(
            world, stats, TreatyKind.Alliance, a, b, 100)).IsNull();
        await Assert.That(DiplomaticInstruments.CreateDirected(
            world, stats, TreatyKind.Peace, a, a, 100)).IsNull();

        var peace = DiplomaticInstruments.SignTreaty(world, stats, TreatyKind.Peace, a, b, 360);
        await Assert.That(peace).IsNotNull();
        await Assert.That(DiplomaticInstruments.LeaveTreaty(world, stats, peace!, a)).IsTrue();
        await Assert.That(peace!.Active).IsFalse();
        await Assert.That(DiplomaticInstruments.LeaveTreaty(world, stats, peace, a)).IsFalse();

        world = ProceduralWorldGenerator.Generate(59);
        stats = new WorldTelemetry();
        a = new PolityId(0);
        b = new PolityId(1);
        var c = new PolityId(2);
        world.Relations.Set(a, b, 70);
        world.Relations.Set(a, c, 70);
        world.Relations.Set(b, c, 70);
        world.Polity(a).Stability = 0.9;
        world.Polity(b).Stability = 0.9;
        world.Polity(c).Stability = 0.9;
        var alliance = DiplomaticInstruments.SignTreaty(world, stats, TreatyKind.Alliance, a, b, 500)!;
        await Assert.That(DiplomaticInstruments.JoinTreaty(world, stats, alliance, c)).IsTrue();
        DiplomaticInstruments.DeclareWar(world, stats, c, new PolityId(3));
        // joiner at war with member → false when trying to join another treaty with war member
        var market = DiplomaticInstruments.SignTreaty(world, stats, TreatyKind.CulturalExchanges, a, b, 500)!;
        // c not at war with a/b
        await Assert.That(DiplomaticInstruments.JoinTreaty(world, stats, market, c)).IsTrue();
        await Assert.That(DiplomaticInstruments.JoinTreaty(world, stats, market, c)).IsFalse(); // already member
        await Assert.That(DiplomaticInstruments.LeaveTreaty(world, stats, market, c)).IsTrue();
        await Assert.That(DiplomaticInstruments.LeaveTreaty(world, stats, alliance, a)).IsTrue();
    }

    [Test]
    public async Task DiplomaticInstruments_JoinOrg_AndDeclareWarGuards()
    {
        var world = ProceduralWorldGenerator.Generate(61);
        var stats = new WorldTelemetry();
        var members = new[] { new PolityId(0), new PolityId(1) };
        world.Relations.Set(members[0], members[1], 70);
        world.Polity(members[0]).Stability = 0.9;
        world.Polity(members[1]).Stability = 0.9;
        var org = DiplomaticInstruments.CreateOrg(
            world, stats, "Forum", "Test", members, SupranationalKind.Forum);

        var joiner = new PolityId(2);
        world.Relations.Set(joiner, members[0], 70);
        world.Relations.Set(joiner, members[1], 70);
        world.Polity(joiner).Stability = 0.9;
        world.Polity(joiner).Civic.Legitimacy = 0.9;
        await Assert.That(DiplomaticInstruments.JoinOrg(world, stats, org, joiner)).IsTrue();
        await Assert.That(DiplomaticInstruments.JoinOrg(world, stats, org, joiner)).IsFalse();
        await Assert.That(DiplomaticInstruments.LeaveOrg(world, stats, org, joiner)).IsTrue();
        await Assert.That(DiplomaticInstruments.LeaveOrg(world, stats, org, joiner)).IsFalse();

        var a = new PolityId(0);
        var b = new PolityId(1);
        await Assert.That(DiplomaticInstruments.DeclareWar(world, stats, a, a)).IsNull();
        var war = DiplomaticInstruments.DeclareWar(world, stats, a, b);
        await Assert.That(war).IsNotNull();
        await Assert.That(DiplomaticInstruments.DeclareWar(world, stats, a, b)).IsNull();
    }

    [Test]
    public async Task MilitaryForce_ScaleAndAdd()
    {
        var force = new MilitaryForce { Land = 10, Air = 5, Naval = 2 };
        force.Scale(2);
        force.Add(new MilitaryForce { Land = 1, Air = 1, Naval = 1 });
        var clone = force.Clone();
        await Assert.That(force.Total).IsEqualTo(clone.Total);
        await Assert.That(force.Land).IsEqualTo(21);
    }
}
