<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-geopolitics">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Geopolitics.Trade

Monthly resource clearing over a `WorldState` from `Novolis.Geopolitics.Core`.

`TradeClearing.RunMonth` computes each Polity's domestic production/consumption from owned
Provinces, pools surplus/deficit within `CommonMarket` treaty members, clears a GDP-weighted
world market for the remainder, and feeds shortage/surplus back into `Stability` and `Gdp`.

Depends only on Core. Writes volumes and shortage counts into `WorldTelemetry`.

PackageId: `Novolis.Geopolitics.Trade` (`2026.1.*` on GitHub Packages).

## Install

```bash
dotnet add package Novolis.Geopolitics.Trade
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`).


