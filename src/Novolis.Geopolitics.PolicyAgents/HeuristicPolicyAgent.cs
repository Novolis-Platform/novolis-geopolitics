using Novolis.Geopolitics.Core;
using Novolis.Geopolitics.Diplomacy;

namespace Novolis.Geopolitics.PolicyAgents;

/// <summary>Deterministic monthly heuristic policy agent with acceptance gates and org-aware agendas.</summary>
public sealed class HeuristicPolicyAgent(Random rng)
{
    private const int SoftCapCommonMarkets = 40;
    private const int SoftCapAlliances = 40;

    public void RunMonth(WorldState world, WorldTelemetry telemetry)
    {
        var order = Enumerable.Range(0, world.Polities.Count).ToList();
        Shuffle(order);

        foreach (var idx in order)
        {
            Decide(world, telemetry, world.Polities[idx]);
        }
    }

    private void Decide(WorldState world, WorldTelemetry telemetry, Polity self)
    {
        AdjustFiscalPolicy(self);

        // Quiet capability expansion — a fiscal-policy nudge only; CivicEngine settles the
        // Military stock from spend next month. No direct stock mutation here.
        if (self.Treasury > self.Gdp * 0.2 && self.Military.Total < self.Gdp * 0.002)
        {
            self.Policy.MilitaryShare = Math.Min(0.55, self.Policy.MilitaryShare + 0.02);
            SyncPolicyMirrors(self);
            if (rng.NextDouble() < 0.08)
            {
                world.AddEvent(GeoEventKind.ForceExpansion, $"{self.Name} expands land forces", self.Id);
            }
        }

        if (self.Treasury < 0 || self.Civic.Legitimacy < 0.2)
        {
            return;
        }

        var myWars = world.ActiveWars.Where(w => w.Attacker == self.Id || w.Defender == self.Id).ToList();
        if (myWars.Count >= 2
            || (myWars.Count == 1 && (self.Military.Total < 40 || self.Civic.WarFatigue > 0.65)))
        {
            foreach (var war in myWars)
            {
                if (rng.NextDouble() < 0.35 + self.Civic.WarFatigue * 0.25)
                {
                    war.Active = false;
                    war.EndedDay = world.Day;
                    telemetry.WarsEnded++;
                    DiplomaticInstruments.SignTreaty(world, telemetry, TreatyKind.Peace, war.Attacker, war.Defender, 360);
                    world.AddEvent(
                        GeoEventKind.PeaceSigned,
                        $"Armistice: {world.Polity(war.Attacker).Name} / {world.Polity(war.Defender).Name}",
                        war.Attacker,
                        war.Defender);
                    break;
                }
            }

            return;
        }

        var neighbors = NeighborPolities(world, self.Id).ToList();

        if (rng.NextDouble() < 0.22)
        {
            TryJoinOrg(world, telemetry, self);
        }

        if (neighbors.Count > 0 && rng.NextDouble() < 0.35)
        {
            TryDiplomacy(world, telemetry, self, neighbors, myWars.Count == 0);
        }

        if (neighbors.Count > 0 && rng.NextDouble() < 0.1)
        {
            TryAidOrEmbargo(world, telemetry, self, neighbors);
        }

        // War only with clear casus; civic legitimacy gates adventurism.
        if (myWars.Count == 0
            && self.Stability > 0.45
            && self.Civic.Legitimacy > 0.4
            && self.Civic.WarFatigue < 0.45
            && self.Military.Total > 80
            && rng.NextDouble() < 0.06)
        {
            TryDeclareWar(world, telemetry, self, neighbors);
        }
    }

    /// <summary>Agent only mutates <see cref="StateFiscalPolicy"/>; CivicEngine settles stocks next month.</summary>
    private static void AdjustFiscalPolicy(Polity self)
    {
        var p = self.Policy;
        var c = self.Civic;

        if (self.Treasury < 0)
        {
            p.MilitaryShare = Math.Max(0.1, p.MilitaryShare - 0.03);
            p.HouseholdTaxRate = Math.Min(0.5, p.HouseholdTaxRate + 0.015);
            p.TransferShare = Math.Max(0.05, p.TransferShare - 0.02);
            p.PropagandaShare = Math.Min(0.6, p.PropagandaShare + 0.03);
        }
        else if (c.Approval < 0.35)
        {
            p.HouseholdTaxRate = Math.Max(0.12, p.HouseholdTaxRate - 0.01);
            p.TransferShare = Math.Min(0.55, p.TransferShare + 0.02);
            if (self.Government is GovernmentType.Democracy or GovernmentType.Multiparty)
            {
                p.MilitaryShare = Math.Max(0.12, p.MilitaryShare - 0.015);
            }
        }
        else if (c.Legitimacy < 0.4)
        {
            p.PropagandaShare = Math.Min(0.55, p.PropagandaShare + 0.025);
            p.InfrastructureShare = Math.Min(0.7, p.InfrastructureShare + 0.01);
        }
        else if (c.WarFatigue > 0.5 || self.Military.Total < self.Gdp * 0.001)
        {
            // Wartime / under-armed: shift toward military; democracies less aggressively.
            var bump = self.Government is GovernmentType.MilitaryJunta or GovernmentType.Autocracy
                ? 0.025
                : 0.012;
            p.MilitaryShare = Math.Min(0.6, p.MilitaryShare + bump);
            p.TransferShare = Math.Max(0.08, p.TransferShare - 0.01);
        }
        else if (c.HumanDevelopment < 0.45 && self.Treasury > self.Gdp * 0.05)
        {
            p.InfrastructureShare = Math.Min(0.75, p.InfrastructureShare + 0.02);
            p.PropagandaShare = Math.Max(0.05, p.PropagandaShare - 0.01);
        }

        SyncPolicyMirrors(self);
    }

    private static void SyncPolicyMirrors(Polity self)
    {
        self.TaxRate = self.Policy.HouseholdTaxRate;
        self.MilitaryBudgetShare = self.Policy.MilitaryShare;
    }

    private void TryJoinOrg(WorldState world, WorldTelemetry telemetry, Polity self)
    {
        var candidates = world.ActiveOrgs
            .Where(o => !o.MemberIds.Contains(self.Id))
            .Where(o => o.ContinentHint is null || o.ContinentHint == self.Continent)
            .OrderBy(o => SupranationalCatalog.MinJoinRelation(o.Kind))
            .ToList();

        foreach (var org in candidates)
        {
            if (!DiplomaticRules.WouldAcceptOrgJoin(world, org, self.Id))
            {
                continue;
            }

            // Prefer forums/FTAs early; defence/unions when already stable and strong.
            var appetite = org.Kind switch
            {
                SupranationalKind.Forum => 0.55,
                SupranationalKind.FreeTradeArea => 0.4,
                SupranationalKind.ResearchForum => 0.35,
                SupranationalKind.CustomsUnion => self.Gdp > 200_000 ? 0.3 : 0.12,
                SupranationalKind.DefenceAlliance => self.Military.Total > 120 ? 0.28 : 0.1,
                SupranationalKind.PoliticalUnion => self.Stability > 0.6 ? 0.12 : 0.04,
                _ => 0.2,
            };

            if (rng.NextDouble() < appetite)
            {
                DiplomaticInstruments.JoinOrg(world, telemetry, org, self.Id);
                return;
            }
        }
    }

    private void TryDiplomacy(
        WorldState world,
        WorldTelemetry telemetry,
        Polity self,
        List<PolityId> neighbors,
        bool allowAlliance)
    {
        var other = neighbors[rng.Next(neighbors.Count)];
        if (world.AreAtWar(self.Id, other))
        {
            return;
        }

        var rel = world.Relations.Get(self.Id, other);
        var roll = rng.NextDouble();

        // Join existing continental CM / FTA treaties first.
        if (rel > 20 && roll < 0.3)
        {
            foreach (var kind in new[] { TreatyKind.CommonMarket, TreatyKind.EconomicPartnership, TreatyKind.CulturalExchanges })
            {
                var existing = world.ActiveTreaties(kind)
                    .FirstOrDefault(t =>
                        t.Members.Contains(other)
                        && !t.Members.Contains(self.Id)
                        && t.Members.Any(m => world.Polity(m).Continent == self.Continent));
                if (existing is not null
                    && DiplomaticRules.WouldAcceptBilateral(world, kind, self.Id, other))
                {
                    DiplomaticInstruments.JoinTreaty(world, telemetry, existing, self.Id);
                    return;
                }
            }
        }

        var offers = new (TreatyKind Kind, double MinRel, double Chance)[]
        {
            (TreatyKind.CulturalExchanges, 15, 0.5),
            (TreatyKind.EconomicPartnership, 25, 0.55),
            (TreatyKind.ResearchPartnership, 30, 0.35),
            (TreatyKind.MilitaryAccess, 40, 0.25),
            (TreatyKind.WeaponTrade, 35, 0.2),
            (TreatyKind.CommonMarket, 35, 0.2),
            (TreatyKind.Alliance, 50, allowAlliance ? 0.18 : 0),
        };

        foreach (var (kind, minRel, chance) in offers)
        {
            if (rel < minRel || rng.NextDouble() > chance)
            {
                continue;
            }

            if (world.HaveTreaty(self.Id, other, kind))
            {
                continue;
            }

            if (kind == TreatyKind.CommonMarket
                && world.CountActiveTreatiesOfKind(TreatyKind.CommonMarket) >= SoftCapCommonMarkets)
            {
                continue;
            }

            if (kind == TreatyKind.Alliance
                && world.CountActiveTreatiesOfKind(TreatyKind.Alliance) >= SoftCapAlliances)
            {
                continue;
            }

            if (!DiplomaticRules.WouldAcceptBilateral(world, kind, self.Id, other))
            {
                continue;
            }

            if (kind == TreatyKind.Alliance)
            {
                var existing = world.ActiveTreaties(TreatyKind.Alliance)
                    .FirstOrDefault(t => t.Members.Contains(other) && !t.Members.Contains(self.Id));
                if (existing is not null)
                {
                    DiplomaticInstruments.JoinTreaty(world, telemetry, existing, self.Id);
                    return;
                }
            }

            DiplomaticInstruments.SignTreaty(world, telemetry, kind, self.Id, other, DurationFor(kind));
            return;
        }
    }

    private void TryAidOrEmbargo(
        WorldState world,
        WorldTelemetry telemetry,
        Polity self,
        List<PolityId> neighbors)
    {
        var other = neighbors[rng.Next(neighbors.Count)];
        var rel = world.Relations.Get(self.Id, other);
        var them = world.Polity(other);

        if ((rel < -40 || world.AreAtWar(self.Id, other)) && !world.AreEmbargoed(self.Id, other))
        {
            DiplomaticInstruments.SignTreaty(world, telemetry, TreatyKind.EconomicEmbargo, self.Id, other, 720);
            return;
        }

        // Rich help poorer friend.
        if (rel >= 20 && self.Treasury > self.Gdp * 0.12 && them.Gdp < self.Gdp * 0.45
            && !world.HaveTreaty(self.Id, other, TreatyKind.EconomicAid)
            && DiplomaticRules.WouldAcceptBilateral(world, TreatyKind.EconomicAid, self.Id, other))
        {
            DiplomaticInstruments.SignTreaty(world, telemetry, TreatyKind.EconomicAid, self.Id, other, 360);
        }
    }

    private void TryDeclareWar(
        WorldState world,
        WorldTelemetry telemetry,
        Polity self,
        List<PolityId> neighbors)
    {
        PolityId? target = null;
        var best = double.MaxValue;
        foreach (var n in neighbors)
        {
            if (world.AreAllied(self.Id, n) || world.HaveTreaty(self.Id, n, TreatyKind.Peace))
            {
                continue;
            }

            // Same defence org → no war.
            if (world.ActiveOrgs.Any(o =>
                    o.Kind is SupranationalKind.DefenceAlliance or SupranationalKind.PoliticalUnion
                    && o.MemberIds.Contains(self.Id) && o.MemberIds.Contains(n)))
            {
                continue;
            }

            var rel = world.Relations.Get(self.Id, n);
            if (rel > -20)
            {
                continue; // need real hostility
            }

            var theirPower = world.Polity(n).PowerScore;
            var myPower = self.PowerScore;
            if (theirPower > myPower * 0.9)
            {
                continue;
            }

            var score = theirPower / Math.Max(1, myPower) + rel * 0.01;
            if (score < best)
            {
                best = score;
                target = n;
            }
        }

        if (target is { } t && best < 1.05)
        {
            DiplomaticInstruments.DeclareWar(world, telemetry, self.Id, t);
        }
    }

    private static int DurationFor(TreatyKind kind) => kind switch
    {
        TreatyKind.Alliance => 1440,
        TreatyKind.CommonMarket => 1080,
        TreatyKind.MilitaryAccess => 720,
        TreatyKind.EconomicAid => 360,
        _ => 720,
    };

    private static IEnumerable<PolityId> NeighborPolities(WorldState world, PolityId self)
    {
        var set = new HashSet<int>();
        foreach (var pr in world.Provinces)
        {
            if (pr.OwnerId != self)
            {
                continue;
            }

            foreach (var n in pr.Neighbors)
            {
                var owner = world.Province(n).OwnerId;
                if (owner != self)
                {
                    set.Add(owner.Value);
                }
            }
        }

        return set.Select(i => new PolityId(i));
    }

    private void Shuffle(List<int> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
