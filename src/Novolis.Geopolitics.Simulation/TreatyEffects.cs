using Novolis.Geopolitics.Core;

namespace Novolis.Geopolitics.Simulation;

/// <summary>Monthly non-trade treaty effects: EP GDP, research, aid transfers, alliance relation floor.</summary>
public static class TreatyEffects
{
    public static void RunMonth(WorldState world, GeoSimulationStats stats)
    {
        ApplyEconomicPartnerships(world, stats);
        ApplyResearchPartnerships(world);
        ApplyCulturalExchanges(world);
        ApplyEconomicAid(world, stats);
        ApplyAllianceRelationFloor(world);
        ApplyForumDrift(world);
    }

    private static void ApplyCulturalExchanges(WorldState world)
    {
        foreach (var cx in world.ActiveTreaties(TreatyKind.CulturalExchanges))
        {
            var members = cx.Members.ToList();
            for (var i = 0; i < members.Count; i++)
            {
                for (var j = i + 1; j < members.Count; j++)
                {
                    // Homage to SP2 yearly cultural relation gain (~1/year → ~0.08/month).
                    world.Relations.Adjust(members[i], members[j], 0.08);
                }
            }
        }
    }

    private static void ApplyForumDrift(WorldState world)
    {
        foreach (var org in world.ActiveOrgs.Where(o => o.Kind == SupranationalKind.Forum))
        {
            var members = org.MemberIds.ToList();
            if (members.Count < 2)
            {
                continue;
            }

            // Light multilateral socialization inside forums.
            for (var i = 0; i < members.Count; i++)
            {
                var j = (i + 1 + (world.Day % 7)) % members.Count;
                if (i != j)
                {
                    world.Relations.Adjust(members[i], members[j], 0.03);
                }
            }
        }
    }

    private static void ApplyEconomicPartnerships(WorldState world, GeoSimulationStats stats)
    {
        foreach (var ep in world.ActiveTreaties(TreatyKind.EconomicPartnership))
        {
            var members = ep.Members.ToList();
            if (members.Count < 2)
            {
                continue;
            }

            var totalGdp = members.Sum(m => world.Polity(m).Gdp);
            if (totalGdp <= 0)
            {
                continue;
            }

            foreach (var id in members)
            {
                var p = world.Polity(id);
                // Homage: small production bonus scaled by partners' relative size (cap ~0.1%/month).
                var partnerShare = (totalGdp - p.Gdp) / totalGdp;
                var boost = 0.001 * Math.Min(1.0, partnerShare * 2.0);
                var delta = p.Gdp * boost / 12.0;
                p.Gdp += delta;
                stats.EconomicPartnershipGdpBoost += delta;
            }
        }
    }

    private static void ApplyResearchPartnerships(WorldState world)
    {
        foreach (var rp in world.ActiveTreaties(TreatyKind.ResearchPartnership))
        {
            var members = rp.Members.ToList();
            if (members.Count < 2)
            {
                continue;
            }

            var maxTech = members.Max(m => world.Polity(m).TechLevel);
            foreach (var id in members)
            {
                var p = world.Polity(id);
                var gap = Math.Max(0, maxTech - p.TechLevel);
                p.TechProgress += 2.0 + gap * 4.0;
            }
        }
    }

    private static void ApplyEconomicAid(WorldState world, GeoSimulationStats stats)
    {
        foreach (var aid in world.ActiveTreaties(TreatyKind.EconomicAid))
        {
            foreach (var donorId in aid.SideA)
            {
                var donor = world.Polity(donorId);
                var monthlyBudget = donor.Gdp * donor.TaxRate / 12.0;
                var pledge = monthlyBudget * 0.01; // 1% of budget expenses homage
                if (pledge <= 0 || donor.Treasury < pledge)
                {
                    continue;
                }

                var recipients = aid.SideB.ToList();
                if (recipients.Count == 0)
                {
                    continue;
                }

                var each = pledge / recipients.Count;
                donor.Treasury -= pledge;
                foreach (var r in recipients)
                {
                    var recipient = world.Polity(r);
                    recipient.Treasury += each;
                    // Aid softens domestic pressure (CivicEngine settles next month).
                    recipient.Civic.Approval = Math.Min(1, recipient.Civic.Approval + 0.002);
                    recipient.Civic.HumanDevelopment = Math.Min(2, recipient.Civic.HumanDevelopment + 0.001);
                    world.Relations.Adjust(donorId, r, 0.5);
                }

                stats.EconomicAidTransferred += pledge;
            }
        }
    }

    private static void ApplyAllianceRelationFloor(WorldState world)
    {
        foreach (var al in world.ActiveTreaties(TreatyKind.Alliance))
        {
            var members = al.Members.ToList();
            for (var i = 0; i < members.Count; i++)
            {
                for (var j = i + 1; j < members.Count; j++)
                {
                    if (world.Relations.Get(members[i], members[j]) < 25)
                    {
                        world.Relations.Set(members[i], members[j], 25);
                    }
                }
            }
        }
    }
}
