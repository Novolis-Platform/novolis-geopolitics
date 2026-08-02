using Novolis.Geopolitics.Core;

namespace Novolis.Geopolitics.Simulation;

/// <summary>SP2-inspired acceptance / refusal checks for bilateral treaties and org joins.</summary>
public static class DiplomaticRules
{
    public static double MinRelationFor(TreatyKind kind) => kind switch
    {
        TreatyKind.Peace => -100,
        TreatyKind.EconomicEmbargo or TreatyKind.WeaponTradeEmbargo => -100,
        TreatyKind.CulturalExchanges => 15,
        TreatyKind.EconomicPartnership => 25,
        TreatyKind.EconomicAid => 10,
        TreatyKind.CommonMarket => 35,
        TreatyKind.ResearchPartnership => 30,
        TreatyKind.MilitaryAccess => 40,
        TreatyKind.WeaponTrade => 35,
        TreatyKind.Alliance => 50,
        _ => 20,
    };

    public static TreatyRefusal EvaluateBilateral(
        WorldState world,
        TreatyKind kind,
        PolityId proposer,
        PolityId responder)
    {
        if (proposer == responder)
        {
            return TreatyRefusal.WrongProfile;
        }

        if (world.HaveTreaty(proposer, responder, kind))
        {
            return TreatyRefusal.AlreadyBound;
        }

        var atWar = world.AreAtWar(proposer, responder);
        if (kind is TreatyKind.EconomicEmbargo or TreatyKind.WeaponTradeEmbargo)
        {
            return TreatyRefusal.Accepted;
        }

        if (kind == TreatyKind.Peace)
        {
            return atWar || world.HaveTreaty(proposer, responder, TreatyKind.Peace)
                ? TreatyRefusal.Accepted
                : TreatyRefusal.WrongProfile;
        }

        if (atWar)
        {
            return TreatyRefusal.AtWar;
        }

        var a = world.Polity(proposer);
        var b = world.Polity(responder);
        var rel = world.Relations.Get(proposer, responder);
        // Autocracies / juntas need warmer relations for deep security/economic ties.
        var relationBar = MinRelationFor(kind);
        if (kind is TreatyKind.Alliance or TreatyKind.MilitaryAccess or TreatyKind.CommonMarket)
        {
            relationBar -= GovernmentRules.AllianceRelationBonus(b.Government);
            relationBar -= GovernmentRules.AllianceRelationBonus(a.Government) * 0.5;
        }

        if (rel < relationBar)
        {
            return TreatyRefusal.RelationsTooLow;
        }

        if ((b.Stability < 0.25 || b.Civic.Legitimacy < 0.22)
            && kind is TreatyKind.Alliance or TreatyKind.CommonMarket)
        {
            return TreatyRefusal.Unstable;
        }

        if (kind is TreatyKind.Alliance or TreatyKind.MilitaryAccess or TreatyKind.CommonMarket)
        {
            // Hostile with one of proposer friends (allies).
            foreach (var allyT in world.TreatiesContaining(proposer, TreatyKind.Alliance))
            {
                foreach (var friend in allyT.Members.Where(m => m != proposer))
                {
                    if (world.Relations.Get(responder, friend) < -35)
                    {
                        return TreatyRefusal.HostileWithFriend;
                    }
                }
            }
        }

        if (kind is TreatyKind.EconomicAid)
        {
            if (a.Treasury < a.Gdp * 0.02)
            {
                return TreatyRefusal.CannotAfford;
            }
        }

        if (kind is TreatyKind.Alliance or TreatyKind.CommonMarket)
        {
            var ratio = a.PowerScore / Math.Max(1, b.PowerScore);
            if (ratio > 8 || ratio < 0.12)
            {
                return TreatyRefusal.PowerImbalance;
            }
        }

        return TreatyRefusal.Accepted;
    }

    public static TreatyRefusal EvaluateOrgJoin(WorldState world, Supranational org, PolityId joiner)
    {
        if (!org.Active || org.MemberIds.Contains(joiner))
        {
            return TreatyRefusal.AlreadyBound;
        }

        if (org.MemberIds.Count == 0)
        {
            return TreatyRefusal.WrongProfile;
        }

        if (org.MemberIds.Any(m => world.AreAtWar(m, joiner)))
        {
            return TreatyRefusal.AtWar;
        }

        var medianRel = org.MemberIds
            .Select(m => world.Relations.Get(joiner, m))
            .OrderBy(x => x)
            .Skip(org.MemberIds.Count / 2)
            .First();
        if (medianRel < SupranationalCatalog.MinJoinRelation(org.Kind))
        {
            return TreatyRefusal.RelationsTooLow;
        }

        var self = world.Polity(joiner);
        if ((self.Stability < 0.3 || self.Civic.Legitimacy < 0.28)
            && org.Kind is SupranationalKind.DefenceAlliance or SupranationalKind.PoliticalUnion)
        {
            return TreatyRefusal.Unstable;
        }

        if (org.Kind is SupranationalKind.DefenceAlliance or SupranationalKind.PoliticalUnion)
        {
            foreach (var m in org.MemberIds)
            {
                foreach (var enemyWar in world.ActiveWars.Where(w => w.Attacker == m || w.Defender == m))
                {
                    var foe = enemyWar.Attacker == m ? enemyWar.Defender : enemyWar.Attacker;
                    if (world.Relations.Get(joiner, foe) > 40)
                    {
                        return TreatyRefusal.HostileWithFriend;
                    }
                }
            }
        }

        if (org.Kind == SupranationalKind.CustomsUnion || org.Kind == SupranationalKind.PoliticalUnion)
        {
            var avgGdp = org.MemberIds.Average(m => world.Polity(m).Gdp);
            if (self.Gdp < avgGdp * 0.08)
            {
                return TreatyRefusal.PowerImbalance;
            }
        }

        return TreatyRefusal.Accepted;
    }

    public static bool WouldAcceptBilateral(WorldState world, TreatyKind kind, PolityId proposer, PolityId responder) =>
        EvaluateBilateral(world, kind, proposer, responder) == TreatyRefusal.Accepted;

    public static bool WouldAcceptOrgJoin(WorldState world, Supranational org, PolityId joiner) =>
        EvaluateOrgJoin(world, org, joiner) == TreatyRefusal.Accepted;
}
