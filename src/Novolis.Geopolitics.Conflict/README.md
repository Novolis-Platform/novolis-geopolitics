<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-geopolitics">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Geopolitics.Conflict

Territorial conflict resolution over a `WorldState` from `Novolis.Geopolitics.Core`.

`ConflictResolver` finds active war frontiers (or coastal raid targets when no land frontier
exists), resolves one battle attempt per war per day using force composition, tech level, and
staging bonuses, and applies province captures directly to `Province.OwnerId`.

Depends only on Core. Does not decide when wars start or end — see
`Novolis.Geopolitics.Diplomacy` for `DeclareWar` and peace.

PackageId: `Novolis.Geopolitics.Conflict` (`2026.1.*` on GitHub Packages).

## Install

```bash
dotnet add package Novolis.Geopolitics.Conflict
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`).


