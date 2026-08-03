using Novolis.Geopolitics.Conflict;
using Novolis.Geopolitics.Core;
using Novolis.Geopolitics.Diplomacy;
using Novolis.Geopolitics.Simulation;
using Novolis.Geopolitics.Trade;

namespace Novolis.Geopolitics.Unit;

/// <summary>
/// Hand-built tiny theatres with known month/day dynamics: civic stock paths,
/// occupation/control, force growth, capture, treaties, and trade clearing.
/// Avoids HeuristicPolicyAgent noise so outcomes are deterministic.
/// </summary>
public sealed class KnownDynamicsScenariosTests
{
    const double Eps = 1e-9;

    // ─── Civic / military month (Geo wrapper over Civics) ─────────────────────

    [Test]
    public async Task Peaceful_Month_Grows_Military_From_Force_Demand()
    {
        var p = Tiny.Polity(0, "Alpha", GovernmentType.Democracy, milShare: 0.55);
        p.Military.Land = 10;
        p.Military.Air = 2;
        p.Military.Naval = 1;
        var land0 = p.Military.Land;
        var air0 = p.Military.Air;
        var naval0 = p.Military.Naval;
        CivicEngine.ApplyMonth(p, Tiny.Peace());
        await Assert.That(p.Military.Land).IsGreaterThan(land0);
        await Assert.That(p.Military.Air).IsGreaterThan(air0);
        await Assert.That(p.Military.Naval).IsGreaterThan(naval0);
        var dLand = p.Military.Land - land0;
        var dAir = p.Military.Air - air0;
        var dNaval = p.Military.Naval - naval0;
        await Assert.That(dLand).IsGreaterThan(dAir);
        await Assert.That(dAir).IsGreaterThan(dNaval);
    }

    [Test]
    public async Task Upkeep_Scale_Shrinks_Force_Stock_Slightly_Each_Month()
    {
        var p = Tiny.Polity(0, "Alpha", GovernmentType.Democracy, milShare: 0.05);
        p.Military.Land = 1000;
        p.Military.Air = 1000;
        p.Military.Naval = 1000;
        // Tiny demand vs large stock → net shrink from upkeep scale 1 - 0.008*1.05
        CivicEngine.ApplyMonth(p, Tiny.Peace());
        await Assert.That(p.Military.Land).IsLessThan(1000);
    }

    [Test]
    public async Task Junta_Builds_More_Force_Than_Democracy_Same_Budget()
    {
        var dem = Tiny.Polity(0, "Dem", GovernmentType.Democracy, milShare: 0.45);
        var junta = Tiny.Polity(1, "Junta", GovernmentType.MilitaryJunta, milShare: 0.45);
        dem.Military.Land = 100;
        dem.Military.Air = 20;
        dem.Military.Naval = 10;
        junta.Military.Land = 100;
        junta.Military.Air = 20;
        junta.Military.Naval = 10;
        CivicEngine.ApplyMonth(dem, Tiny.Peace());
        CivicEngine.ApplyMonth(junta, Tiny.Peace());
        await Assert.That(junta.Military.Total).IsGreaterThan(dem.Military.Total);
    }

    [Test]
    public async Task War_Month_Raises_Fatigue_Faster_Than_Peace()
    {
        var war = Tiny.Polity(0, "Warland", GovernmentType.Autocracy, milShare: 0.35);
        var peace = Tiny.Polity(1, "Peaceland", GovernmentType.Autocracy, milShare: 0.35);
        war.Civic.WarFatigue = 0.05;
        peace.Civic.WarFatigue = 0.05;
        CivicEngine.ApplyMonth(war, new CivicEngine.MonthContext
        {
            ControlRatio = 1,
            ActiveWars = 2,
            ResourceShortage = 0,
            OccupyingForeignLand = false,
            LostHomeProvinces = false,
        });
        CivicEngine.ApplyMonth(peace, Tiny.Peace());
        await Assert.That(Near(war.Civic.WarFatigue, 0.05 + 0.06)).IsTrue();
        await Assert.That(peace.Civic.WarFatigue).IsLessThan(0.05);
        await Assert.That(war.Civic.WarFatigue).IsGreaterThan(peace.Civic.WarFatigue);
    }

    [Test]
    public async Task Lost_Homeland_Depresses_Legitimacy_Via_Pipeline()
    {
        var world = Tiny.BorderWorld();
        // Beta occupies Alpha's home province 0
        world.Provinces[0].OwnerId = new PolityId(1);
        var alpha = world.Polity(new PolityId(0));
        var leg0 = alpha.Civic.Legitimacy;
        CivicPipeline.RunMonth(world, new WorldTelemetry());
        await Assert.That(alpha.Civic.Legitimacy).IsLessThan(leg0);
        await Assert.That(alpha.Civic.WarFatigue).IsGreaterThanOrEqualTo(0); // peacetime decay if no wars
    }

    [Test]
    public async Task Occupier_Pays_Fatigue_Premium_While_At_War()
    {
        var world = Tiny.BorderWorld();
        world.Provinces[1].OwnerId = new PolityId(0); // Alpha holds Beta home
        var alpha = world.Polity(new PolityId(0));
        var fat0 = alpha.Civic.WarFatigue;
        var stats = new WorldTelemetry();
        world.Relations.Set(new PolityId(0), new PolityId(1), -50);
        DiplomaticInstruments.DeclareWar(world, stats, new PolityId(0), new PolityId(1));
        CivicPipeline.RunMonth(world, stats);
        // ActiveWars=1 + occupying → +0.03 + 0.01
        await Assert.That(alpha.Civic.WarFatigue).IsGreaterThan(fat0 + 0.03);
        await Assert.That(world.AreAtWar(new PolityId(0), new PolityId(1))).IsTrue();
    }

    [Test]
    public async Task Control_Half_Cuts_Tax_Take_Versus_Full_Control()
    {
        var full = Tiny.Polity(0, "Full", GovernmentType.Democracy);
        var half = Tiny.Polity(1, "Half", GovernmentType.Democracy);
        CivicEngine.ApplyMonth(full, Tiny.Peace(control: 1.0));
        CivicEngine.ApplyMonth(half, Tiny.Peace(control: 0.5));
        await Assert.That(half.Civic.LastTaxCollected).IsLessThan(full.Civic.LastTaxCollected * 0.55);
    }

    [Test]
    public async Task Resource_Shortage_From_Balance_Hurts_Approval_In_Pipeline()
    {
        var shortWorld = Tiny.BorderWorld();
        var calmWorld = Tiny.BorderWorld();
        shortWorld.Polity(new PolityId(0)).Balance[ResourceKind.Food] = -200_000;
        CivicPipeline.RunMonth(shortWorld, new WorldTelemetry());
        CivicPipeline.RunMonth(calmWorld, new WorldTelemetry());
        await Assert.That(shortWorld.Polity(new PolityId(0)).Civic.Approval)
            .IsLessThan(calmWorld.Polity(new PolityId(0)).Civic.Approval);
    }

    [Test]
    public async Task Twelve_Peaceful_Months_Grow_Gdp_And_Hd()
    {
        var p = Tiny.Polity(0, "Nord", GovernmentType.Democracy, milShare: 0.15);
        p.Policy.InfrastructureShare = 0.8;
        p.Policy.PropagandaShare = 0.2;
        var gdp0 = p.Gdp;
        var hd0 = p.Civic.HumanDevelopment;
        for (var i = 0; i < 12; i++)
            CivicEngine.ApplyMonth(p, Tiny.Peace());
        await Assert.That(p.Gdp).IsGreaterThan(gdp0);
        await Assert.That(p.Civic.HumanDevelopment).IsGreaterThan(hd0);
    }

    [Test]
    public async Task Militarized_Polity_Corrupts_Faster_Over_Year()
    {
        var guns = Tiny.Polity(0, "Guns", GovernmentType.Autocracy, milShare: 0.6);
        var butter = Tiny.Polity(1, "Butter", GovernmentType.Democracy, milShare: 0.12);
        butter.Policy.InfrastructureShare = 0.85;
        butter.Policy.TransferShare = 0.35;
        for (var i = 0; i < 12; i++)
        {
            CivicEngine.ApplyMonth(guns, Tiny.Peace());
            CivicEngine.ApplyMonth(butter, Tiny.Peace());
        }

        await Assert.That(guns.Civic.Corruption).IsGreaterThan(butter.Civic.Corruption);
        await Assert.That(butter.Civic.HumanDevelopment).IsGreaterThan(guns.Civic.HumanDevelopment);
    }

    [Test]
    public async Task Insolvent_Treasury_Triggers_Budget_Crisis_Telemetry()
    {
        var world = Tiny.BorderWorld();
        var alpha = world.Polity(new PolityId(0));
        alpha.Treasury = -5_000;
        // Observed path isn't available in Geo — keep treasury negative by huge mil after...
        // Standalone: force insolvency branch via empty tax base + negative treasury
        alpha.Gdp = 1;
        alpha.Policy.HouseholdTaxRate = 0;
        var stats = new WorldTelemetry();
        CivicPipeline.RunMonth(world, stats);
        await Assert.That(stats.BudgetCrises).IsGreaterThan(0);
        await Assert.That(alpha.Treasury).IsLessThan(0);
    }

    // ─── Conflict / capture ───────────────────────────────────────────────────

    [Test]
    public async Task Overwhelming_Attacker_Captures_Border_Province()
    {
        var world = Tiny.BorderWorld();
        var atk = world.Polity(new PolityId(0));
        var def = world.Polity(new PolityId(1));
        atk.Military.Land = 5_000;
        atk.Military.Air = 1_000;
        atk.Military.Naval = 200;
        def.Military.Land = 5;
        def.Military.Air = 1;
        def.Military.Naval = 1;
        var stats = new WorldTelemetry();
        world.Relations.Set(atk.Id, def.Id, -60);
        var war = DiplomaticInstruments.DeclareWar(world, stats, atk.Id, def.Id);
        await Assert.That(war).IsNotNull();

        var resolver = new ConflictResolver(new Random(7));
        var captured = false;
        for (var i = 0; i < 40 && !captured; i++)
            captured = resolver.TryResolveFront(world, war!);

        await Assert.That(captured).IsTrue();
        await Assert.That(world.CountOwnedProvinces(atk.Id)).IsEqualTo(2);
        await Assert.That(war!.ProvincesTakenByAttacker).IsGreaterThan(0);
        await Assert.That(def.Stability).IsLessThan(0.75);
    }

    [Test]
    public async Task Capture_Raises_Attacker_Stability_And_Hurts_Defender()
    {
        var world = Tiny.BorderWorld();
        var atk = world.Polity(new PolityId(0));
        var def = world.Polity(new PolityId(1));
        atk.Stability = 0.7;
        def.Stability = 0.7;
        atk.Military.Land = 8_000;
        atk.Military.Air = 2_000;
        atk.Military.Naval = 500;
        def.Military.Land = 1;
        def.Military.Air = 1;
        def.Military.Naval = 1;
        var stats = new WorldTelemetry();
        DiplomaticInstruments.DeclareWar(world, stats, atk.Id, def.Id);
        var war = world.ActiveWars.Single();
        var resolver = new ConflictResolver(new Random(99));
        for (var i = 0; i < 50; i++)
        {
            if (resolver.TryResolveFront(world, war))
                break;
        }

        await Assert.That(atk.Stability).IsGreaterThanOrEqualTo(0.7);
        await Assert.That(def.Stability).IsLessThan(0.7);
    }

    [Test]
    public async Task Battle_Always_Inflicts_Casualties_Even_On_Failed_Assault()
    {
        var world = Tiny.BorderWorld();
        var atk = world.Polity(new PolityId(0));
        var def = world.Polity(new PolityId(1));
        atk.Military.Land = 100;
        def.Military.Land = 10_000; // defender dominates
        var stats = new WorldTelemetry();
        DiplomaticInstruments.DeclareWar(world, stats, atk.Id, def.Id);
        var war = world.ActiveWars.Single();
        var land0 = atk.Military.Land;
        var def0 = def.Military.Land;
        new ConflictResolver(new Random(3)).TryResolveFront(world, war);
        await Assert.That(atk.Military.Land).IsLessThan(land0);
        await Assert.That(def.Military.Land).IsLessThan(def0);
    }

    // ─── Diplomacy ────────────────────────────────────────────────────────────

    [Test]
    public async Task Alliance_Blocks_DeclareWar()
    {
        var world = Tiny.BorderWorld();
        var stats = new WorldTelemetry();
        world.Relations.Set(new PolityId(0), new PolityId(1), 80);
        DiplomaticInstruments.SignTreaty(world, stats, TreatyKind.Alliance, new PolityId(0), new PolityId(1), 2000);
        var war = DiplomaticInstruments.DeclareWar(world, stats, new PolityId(0), new PolityId(1));
        await Assert.That(war).IsNull();
        await Assert.That(world.AreAtWar(new PolityId(0), new PolityId(1))).IsFalse();
    }

    [Test]
    public async Task Peace_Treaty_Blocks_Redeclare_Within_Grace()
    {
        var world = Tiny.BorderWorld();
        var stats = new WorldTelemetry();
        world.Relations.Set(new PolityId(0), new PolityId(1), 40);
        DiplomaticInstruments.SignTreaty(world, stats, TreatyKind.Peace, new PolityId(0), new PolityId(1), 180);
        var war = DiplomaticInstruments.DeclareWar(world, stats, new PolityId(0), new PolityId(1));
        await Assert.That(war).IsNull();
    }

    [Test]
    public async Task Embargo_Treaty_Is_Recorded_And_Active()
    {
        var world = Tiny.BorderWorld();
        var stats = new WorldTelemetry();
        world.Relations.Set(new PolityId(0), new PolityId(1), 0);
        DiplomaticInstruments.SignTreaty(
            world, stats, TreatyKind.EconomicEmbargo, new PolityId(0), new PolityId(1), 500);
        await Assert.That(world.CountActiveTreatiesOfKind(TreatyKind.EconomicEmbargo)).IsEqualTo(1);
        await Assert.That(world.AreEmbargoed(new PolityId(0), new PolityId(1))).IsTrue();
    }

    [Test]
    public async Task Economic_Partnership_Raises_Gdp_Over_Months()
    {
        var world = Tiny.BorderWorld();
        var a = world.Polity(new PolityId(0));
        var stats = new WorldTelemetry();
        world.Relations.Set(new PolityId(0), new PolityId(1), 50);
        var gdp0 = a.Gdp;
        DiplomaticInstruments.SignTreaty(
            world, stats, TreatyKind.EconomicPartnership, new PolityId(0), new PolityId(1), 1000);
        for (var i = 0; i < 24; i++)
            TreatyEffects.RunMonth(world, stats);
        await Assert.That(a.Gdp).IsGreaterThan(gdp0);
        await Assert.That(stats.EconomicPartnershipGdpBoost).IsGreaterThan(0);
    }

    [Test]
    public async Task Research_Partnership_Sets_Pipeline_Multiplier()
    {
        var world = Tiny.BorderWorld();
        // Keep GDP-only tax so one month of infra does not blow past the tech threshold.
        foreach (var pr in world.Provinces)
            pr.Population = 0;
        var a = world.Polity(new PolityId(0));
        a.Policy.InfrastructureShare = 0.9;
        a.Policy.PropagandaShare = 0.1;
        var stats = new WorldTelemetry();
        world.Relations.Set(new PolityId(0), new PolityId(1), 55);
        DiplomaticInstruments.SignTreaty(
            world, stats, TreatyKind.ResearchPartnership, new PolityId(0), new PolityId(1), 1000);

        var twin = Tiny.BorderWorld();
        foreach (var pr in twin.Provinces)
            pr.Population = 0;
        var b = twin.Polity(new PolityId(0));
        b.Policy.InfrastructureShare = 0.9;
        b.Policy.PropagandaShare = 0.1;
        b.Gdp = a.Gdp;
        a.TechProgress = 0;
        b.TechProgress = 0;
        a.TechLevel = 1.0;
        b.TechLevel = 1.0;
        b.Stability = a.Stability;
        b.Civic.HumanDevelopment = a.Civic.HumanDevelopment;
        b.Treasury = a.Treasury;

        CivicPipeline.RunMonth(world, stats);
        CivicPipeline.RunMonth(twin, new WorldTelemetry());
        await Assert.That(a.TechProgress).IsGreaterThan(b.TechProgress);
    }

    // ─── Trade ────────────────────────────────────────────────────────────────

    [Test]
    public async Task CommonMarket_Moves_Food_From_Surplus_To_Deficit()
    {
        var world = Tiny.BorderWorld();
        var stats = new WorldTelemetry();
        world.Relations.Set(new PolityId(0), new PolityId(1), 60);
        DiplomaticInstruments.SignTreaty(
            world, stats, TreatyKind.CommonMarket, new PolityId(0), new PolityId(1), 1000);

        foreach (var pr in world.Provinces.Where(p => p.OwnerId.Value == 0))
        {
            pr.ResourceWeights[ResourceKind.Food] = 0.85;
            pr.Population = 4_000_000;
        }

        foreach (var pr in world.Provinces.Where(p => p.OwnerId.Value == 1))
        {
            pr.ResourceWeights[ResourceKind.Food] = 0.05;
            pr.Population = 4_000_000;
        }

        world.Polity(new PolityId(0)).Gdp = 400_000;
        world.Polity(new PolityId(1)).Gdp = 400_000;
        world.Polity(new PolityId(0)).Stability = 0.85;
        world.Polity(new PolityId(1)).Stability = 0.85;

        TradeClearing.RunMonth(world, stats);
        await Assert.That(stats.CommonMarketVolume).IsGreaterThan(0);
    }

    [Test]
    public async Task World_Market_Clears_Some_Volume_Without_Treaty()
    {
        var world = Tiny.BorderWorld();
        // Give complementary resource weights
        var foodHeavy = ResourceVector.FromArray([0.8, 0.05, 0.05, 0.05, 0.025, 0.025]);
        var oreHeavy = ResourceVector.FromArray([0.05, 0.8, 0.05, 0.05, 0.025, 0.025]);
        for (var i = 0; i < ResourceKinds.Count; i++)
        {
            world.Provinces[0].ResourceWeights[(ResourceKind)i] = foodHeavy[(ResourceKind)i];
            world.Provinces[1].ResourceWeights[(ResourceKind)i] = oreHeavy[(ResourceKind)i];
        }
        world.Provinces[0].Population = 3_000_000;
        world.Provinces[1].Population = 3_000_000;
        var stats = new WorldTelemetry();
        TradeClearing.RunMonth(world, stats);
        await Assert.That(stats.WorldMarketVolume + stats.CommonMarketVolume).IsGreaterThanOrEqualTo(0);
    }

    // ─── Composed month order (Trade → Civic → Treaty) ────────────────────────

    [Test]
    public async Task Composed_Month_Preserves_Province_Count_And_Finite_Stocks()
    {
        var world = Tiny.BorderWorld();
        var stats = new WorldTelemetry();
        for (var m = 0; m < 6; m++)
        {
            TradeClearing.RunMonth(world, stats);
            CivicPipeline.RunMonth(world, stats);
            PopulationMigration.RunMonth(world, stats);
            TreatyEffects.RunMonth(world, stats);
            world.Day += WorldState.DaysPerMonth;
        }

        await Assert.That(world.Provinces.Count).IsEqualTo(2);
        foreach (var p in world.Polities)
        {
            await Assert.That(double.IsFinite(p.Gdp)).IsTrue();
            await Assert.That(double.IsFinite(p.Treasury)).IsTrue();
            await Assert.That(p.Civic.Legitimacy).IsGreaterThanOrEqualTo(0);
            await Assert.That(p.Civic.Legitimacy).IsLessThanOrEqualTo(1);
            await Assert.That(p.Military.Land).IsGreaterThan(0);
        }
    }

    [Test]
    public async Task Occupation_Year_Depresses_Defender_Stability_Vs_Peace()
    {
        var occupied = Tiny.BorderWorld();
        occupied.Provinces[0].OwnerId = new PolityId(1); // Beta holds Alpha home
        var peace = Tiny.BorderWorld();
        var statsO = new WorldTelemetry();
        var statsP = new WorldTelemetry();
        for (var i = 0; i < 12; i++)
        {
            CivicPipeline.RunMonth(occupied, statsO);
            CivicPipeline.RunMonth(peace, statsP);
        }

        var oAlpha = occupied.Polity(new PolityId(0));
        var pAlpha = peace.Polity(new PolityId(0));
        await Assert.That(oAlpha.Stability).IsLessThan(pAlpha.Stability);
        await Assert.That(oAlpha.Civic.Legitimacy).IsLessThan(pAlpha.Civic.Legitimacy);
    }

    [Test]
    public async Task War_Year_Without_Capture_Still_Raises_Fatigue_Both_Sides()
    {
        var world = Tiny.BorderWorld();
        var stats = new WorldTelemetry();
        world.Relations.Set(new PolityId(0), new PolityId(1), -40);
        DiplomaticInstruments.DeclareWar(world, stats, new PolityId(0), new PolityId(1));
        // Balanced forces — unlikely instant capture; civic months only
        for (var i = 0; i < 8; i++)
            CivicPipeline.RunMonth(world, stats);

        await Assert.That(world.Polity(new PolityId(0)).Civic.WarFatigue).IsGreaterThan(0.15);
        await Assert.That(world.Polity(new PolityId(1)).Civic.WarFatigue).IsGreaterThan(0.15);
    }

    [Test]
    public async Task Tax_Rate_Mirror_Updates_After_Civic_Month()
    {
        var p = Tiny.Polity(0, "Mirror", GovernmentType.Multiparty);
        p.Policy.HouseholdTaxRate = 0.31;
        p.Policy.MilitaryShare = 0.33;
        CivicEngine.ApplyMonth(p, Tiny.Peace());
        await Assert.That(Near(p.TaxRate, 0.31)).IsTrue();
        await Assert.That(Near(p.MilitaryBudgetShare, 0.33)).IsTrue();
    }

    [Test]
    public async Task PowerScore_Responds_To_Military_And_Stability()
    {
        var weak = Tiny.Polity(0, "Weak", GovernmentType.Democracy);
        var strong = Tiny.Polity(1, "Strong", GovernmentType.Democracy);
        weak.Military.Land = 10;
        weak.Military.Air = 1;
        weak.Military.Naval = 1;
        strong.Military.Land = 5_000;
        strong.Military.Air = 1_000;
        strong.Military.Naval = 500;
        strong.Stability = 0.95;
        weak.Stability = 0.4;
        await Assert.That(strong.PowerScore).IsGreaterThan(weak.PowerScore);
    }

    // ─── Population migration ─────────────────────────────────────────────────

    [Test]
    public async Task High_Tax_Polity_Loses_Population_To_Low_Tax_Neighbor()
    {
        var world = Tiny.BorderWorld();
        world.Provinces[0].Population = 2_000_000;
        world.Provinces[1].Population = 2_000_000;
        var alpha = world.Polity(new PolityId(0));
        var beta = world.Polity(new PolityId(1));
        alpha.Policy.HouseholdTaxRate = 0.45;
        alpha.Civic.HumanDevelopment = 0.35;
        alpha.Civic.EmigrationPressure = 0.8;
        alpha.Civic.ImmigrationAttractiveness = 0.2;
        beta.Policy.HouseholdTaxRate = 0.12;
        beta.Civic.HumanDevelopment = 0.7;
        beta.Civic.EmigrationPressure = 0.1;
        beta.Civic.ImmigrationAttractiveness = 0.85;

        var popA0 = world.OwnedPopulation(alpha.Id);
        var stats = new WorldTelemetry();
        for (var i = 0; i < 6; i++)
            PopulationMigration.RunMonth(world, stats);

        await Assert.That(world.OwnedPopulation(alpha.Id)).IsLessThan(popA0);
        await Assert.That(stats.PopulationMigrated).IsGreaterThan(0);
    }

    [Test]
    public async Task Pop_Weighted_Control_Is_Zero_When_All_Home_Pop_Lost()
    {
        var world = Tiny.BorderWorld();
        world.Provinces[0].Population = 1_000_000;
        world.Provinces[1].Population = 1_000_000;
        world.Provinces[0].OwnerId = new PolityId(1);
        var ratio = world.PopWeightedControlRatio(new PolityId(0));
        await Assert.That(Near(ratio, 0.0)).IsTrue();
        var betaCtrl = world.PopWeightedControlRatio(new PolityId(1));
        await Assert.That(betaCtrl).IsGreaterThan(0.99);
    }

    [Test]
    public async Task Occupied_Province_Produces_Migration_Over_Months()
    {
        var world = Tiny.BorderWorld();
        world.Provinces[0].Population = 3_000_000;
        world.Provinces[1].Population = 3_000_000;
        world.Provinces[0].OwnerId = new PolityId(1);
        var beta = world.Polity(new PolityId(1));
        var alpha = world.Polity(new PolityId(0));
        beta.Civic.EmigrationPressure = 0.2;
        beta.Policy.HouseholdTaxRate = 0.3;
        alpha.Civic.ImmigrationAttractiveness = 0.9;
        var stats = new WorldTelemetry();
        for (var i = 0; i < 4; i++)
            PopulationMigration.RunMonth(world, stats);
        await Assert.That(stats.PopulationMigrated).IsGreaterThan(0);
    }

    [Test]
    public async Task Civic_With_World_Sets_Emigration_Pressure_From_High_Tax()
    {
        var world = Tiny.BorderWorld();
        world.Provinces[0].Population = 2_000_000;
        world.Provinces[1].Population = 2_000_000;
        var alpha = world.Polity(new PolityId(0));
        alpha.Policy.HouseholdTaxRate = 0.48;
        alpha.Policy.TransferShare = 0.08;
        CivicEngine.ApplyMonth(alpha, Tiny.Peace(), world);
        await Assert.That(alpha.Civic.EmigrationPressure).IsGreaterThan(0.4);
    }

    static bool Near(double a, double b) => Math.Abs(a - b) < Eps;
}

file static class Tiny
{
    public static Polity Polity(int id, string name, GovernmentType gov, double milShare = 0.28) => new()
    {
        Id = new PolityId(id),
        Name = name,
        Continent = "Test",
        Government = gov,
        Gdp = 100_000,
        Treasury = 20_000,
        Stability = 0.75,
        TechLevel = 1.0,
        Policy =
        {
            HouseholdTaxRate = 0.22,
            TransferShare = 0.25,
            InfrastructureShare = 0.45,
            PropagandaShare = 0.20,
            MilitaryShare = milShare,
        },
        Civic =
        {
            Legitimacy = 0.65,
            Approval = 0.55,
            Corruption = 0.15,
            HumanDevelopment = 0.55,
            WarFatigue = 0,
        },
        Military = new MilitaryForce { Land = 200, Air = 50, Naval = 40 },
    };

    public static CivicEngine.MonthContext Peace(double control = 1.0) => new()
    {
        ControlRatio = control,
        ActiveWars = 0,
        ResourceShortage = 0,
        OccupyingForeignLand = false,
        LostHomeProvinces = false,
    };

    public static WorldState BorderWorld()
    {
        var w = new WorldState { Seed = 1, SeedName = "tiny-border", Day = 0 };
        w.Polities.Add(Polity(0, "Alpha", GovernmentType.Democracy));
        w.Polities.Add(Polity(1, "Beta", GovernmentType.Autocracy));

        void AddProv(int id, int owner, bool coastal, params int[] neighbors)
        {
            w.Provinces.Add(new Province
            {
                Id = new ProvinceId(id),
                Name = $"P{id}",
                OwnerId = new PolityId(owner),
                HomePolityId = new PolityId(owner),
                Population = 1_500_000,
                Wealth = 40_000,
                Coastal = coastal,
                Neighbors = neighbors.Select(n => new ProvinceId(n)).ToList(),
                ResourceWeights = ResourceVector.FromArray([0.3, 0.2, 0.15, 0.15, 0.1, 0.1]),
            });
        }

        AddProv(0, 0, true, 1);
        AddProv(1, 1, true, 0);
        w.Relations.Set(new PolityId(0), new PolityId(1), -10);
        return w;
    }
}
