namespace Novolis.Geopolitics.Core;

/// <summary>
/// Cumulative counters observed while a <see cref="WorldState"/> advances — days ticked,
/// diplomatic and combat events, and monthly trade/civic aggregates. Written by the engines
/// in <c>Novolis.Geopolitics.Diplomacy</c>, <c>.Conflict</c>, <c>.Trade</c>, and <c>.PolicyAgents</c>;
/// read by composition (<c>Novolis.Geopolitics.Simulation</c>) and observers. Not itself a
/// simulation step — a plain accumulator record.
/// </summary>
public sealed class WorldTelemetry
{
    public int DaysAdvanced { get; set; }
    public int WarsStarted { get; set; }
    public int WarsEnded { get; set; }
    public int ProvincesCaptured { get; set; }
    public int TreatiesSigned { get; set; }
    public int TreatyJoins { get; set; }
    public int TreatyLeaves { get; set; }
    public int OrgsCreated { get; set; }
    public int OrgJoins { get; set; }
    public int OrgLeaves { get; set; }
    public int BudgetCrises { get; set; }
    public int TechAdvances { get; set; }
    public double CommonMarketVolume { get; set; }
    public double WorldMarketVolume { get; set; }
    public double EconomicPartnershipGdpBoost { get; set; }
    public double EconomicAidTransferred { get; set; }
    public int ResourceShortageEvents { get; set; }
    public double MeanLegitimacy { get; set; }
    public double MeanApproval { get; set; }
    public double PopulationMigrated { get; set; }
    public double RefugeesDisplaced { get; set; }
}
