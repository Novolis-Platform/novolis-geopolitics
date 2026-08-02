# World seed attribution

`world-seed.json` (and the matching `ProceduralWorldGenerator`) is an **original fictional geography**:

- ~200 polities across 8 named continents
- ~800–1200 provinces with grid adjacency, resource weights, and a few intercontinental bridges
- Starting GDP, military, and relation priors from deterministic RNG (`seed = 20260802`, generator `procedural-v2`)
- Continent supranationals (charter Alliance / Common Market / Research) are applied at sim bootstrap, not baked into the JSON

It is **not** derived from SuperPower 2 `DATABASE.GDB`, Natural Earth country polygons, or any proprietary map pack. Continent/polity names are invented syllables.

See also [diplomacy-homage.md](diplomacy-homage.md).
