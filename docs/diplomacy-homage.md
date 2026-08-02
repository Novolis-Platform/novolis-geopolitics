# Diplomacy homage (SP2 pillars)

GeoPolity’s diplomacy layer is an **original** Novolis implementation inspired by SuperPower 2’s political/economic treaty pillars. It is **not** a port of the GolemLabs SDK, does not load `DATABASE.GDB`, and is not affiliated with THQ Nordic / GolemLabs.

| GeoPolity | SP2-inspired pillar | Our effect (homage, not parity) |
|-----------|---------------------|----------------------------------|
| `Alliance` (multilateral) | Alliance | Mutual defense join, relation floor |
| `MilitaryAccess` | Military access / trespass | Staging combat bonus |
| `EconomicPartnership` | Economic Partnership | Small GDP boost from partners |
| `CommonMarket` | Common Market | Resource surplus fills members first |
| `EconomicAid` | Economic Aid | Capped treasury transfer SideA→SideB |
| `EconomicEmbargo` | Economic Embargo | Blocks/reduces bilateral resource trade |
| `ResearchPartnership` | Research Partnership | Tech progress multiplier |
| `Peace` | Peace / war end | Directed ceasefire + redeclare cooldown |
| `Supranational` kinds | Named blocs | See below |

### Supranational archetypes

| Kind | Role | Linked treaties |
|------|------|-----------------|
| `Forum` | Talk shop / assembly | Cultural exchanges |
| `DefenceAlliance` | Mutual defense | Alliance + military access |
| `FreeTradeArea` | Preferential trade | Economic partnership |
| `CustomsUnion` | Resource-first market | Common market (+ FTA) |
| `ResearchForum` | Tech collaboration | Research partnership |
| `PoliticalUnion` | Deep integration | Defense + customs + research + culture |

Join barriers scale by kind (forum easy → political union hard). Bilateral offers use refusal reasons (relations, war with friend, power imbalance, affordability) inspired by SP2 treaty refusals — homage formulas only.

## Resources

Six abstract goods (`Food`, `Energy`, `Materials`, `Goods`, `MilitaryGoods`, `Rare`) flow monthly: domestic balance → common market → world market → shortage/surplus stability effects. This is **not** Novolis.Economy and not SP2’s full resource table.

## IP

Local Steam SDK may be used as **read-only inspiration** during development. Never vendor SDK sources, string tables, or GDB into this repository.
