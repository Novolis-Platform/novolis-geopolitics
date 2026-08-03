# Population (Wave 1)

`PopulationMigration.RunMonth` moves `Province.Population` using Civics emigration pressure, tax differentials, war, and occupation. Call after civic settlement. Control ratios for civic months should use `WorldState.PopWeightedControlRatio`. Soft-blends polity GDP toward owned pop × wealth.

See also Civics `docs/demography-coupling.md`.
