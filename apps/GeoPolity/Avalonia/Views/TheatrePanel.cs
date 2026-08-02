using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using GeoPolity.Session;
using Novolis.Geopolitics.Core;

namespace GeoPolity.AvaloniaUi.Views;

internal sealed class TheatrePanel : UserControl
{
    private static readonly IBrush PanelBg = new SolidColorBrush(Color.Parse("#122636"));
    private static readonly IBrush Teal = new SolidColorBrush(Color.Parse("#2a9d8f"));
    private static readonly IBrush Copper = new SolidColorBrush(Color.Parse("#c47b3a"));
    private static readonly IBrush Fog = new SolidColorBrush(Color.Parse("#c8d4dc"));

    private readonly StackPanel _continents = new() { Spacing = 4 };
    private readonly StackPanel _orgs = new() { Spacing = 3 };

    public TheatrePanel()
    {
        Background = PanelBg;
        Padding = new Thickness(12);
        Content = new StackPanel
        {
            Spacing = 10,
            Children =
            {
                Header("CONTINENTS"),
                _continents,
                Header("SUPRANATIONALS"),
                _orgs,
            },
        };
    }

    public void Bind(GeoSession session)
    {
        var world = session.World;
        _continents.Children.Clear();
        var groups = world.Polities
            .GroupBy(p => p.Continent)
            .OrderByDescending(g => g.Sum(p => p.PowerScore))
            .Take(8)
            .ToList();
        var max = Math.Max(1.0, groups.Count == 0 ? 1 : groups.Max(g => g.Sum(p => p.PowerScore)));

        foreach (var g in groups)
        {
            var power = g.Sum(p => p.PowerScore);
            var wars = world.ActiveWars.Count(w =>
                world.Polity(w.Attacker).Continent == g.Key
                || world.Polity(w.Defender).Continent == g.Key);
            var frac = power / max;
            _continents.Children.Add(new StackPanel
            {
                Spacing = 2,
                Children =
                {
                    new TextBlock
                    {
                        Text = $"{g.Key} ({wars}w)  {power:0}",
                        Foreground = Fog,
                        FontSize = 12,
                    },
                    new ProgressBar
                    {
                        Minimum = 0,
                        Maximum = 1,
                        Value = frac,
                        Height = 8,
                        Foreground = Teal,
                    },
                },
            });
        }

        _orgs.Children.Clear();
        foreach (var o in world.ActiveOrgs.OrderByDescending(x => x.MemberIds.Count).Take(12))
        {
            _orgs.Children.Add(new TextBlock
            {
                Text = $"{SupranationalCatalog.ShortLabel(o.Kind),-8}  {Truncate(o.Name, 22)}  {o.MemberIds.Count}",
                Foreground = Fog,
                FontSize = 12,
                FontFamily = new FontFamily("Cascadia Mono, Consolas, monospace"),
            });
        }

        if (_orgs.Children.Count == 0)
        {
            _orgs.Children.Add(new TextBlock { Text = "no blocs", Foreground = Copper, FontSize = 12 });
        }
    }

    private static TextBlock Header(string text) => new()
    {
        Text = text,
        Foreground = Copper,
        FontWeight = FontWeight.Bold,
        FontSize = 13,
        LetterSpacing = 1.2,
    };

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..(max - 1)] + "…";
}
