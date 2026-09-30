using Novolis.Geopolitics.Conflict;
using Novolis.Geopolitics.Core;
using Novolis.Geopolitics.Diplomacy;
using Novolis.Geopolitics.Simulation;
using Novolis.Geopolitics.Trade;

namespace Novolis.Geopolitics.Unit;

static class Tiny
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
