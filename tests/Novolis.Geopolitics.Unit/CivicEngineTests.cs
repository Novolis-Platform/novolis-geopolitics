using Novolis.Geopolitics.Core;
using Novolis.Geopolitics.Diplomacy;
using Novolis.Geopolitics.Scenarios;
using Novolis.Geopolitics.Simulation;
using Novolis.Geopolitics.Trade;

namespace Novolis.Geopolitics.Unit;

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
