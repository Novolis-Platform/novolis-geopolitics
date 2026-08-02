using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using GeoPolity.AvaloniaUi.Views;
using GeoPolity.Session;
using Novolis.Avalonia.Agent;
using Novolis.Avalonia.Agent.Protocol;
using Novolis.Avalonia.Studio;

namespace GeoPolity.AvaloniaUi;

public sealed class MainWindow : Window
{
    private readonly GeoSession _session;
    private readonly StudioChrome _chrome = StudioChrome.Create();
    private readonly CommandPanel _command = new();
    private readonly TheatrePanel _theatre = new();
    private readonly HeadlinePanel _headlines = new();
    private readonly DispatcherTimer _timer;
    private Button? _runButton;

    public MainWindow(GeoSession session)
    {
        _session = session;
        Title = "GeoPolity";
        Width = 1180;
        Height = 760;
        MinWidth = 900;
        MinHeight = 560;
        Background = new SolidColorBrush(Color.Parse("#07131d"));

        Content = BuildChrome();
        KeyDown += OnKeyDown;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(_session.Clock.PulseMs) };
        _timer.Tick += (_, _) => OnPulseTick();
        _timer.Start();
        Refresh();
    }

    private Control BuildChrome()
    {
        var toolbar = BuildToolbar();
        var body = new Grid
        {
            ColumnDefinitions = ColumnDefinitions.Parse("320,*"),
            RowDefinitions = RowDefinitions.Parse("*,200"),
            Margin = new Thickness(8),
        };

        body.Children.Add(_command);
        Grid.SetColumn(_command, 0);
        Grid.SetRow(_command, 0);
        Grid.SetRowSpan(_command, 2);

        body.Children.Add(_theatre);
        Grid.SetColumn(_theatre, 1);
        Grid.SetRow(_theatre, 0);

        body.Children.Add(_headlines);
        Grid.SetColumn(_headlines, 1);
        Grid.SetRow(_headlines, 1);

        var root = new DockPanel { LastChildFill = true };
        var top = new StackPanel
        {
            [DockPanel.DockProperty] = Dock.Top,
            Children =
            {
                toolbar,
                _chrome.StatusLine,
                _chrome.FlashLine,
            },
        };
        root.Children.Add(top);
        root.Children.Add(body);

        var overlayHost = new Grid { Children = { root, _chrome.BusyOverlay } };
        return overlayHost;
    }

    private Control BuildToolbar()
    {
        _runButton = MakeButton("Run / Pause", "geopolity.run", () =>
        {
            GeoSessionCommands.ToggleRun(_session);
            Refresh();
        });

        var speeds = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Children =
            {
                MakeButton("Day", "geopolity.speed.day", () => SetSpeed(1)),
                MakeButton("Week", "geopolity.speed.week", () => SetSpeed(2)),
                MakeButton("Month", "geopolity.speed.month", () => SetSpeed(3)),
                MakeButton("Year", "geopolity.speed.year", () => SetSpeed(4)),
                MakeButton("5y", "geopolity.speed.5y", () => SetSpeed(5)),
                MakeButton("Step", "geopolity.step", () =>
                {
                    GeoSessionCommands.Step(_session, _session.Clock.DaysPerPulse);
                    Refresh();
                }),
            },
        };

        return new Border
        {
            Background = new SolidColorBrush(Color.Parse("#0b1c2c")),
            Padding = new Thickness(10, 8),
            BorderBrush = new SolidColorBrush(Color.Parse("#1e3a4c")),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = new DockPanel
            {
                LastChildFill = true,
                Children =
                {
                    new TextBlock
                    {
                        Text = "GEOPOLITY",
                        FontSize = 20,
                        FontWeight = FontWeight.Bold,
                        Foreground = new SolidColorBrush(Color.Parse("#2a9d8f")),
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 16, 0),
                        [DockPanel.DockProperty] = Dock.Left,
                    },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 8,
                        VerticalAlignment = VerticalAlignment.Center,
                        [DockPanel.DockProperty] = Dock.Right,
                        Children = { _runButton },
                    },
                    speeds,
                },
            },
        };
    }

    private void SetSpeed(int preset)
    {
        GeoSessionCommands.SetSpeed(_session, preset);
        _timer.Interval = TimeSpan.FromMilliseconds(_session.Clock.PulseMs);
        Refresh();
    }

    private void OnPulseTick()
    {
        if (_session.QuitRequested)
        {
            Close();
            return;
        }

        var beforeMs = _session.Clock.PulseMs;
        _session.PulseIfRunning();
        if (_session.Clock.PulseMs != beforeMs)
        {
            _timer.Interval = TimeSpan.FromMilliseconds(_session.Clock.PulseMs);
        }

        Refresh();
    }

    private void Refresh()
    {
        _command.Bind(_session);
        _theatre.Bind(_session);
        _headlines.Bind(_session);
        _chrome.StatusLine.Text =
            $"Y{_session.World.Year} M{_session.World.Month + 1} · " +
            $"{(_session.Clock.Running ? "RUN" : "PAUSE")} · {_session.Clock.SpeedLabel} · " +
            $"{_session.World.ActiveWars.Count()} wars";
        if (_runButton is not null)
        {
            _runButton.Content = _session.Clock.Running ? "Pause" : "Run";
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Space:
                GeoSessionCommands.ToggleRun(_session);
                e.Handled = true;
                break;
            case Key.D1:
            case Key.NumPad1:
                SetSpeed(1);
                e.Handled = true;
                break;
            case Key.D2:
            case Key.NumPad2:
                SetSpeed(2);
                e.Handled = true;
                break;
            case Key.D3:
            case Key.NumPad3:
                SetSpeed(3);
                e.Handled = true;
                break;
            case Key.D4:
            case Key.NumPad4:
                SetSpeed(4);
                e.Handled = true;
                break;
            case Key.D5:
            case Key.NumPad5:
                SetSpeed(5);
                e.Handled = true;
                break;
            case Key.Q:
                GeoSessionCommands.Quit(_session);
                Close();
                e.Handled = true;
                break;
        }

        if (e.Handled)
        {
            Refresh();
        }
    }

    private static Button MakeButton(string label, string agentId, Action onClick)
    {
        var btn = new Button
        {
            Content = label,
            Padding = new Thickness(10, 6),
            Background = new SolidColorBrush(Color.Parse("#163447")),
            Foreground = new SolidColorBrush(Color.Parse("#e8f1f4")),
            BorderBrush = new SolidColorBrush(Color.Parse("#2a9d8f")),
            BorderThickness = new Thickness(1),
        };
        btn.Click += (_, _) => onClick();
        AgentProperties.SetId(btn, agentId);
        AgentProperties.SetRole(btn, AgentRoleNames.Button);
        return btn;
    }
}
