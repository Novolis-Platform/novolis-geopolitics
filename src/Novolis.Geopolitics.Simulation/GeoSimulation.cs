using Novolis.Geopolitics.Core;

namespace Novolis.Geopolitics.Simulation;

public sealed class GeoSimulationStats
{
    public int DaysAdvanced { get; set; }
    public int WarsStarted { get; set; }
    public int WarsEnded { get; set; }
    public int ProvincesCaptured { get; set; }
    public int TreatiesSigned { get; set; }
    public int TreatyJoins { get; set; }
    public int TreatyLeaves { get; set; }
    public int OrgsCreated { get; set; }
    public int OrgJoins { get; set; }
    public int OrgLeaves { get; set; }
    public int BudgetCrises { get; set; }
    public int TechAdvances { get; set; }
    public double CommonMarketVolume { get; set; }
    public double WorldMarketVolume { get; set; }
    public double EconomicPartnershipGdpBoost { get; set; }
    public double EconomicAidTransferred { get; set; }
    public int ResourceShortageEvents { get; set; }
    public double MeanLegitimacy { get; set; }
    public double MeanApproval { get; set; }
}

/// <summary>Advances a <see cref="WorldState"/> by calendar days with month-boundary AI/budget.</summary>
public sealed class GeoSimulation
{
    private readonly Random _rng;
    private readonly CombatResolver _combat;
    private readonly PolityAi _ai;

    public GeoSimulation(WorldState world, int? rngSeed = null)
    {
        World = world;
        _rng = new Random(rngSeed ?? world.Seed);
        _combat = new CombatResolver(_rng);
        _ai = new PolityAi(_rng);
        Stats = new GeoSimulationStats();
        WorldBootstrap.EnsureContinentOrgs(world, Stats);
    }

    public WorldState World { get; }
    public GeoSimulationStats Stats { get; }

    public void Advance(int days)
    {
        if (days < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(days));
        }

        for (var i = 0; i < days; i++)
        {
            AdvanceOneDay();
        }

        Stats.DaysAdvanced += days;
    }

    public void AdvanceYears(int years) => Advance(years * WorldState.DaysPerYear);

    private void AdvanceOneDay()
    {
        World.Day++;

        ExpireTreaties();
        DriftRelations();
        ResolveWarFronts();

        if (World.Day % WorldState.DaysPerMonth == 0)
        {
            // Trade first so resource balances feed civic approval/growth this period.
            TradeResolver.RunMonth(World, Stats);
            CivicPipeline.RunMonth(World, Stats);
            TreatyEffects.RunMonth(World, Stats);
            _ai.RunMonth(World, Stats);
        }
    }

    private void ExpireTreaties()
    {
        foreach (var t in World.Treaties)
        {
            if (!t.Active)
            {
                continue;
            }

            if (t.ExpiresDay >= 0 && World.Day >= t.ExpiresDay)
            {
                t.Active = false;
                if (t.Kind == TreatyKind.Alliance)
                {
                    World.AddEvent(GeoEventKind.AllianceBroken, $"Alliance expired: {t.Name}");
                }
            }
        }
    }

    private void DriftRelations()
    {
        foreach (var war in World.ActiveWars)
        {
            World.Relations.Adjust(war.Attacker, war.Defender, -0.15);
        }

        if (World.Day % 7 != 0)
        {
            return;
        }

        var n = World.Polities.Count;
        var samples = Math.Min(400, n * 2);
        for (var s = 0; s < samples; s++)
        {
            var a = new PolityId(_rng.Next(n));
            var b = new PolityId(_rng.Next(n));
            if (a == b || World.AreAtWar(a, b))
            {
                continue;
            }

            var score = World.Relations.Get(a, b);
            var pull = -score * 0.01;
            var noise = (_rng.NextDouble() - 0.5) * 0.8;
            if (World.AreAllied(a, b))
            {
                pull += 0.2;
            }

            if (World.HaveTreaty(a, b, TreatyKind.EconomicPartnership)
                || World.HaveTreaty(a, b, TreatyKind.CommonMarket))
            {
                pull += 0.12;
            }

            if (World.AreEmbargoed(a, b))
            {
                pull -= 0.15;
            }

            World.Relations.Adjust(a, b, pull + noise);
        }
    }

    private void ResolveWarFronts()
    {
        var active = World.Wars.Where(w => w.Active).ToList();
        foreach (var war in active)
        {
            var captured = _combat.TryResolveFront(World, war);
            if (captured)
            {
                Stats.ProvincesCaptured++;
            }

            var attackerPower = World.Polity(war.Attacker).Military.Total;
            var defenderPower = World.Polity(war.Defender).Military.Total;
            var duration = World.Day - war.StartedDay;
            if (duration > 120 && (attackerPower < 30 || defenderPower < 30 ||
                                   war.ProvincesTakenByAttacker + war.ProvincesTakenByDefender > 8))
            {
                EndWar(war, seekPeace: true);
            }
        }
    }

    internal void EndWar(War war, bool seekPeace)
    {
        if (!war.Active)
        {
            return;
        }

        war.Active = false;
        war.EndedDay = World.Day;
        Stats.WarsEnded++;

        Diplomacy.SignTreaty(World, Stats, TreatyKind.Peace, war.Attacker, war.Defender, durationDays: 360);
        World.Relations.Adjust(war.Attacker, war.Defender, seekPeace ? 8 : -5);
        World.AddEvent(
            GeoEventKind.PeaceSigned,
            $"Peace: {World.Polity(war.Attacker).Name} / {World.Polity(war.Defender).Name}",
            war.Attacker,
            war.Defender);
    }

}
