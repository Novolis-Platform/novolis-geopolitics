using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using GeoPolity.Session;
using Novolis.Avalonia.Briefing;
using Novolis.Geopolitics.Core;

namespace GeoPolity.AvaloniaUi.Views;

internal sealed class CommandPanel : UserControl
{
    private static readonly IBrush Navy = new SolidColorBrush(Color.Parse("#0b1c2c"));
    private static readonly IBrush Teal = new SolidColorBrush(Color.Parse("#2a9d8f"));
    private static readonly IBrush Copper = new SolidColorBrush(Color.Parse("#c47b3a"));
    private static readonly IBrush Fog = new SolidColorBrush(Color.Parse("#c8d4dc"));

    private readonly TextBlock _date = MakeValue();
    private readonly TextBlock _clock = MakeValue();
    private readonly TextBlock _world = MakeValue();
    private readonly TextBlock _wars = MakeValue();
    private readonly TextBlock _blocs = MakeValue();
    private readonly TextBlock _orgs = MakeValue();
    private readonly TextBlock _strain = MakeValue();
    private readonly TextBlock _civics = MakeValue();
    private readonly TextBlock _campaign = MakeValue();
    private readonly TextBlock _status = MakeMuted();
    private readonly StackPanel _powerList = new() { Spacing = 4 };
    private readonly DualMetricStrip _civicStrip = new()
    {
        LeftLabel = "Legitimacy",
        RightLabel = "Approval",
        Caption = "World mean civics",
        Margin = new Thickness(0, 8, 0, 8),
    };

    public CommandPanel()
    {
        Background = Navy;
        Padding = new Thickness(12);
        Content = new StackPanel
        {
            Spacing = 6,
            Children =
            {
                MakeHeader("BRIDGE"),
                Row("Date", _date),
                Row("Clock", _clock),
                _status,
                Row("World", _world),
                Row("Wars", _wars),
                Row("Blocs", _blocs),
                Row("Orgs", _orgs),
                Row("Strain", _strain),
                _civicStrip,
                Row("Campaign", _campaign),
                MakeHeader("POWER"),
                _powerList,
            },
        };
    }

    public void Bind(GeoSession session)
    {
        var world = session.World;
        var sim = session.Simulation;
        var clock = session.Clock;

        _date.Text = $"Y{world.Year} · M{world.Month + 1} · D{world.DayOfYear + 1}";
        _clock.Text = clock.Running
            ? $"RUN  {clock.DaysPerPulse}d / pulse ({clock.SpeedLabel})"
            : "PAUSE";
        _clock.Foreground = clock.Running ? Teal : Copper;
        _status.Text = clock.StatusNote ?? "";
        _world.Text = $"{world.Polities.Count} nations · {world.Provinces.Count} provinces";
        var wars = world.ActiveWars.Count();
        _wars.Text = wars.ToString();
        _wars.Foreground = wars == 0 ? Teal : wars < 15 ? Copper : Brushes.IndianRed;
        var cm = world.CountActiveTreatiesOfKind(TreatyKind.CommonMarket);
        var alliances = world.CountActiveTreatiesOfKind(TreatyKind.Alliance);
        var embargoes = world.CountActiveTreatiesOfKind(TreatyKind.EconomicEmbargo);
        _blocs.Text = $"{cm} markets · {alliances} alliances · {embargoes} embargoes";
        _orgs.Text = $"{world.ActiveOrgs.Count()} active · {sim.Stats.OrgJoins} joins";
        var shortage = world.Polities.Sum(p =>
            ResourceKinds.All.Sum(k => p.Balance[k] < 0 ? -p.Balance[k] : 0));
        _strain.Text = $"{shortage:0} shortage idx";
        _civicStrip.LeftValue = sim.Stats.MeanLegitimacy.ToString("0.00");
        _civicStrip.RightValue = sim.Stats.MeanApproval.ToString("0.00");
        _campaign.Text =
            $"wars {sim.Stats.WarsStarted}/{sim.Stats.WarsEnded} · captured {sim.Stats.ProvincesCaptured}";

        _powerList.Children.Clear();
        var maxPower = Math.Max(1.0, world.Polities.Max(p => p.PowerScore));
        var rank = 1;
        foreach (var p in world.Polities.OrderByDescending(x => x.PowerScore).Take(5))
        {
            _powerList.Children.Add(new TextBlock
            {
                Text = $"{rank}. {Truncate(p.Name, 14)}  {p.PowerScore:0}  {ShortGov(p.Government)} L{p.Civic.Legitimacy:0.00}",
                Foreground = Fog,
                FontSize = 12,
            });
            rank++;
        }

        _ = maxPower;
    }

    private static Control Row(string label, TextBlock value) =>
        new DockPanel
        {
            LastChildFill = true,
            Children =
            {
                new TextBlock
                {
                    Text = label,
                    Width = 72,
                    Foreground = Teal,
                    FontSize = 12,
                    [DockPanel.DockProperty] = Dock.Left,
                },
                value,
            },
        };

    private static TextBlock MakeHeader(string text) => new()
    {
        Text = text,
        Foreground = Copper,
        FontWeight = FontWeight.Bold,
        FontSize = 13,
        LetterSpacing = 1.5,
        Margin = new Thickness(0, 4, 0, 2),
    };

    private static TextBlock MakeValue() => new()
    {
        Foreground = Fog,
        FontSize = 13,
        TextWrapping = TextWrapping.Wrap,
    };

    private static TextBlock MakeMuted() => new()
    {
        Foreground = new SolidColorBrush(Color.Parse("#7a8a96")),
        FontSize = 11,
        Margin = new Thickness(0, 0, 0, 4),
    };

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..(max - 1)] + "…";

    private static string ShortGov(GovernmentType g) => g switch
    {
        GovernmentType.Democracy => "Dem",
        GovernmentType.Multiparty => "Multi",
        GovernmentType.SingleParty => "1Pty",
        GovernmentType.Autocracy => "Auto",
        GovernmentType.MilitaryJunta => "Junta",
        GovernmentType.Monarchy => "Mon",
        _ => "?",
    };
}
