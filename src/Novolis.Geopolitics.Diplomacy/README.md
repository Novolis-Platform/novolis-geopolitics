<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-geopolitics/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-geopolitics/) · [Source](https://github.com/Novolis-Platform/novolis-geopolitics)
<!-- novolis-pkg-brand:end -->

# Novolis.Geopolitics.Diplomacy

Treaty and supranational-organization lifecycle over a `WorldState` from
`Novolis.Geopolitics.Core`.

| Type | Role |
|------|------|
| `DiplomaticInstruments` | Sign/join/leave treaties, create/join/leave orgs, declare war |
| `DiplomaticRules` | Acceptance/refusal evaluation for bilateral treaties and org joins |
| `TreatyEffects` | Monthly non-trade effects: partnership GDP boost, research catch-up, aid transfers, alliance relation floor |

Depends only on Core. `TreatyEffects.RunMonth` writes into `WorldTelemetry` (Core); it does not
mutate `CivicState` directly.

PackageId: `Novolis.Geopolitics.Diplomacy` (`2026.1.*` on GitHub Packages).

## Install

```bash
dotnet add package Novolis.Geopolitics.Diplomacy
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`).


