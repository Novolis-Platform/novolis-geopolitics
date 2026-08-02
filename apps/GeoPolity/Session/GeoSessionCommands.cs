namespace GeoPolity.Session;

/// <summary>Pure session command helpers — keys, Avalonia buttons, and Agent Execute share this path.</summary>
public static class GeoSessionCommands
{
    public static void Pause(GeoSession session) => session.Clock.Pause();

    public static void Resume(GeoSession session) => session.Clock.Resume();

    public static void ToggleRun(GeoSession session) => session.Clock.ToggleRun();

    public static void SetSpeed(GeoSession session, int preset) =>
        session.Clock.SetSpeedPreset(preset);

    public static void Step(GeoSession session, int days)
    {
        var d = Math.Clamp(days, 1, 3650);
        session.AdvanceDays(d);
        session.Clock.StatusNote = $"step {d}d";
    }

    public static void AdvanceYears(GeoSession session, int years)
    {
        var y = Math.Clamp(years, 1, 100);
        session.AdvanceYears(y);
        session.Clock.StatusNote = $"advanced {y}y";
    }

    public static void Quit(GeoSession session) => session.RequestQuit();
}
