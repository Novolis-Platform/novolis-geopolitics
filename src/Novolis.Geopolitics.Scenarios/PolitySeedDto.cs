using System.Text.Json;
using System.Text.Json.Serialization;
using Novolis.Geopolitics.Core;

namespace Novolis.Geopolitics.Scenarios;

public sealed class PolitySeedDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Continent { get; set; } = "";
    public double Gdp { get; set; }
    public double Treasury { get; set; }
    public double TaxRate { get; set; }
    public double MilitaryBudgetShare { get; set; }
    public double Stability { get; set; }
    public double TechLevel { get; set; }
    public double Land { get; set; }
    public double Air { get; set; }
    public double Naval { get; set; }

    /// <summary>Optional; defaults to Democracy when missing from older seeds.</summary>
    public string? Government { get; set; }

    public double? TransferShare { get; set; }
    public double? InfrastructureShare { get; set; }
    public double? PropagandaShare { get; set; }
    public double? Legitimacy { get; set; }
    public double? Approval { get; set; }
    public double? Corruption { get; set; }
    public double? HumanDevelopment { get; set; }
}
