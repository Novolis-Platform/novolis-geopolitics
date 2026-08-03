using Novolis.Geopolitics.Core;

namespace Novolis.Geopolitics.Diplomacy;

/// <summary>Treaty and supranational-organization lifecycle: sign, join, leave, declare war.</summary>
public static class DiplomaticInstruments
{
    public static bool IsMultilateral(TreatyKind kind) => !Treaty.IsDirected(kind);

    /// <summary>Bilateral convenience: Peace/Aid/Embargo use sides; others create a 2-member multilateral.</summary>
    public static Treaty? SignTreaty(
        WorldState world,
        WorldTelemetry telemetry,
        TreatyKind kind,
        PolityId a,
        PolityId b,
        int durationDays,
        string? name = null)
    {
        if (a == b)
        {
            return null;
        }

        if (kind != TreatyKind.Peace && world.AreAtWar(a, b) && kind is not TreatyKind.EconomicEmbargo
            and not TreatyKind.WeaponTradeEmbargo)
        {
            return null;
        }

        if (world.HaveTreaty(a, b, kind))
        {
            return null;
        }

        // Hostile instruments and peace skip bilateral consent gates (peace is set by war end).
        if (kind is not TreatyKind.EconomicEmbargo and not TreatyKind.WeaponTradeEmbargo
            and not TreatyKind.Peace)
        {
            if (!DiplomaticRules.WouldAcceptBilateral(world, kind, a, b))
            {
                return null;
            }

            if (kind is TreatyKind.Alliance or TreatyKind.CommonMarket or TreatyKind.EconomicPartnership
                or TreatyKind.MilitaryAccess or TreatyKind.ResearchPartnership or TreatyKind.CulturalExchanges
                or TreatyKind.WeaponTrade)
            {
                if (!DiplomaticRules.WouldAcceptBilateral(world, kind, b, a))
                {
                    return null;
                }
            }
        }

        if (Treaty.IsDirected(kind))
        {
            return CreateDirected(world, telemetry, kind, a, b, durationDays, name);
        }

        return CreateMultilateral(world, telemetry, kind, a, [a, b], durationDays, name);
    }

    public static Treaty? CreateMultilateral(
        WorldState world,
        WorldTelemetry telemetry,
        TreatyKind kind,
        PolityId creator,
        IEnumerable<PolityId> members,
        int durationDays,
        string? name = null,
        SupranationalId? linkedOrg = null)
    {
        if (Treaty.IsDirected(kind))
        {
            throw new ArgumentException("Use CreateDirected for directed treaty kinds.", nameof(kind));
        }

        var memberSet = members.ToHashSet();
        if (memberSet.Count < 2)
        {
            return null;
        }

        // Refuse if any pair already shares this kind (except when expanding via Join).
        foreach (var x in memberSet)
        {
            foreach (var y in memberSet)
            {
                if (x.Value >= y.Value)
                {
                    continue;
                }

                if (world.AreAtWar(x, y) && kind is not TreatyKind.EconomicEmbargo)
                {
                    return null;
                }
            }
        }

        var treaty = new Treaty
        {
            Id = world.NextTreatyId++,
            Name = name ?? DefaultName(world, kind, creator),
            Kind = kind,
            Creator = creator,
            SignedDay = world.Day,
            ExpiresDay = durationDays < 0 ? -1 : world.Day + durationDays,
            Active = true,
            LinkedOrgId = linkedOrg,
        };
        foreach (var m in memberSet)
        {
            treaty.Members.Add(m);
        }

        world.Treaties.Add(treaty);
        telemetry.TreatiesSigned++;
        BoostRelationsAmong(world, treaty.Members, RelationBoost(kind));
        EmitFormed(world, treaty);
        return treaty;
    }

    public static Treaty? CreateDirected(
        WorldState world,
        WorldTelemetry telemetry,
        TreatyKind kind,
        PolityId sideA,
        PolityId sideB,
        int durationDays,
        string? name = null)
    {
        if (!Treaty.IsDirected(kind) || sideA == sideB)
        {
            return null;
        }

        if (world.HaveTreaty(sideA, sideB, kind))
        {
            return null;
        }

        var treaty = new Treaty
        {
            Id = world.NextTreatyId++,
            Name = name ?? DefaultName(world, kind, sideA),
            Kind = kind,
            Creator = sideA,
            SignedDay = world.Day,
            ExpiresDay = durationDays < 0 ? -1 : world.Day + durationDays,
            Active = true,
        };
        treaty.SideA.Add(sideA);
        treaty.SideB.Add(sideB);
        world.Treaties.Add(treaty);
        telemetry.TreatiesSigned++;

        if (kind == TreatyKind.EconomicEmbargo || kind == TreatyKind.WeaponTradeEmbargo)
        {
            world.Relations.Adjust(sideA, sideB, -12);
            world.AddEvent(
                GeoEventKind.EmbargoImposed,
                $"{treaty.Name}: {world.Polity(sideA).Name} ↔ {world.Polity(sideB).Name}",
                sideA,
                sideB);
        }

        return treaty;
    }

    public static bool JoinTreaty(WorldState world, WorldTelemetry telemetry, Treaty treaty, PolityId joiner)
    {
        if (!treaty.Active || Treaty.IsDirected(treaty.Kind) || treaty.Members.Contains(joiner))
        {
            return false;
        }

        if (treaty.Members.Any(m => world.AreAtWar(m, joiner)))
        {
            return false;
        }

        treaty.Members.Add(joiner);
        telemetry.TreatyJoins++;
        foreach (var m in treaty.Members)
        {
            if (m != joiner)
            {
                world.Relations.Adjust(joiner, m, RelationBoost(treaty.Kind) * 0.5);
            }
        }

        world.AddEvent(
            GeoEventKind.TreatyJoined,
            $"{world.Polity(joiner).Name} joined {treaty.Name}",
            joiner);
        return true;
    }

    public static bool LeaveTreaty(WorldState world, WorldTelemetry telemetry, Treaty treaty, PolityId leaver)
    {
        if (!treaty.Active || !treaty.Contains(leaver))
        {
            return false;
        }

        if (Treaty.IsDirected(treaty.Kind))
        {
            treaty.SideA.Remove(leaver);
            treaty.SideB.Remove(leaver);
            if (treaty.SideA.Count == 0 || treaty.SideB.Count == 0)
            {
                treaty.Active = false;
            }
        }
        else
        {
            treaty.Members.Remove(leaver);
            foreach (var m in treaty.Members)
            {
                world.Relations.Adjust(leaver, m, -10);
            }

            if (treaty.Members.Count < 2)
            {
                treaty.Active = false;
            }
        }

        telemetry.TreatyLeaves++;
        world.AddEvent(
            GeoEventKind.TreatyLeft,
            $"{world.Polity(leaver).Name} left {treaty.Name}",
            leaver);

        if (treaty.Kind == TreatyKind.Alliance && !treaty.Active)
        {
            world.AddEvent(GeoEventKind.AllianceBroken, $"{treaty.Name} dissolved");
        }

        return true;
    }

    public static Supranational CreateOrg(
        WorldState world,
        WorldTelemetry telemetry,
        string name,
        string? continentHint,
        IEnumerable<PolityId> members,
        SupranationalKind kind,
        SupranationalCharter? charter = null)
    {
        charter ??= SupranationalCatalog.CharterFor(kind);
        var org = new Supranational
        {
            Id = new SupranationalId(world.NextOrgId++),
            Name = name,
            Kind = kind,
            ContinentHint = continentHint,
            Charter = charter,
            Active = true,
        };
        foreach (var m in members)
        {
            org.MemberIds.Add(m);
        }

        world.Supranationals.Add(org);
        telemetry.OrgsCreated++;

        var list = org.MemberIds.ToList();
        if (list.Count >= 2)
        {
            var creator = list.OrderByDescending(id => world.Polity(id).Gdp).First();
            foreach (var (treatyKind, suffix) in SupranationalCatalog.LinkedInstruments(kind))
            {
                var t = CreateMultilateral(world, telemetry, treatyKind, creator, list, -1,
                    $"{name} {suffix}", org.Id);
                if (t is not null)
                {
                    org.LinkedTreatyIds.Add(t.Id);
                }
            }
        }

        world.AddEvent(
            GeoEventKind.TreatyFormed,
            $"Org founded: {name} ({SupranationalCatalog.ShortLabel(kind)})",
            list.Count > 0 ? list[0] : default);
        return org;
    }

    public static bool JoinOrg(WorldState world, WorldTelemetry telemetry, Supranational org, PolityId joiner)
    {
        if (DiplomaticRules.EvaluateOrgJoin(world, org, joiner) != TreatyRefusal.Accepted)
        {
            return false;
        }

        org.MemberIds.Add(joiner);
        telemetry.OrgJoins++;
        foreach (var tid in org.LinkedTreatyIds)
        {
            var t = world.Treaties.FirstOrDefault(x => x.Id == tid && x.Active);
            if (t is not null)
            {
                JoinTreaty(world, telemetry, t, joiner);
            }
        }

        world.AddEvent(
            GeoEventKind.OrgJoined,
            $"{world.Polity(joiner).Name} joined {org.Name}",
            joiner);
        return true;
    }

    public static bool LeaveOrg(WorldState world, WorldTelemetry telemetry, Supranational org, PolityId leaver)
    {
        if (!org.Active || !org.MemberIds.Remove(leaver))
        {
            return false;
        }

        telemetry.OrgLeaves++;
        foreach (var tid in org.LinkedTreatyIds)
        {
            var t = world.Treaties.FirstOrDefault(x => x.Id == tid);
            if (t is not null && t.Contains(leaver))
            {
                LeaveTreaty(world, telemetry, t, leaver);
            }
        }

        foreach (var m in org.MemberIds)
        {
            world.Relations.Adjust(leaver, m, -10);
        }

        world.AddEvent(
            GeoEventKind.OrgLeft,
            $"{world.Polity(leaver).Name} left {org.Name}",
            leaver);

        if (org.MemberIds.Count < 2)
        {
            org.Active = false;
        }

        return true;
    }

    public static War? DeclareWar(WorldState world, WorldTelemetry telemetry, PolityId attacker, PolityId defender)
    {
        if (attacker == defender || world.AreAtWar(attacker, defender))
        {
            return null;
        }

        if (world.HaveTreaty(attacker, defender, TreatyKind.Peace))
        {
            var peace = world.FindSharedTreaty(attacker, defender, TreatyKind.Peace);
            if (peace is not null && world.Day - peace.SignedDay < 180)
            {
                return null;
            }

            if (peace is not null)
            {
                peace.Active = false;
            }
        }

        if (world.AreAllied(attacker, defender))
        {
            return null;
        }

        var war = new War
        {
            Id = world.NextWarId++,
            Attacker = attacker,
            Defender = defender,
            StartedDay = world.Day,
            Active = true,
        };
        world.Wars.Add(war);
        telemetry.WarsStarted++;
        world.Relations.Set(attacker, defender, Math.Min(world.Relations.Get(attacker, defender), -40));
        world.AddEvent(
            GeoEventKind.WarDeclared,
            $"War: {world.Polity(attacker).Name} → {world.Polity(defender).Name}",
            attacker,
            defender);

        foreach (var allyTreaty in world.TreatiesContaining(defender, TreatyKind.Alliance).ToList())
        {
            foreach (var ally in allyTreaty.Members.Where(m => m != defender).ToList())
            {
                if (ally == attacker || world.AreAtWar(ally, attacker))
                {
                    continue;
                }

                if (world.Relations.Get(ally, defender) >= 40 && world.Relations.Get(ally, attacker) < 15)
                {
                    DeclareWar(world, telemetry, ally, attacker);
                }
            }
        }

        return war;
    }

    private static void BoostRelationsAmong(WorldState world, IEnumerable<PolityId> members, double boost)
    {
        var list = members.ToList();
        for (var i = 0; i < list.Count; i++)
        {
            for (var j = i + 1; j < list.Count; j++)
            {
                world.Relations.Adjust(list[i], list[j], boost);
            }
        }
    }

    private static double RelationBoost(TreatyKind kind) => kind switch
    {
        TreatyKind.Alliance => 20,
        TreatyKind.MilitaryAccess => 10,
        TreatyKind.EconomicPartnership => 8,
        TreatyKind.CommonMarket => 12,
        TreatyKind.ResearchPartnership => 6,
        TreatyKind.EconomicAid => 5,
        TreatyKind.CulturalExchanges => 4,
        TreatyKind.WeaponTrade => 7,
        _ => 2,
    };

    private static void EmitFormed(WorldState world, Treaty treaty)
    {
        var kind = treaty.Kind switch
        {
            TreatyKind.Alliance => GeoEventKind.AllianceFormed,
            TreatyKind.EconomicPartnership or TreatyKind.CommonMarket or TreatyKind.CulturalExchanges
                or TreatyKind.WeaponTrade => GeoEventKind.TradeSigned,
            _ => GeoEventKind.TreatyFormed,
        };
        world.AddEvent(kind, $"Treaty formed: {treaty.Name} ({treaty.Kind})", treaty.Creator);
    }

    private static string DefaultName(WorldState world, TreatyKind kind, PolityId creator) =>
        $"{world.Polity(creator).Name} {kind} #{world.NextTreatyId}";
}
