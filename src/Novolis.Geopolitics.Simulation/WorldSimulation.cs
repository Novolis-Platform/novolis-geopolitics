using Novolis.Geopolitics.Conflict;
using Novolis.Geopolitics.Core;
using Novolis.Geopolitics.Diplomacy;
using Novolis.Geopolitics.PolicyAgents;
using Novolis.Geopolitics.Scenarios;
using Novolis.Geopolitics.Trade;

namespace Novolis.Geopolitics.Simulation;

/// <summary>Advances a <see cref="WorldState"/> by calendar days with month-boundary policy/budget.</summary>
public sealed class WorldSimulation
{
    private readonly Random _rng;
    private readonly ConflictResolver _combat;
    private readonly HeuristicPolicyAgent _policyAgent;

    public WorldSimulation(WorldState world, int? rngSeed = null)
    {
        World = world;
        _rng = new Random(rngSeed ?? world.Seed);
        _combat = new ConflictResolver(_rng);
        _policyAgent = new HeuristicPolicyAgent(_rng);
        Telemetry = new WorldTelemetry();
        InstitutionSeeder.EnsureContinentOrgs(world, Telemetry);
    }

    public WorldState World { get; }
    public WorldTelemetry Telemetry { get; }

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

        Telemetry.DaysAdvanced += days;
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
            TradeClearing.RunMonth(World, Telemetry);
            CivicPipeline.RunMonth(World, Telemetry);
            TreatyEffects.RunMonth(World, Telemetry);
            _policyAgent.RunMonth(World, Telemetry);
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
                Telemetry.ProvincesCaptured++;
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
        Telemetry.WarsEnded++;

        DiplomaticInstruments.SignTreaty(World, Telemetry, TreatyKind.Peace, war.Attacker, war.Defender, durationDays: 360);
        World.Relations.Adjust(war.Attacker, war.Defender, seekPeace ? 8 : -5);
        World.AddEvent(
            GeoEventKind.PeaceSigned,
            $"Peace: {World.Polity(war.Attacker).Name} / {World.Polity(war.Defender).Name}",
            war.Attacker,
            war.Defender);
    }
}
