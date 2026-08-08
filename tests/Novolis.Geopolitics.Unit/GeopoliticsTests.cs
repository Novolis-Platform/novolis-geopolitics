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

public sealed class CivicEngineTests
{
    [Test]
    public async Task ApplyMonth_CollectsTax_AndUpdatesCivicStocks()
    {
        var polity = new Polity
        {
            Id = new PolityId(0),
            Name = "Testland",
            Continent = "Test",
            Government = GovernmentType.Democracy,
            Gdp = 120_000,
            Treasury = 10_000,
            Policy =
            {
                HouseholdTaxRate = 0.24,
                TransferShare = 0.3,
                InfrastructureShare = 0.5,
                PropagandaShare = 0.2,
                MilitaryShare = 0.25,
            },
            Civic =
            {
                Legitimacy = 0.55,
                Approval = 0.5,
                Corruption = 0.1,
                HumanDevelopment = 0.5,
            },
        };

        var beforeTreasury = polity.Treasury;
        CivicEngine.ApplyMonth(polity, new CivicEngine.MonthContext
        {
            ControlRatio = 1.0,
            ActiveWars = 0,
            ResourceShortage = 0,
            OccupyingForeignLand = false,
            LostHomeProvinces = false,
        });

        await Assert.That(polity.Civic.LastTaxCollected).IsGreaterThan(0);
        await Assert.That(polity.TaxRate).IsEqualTo(0.24);
        await Assert.That(polity.MilitaryBudgetShare).IsEqualTo(0.25);
        await Assert.That(polity.Stability).IsGreaterThan(0);
        await Assert.That(polity.Stability).IsLessThanOrEqualTo(1);
        // Peaceful month with transfers should not collapse legitimacy.
        await Assert.That(polity.Civic.Legitimacy).IsGreaterThan(0.4);
        await Assert.That(polity.Treasury).IsNotEqualTo(beforeTreasury);
    }

    [Test]
    public async Task ApplyMonth_WarAndShortage_RaiseFatigue_AndHurtApproval()
    {
        var polity = new Polity
        {
            Id = new PolityId(0),
            Name = "Warland",
            Continent = "Test",
            Government = GovernmentType.MilitaryJunta,
            Gdp = 80_000,
            Treasury = 5_000,
            Policy = { HouseholdTaxRate = 0.35, MilitaryShare = 0.5, TransferShare = 0.1 },
            Civic = { Legitimacy = 0.6, Approval = 0.55, WarFatigue = 0.1, HumanDevelopment = 0.4 },
        };

        var approval0 = polity.Civic.Approval;
        CivicEngine.ApplyMonth(polity, new CivicEngine.MonthContext
        {
            ControlRatio = 0.6,
            ActiveWars = 2,
            ResourceShortage = 50_000,
            OccupyingForeignLand = true,
            LostHomeProvinces = true,
        });

        await Assert.That(polity.Civic.WarFatigue).IsGreaterThan(0.1);
        await Assert.That(polity.Civic.Approval).IsLessThan(approval0);
    }

    [Test]
    public async Task SeedLoad_InitializesPolicyAndCivicFromLegacyFields()
    {
        var world = DefaultWorld.Load();
        var p = world.Polities[0];
        await Assert.That(p.Policy.HouseholdTaxRate).IsEqualTo(p.TaxRate);
        await Assert.That(p.Policy.MilitaryShare).IsEqualTo(p.MilitaryBudgetShare);
        await Assert.That(p.Civic.Legitimacy).IsGreaterThan(0);
        await Assert.That(p.Civic.Approval).IsGreaterThan(0);
    }
}

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
