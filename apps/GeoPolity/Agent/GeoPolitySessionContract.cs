using Novolis.Agent.Core;
using Novolis.Agent.Surface;

namespace GeoPolity.Agent;

[AgentSurface("geopolity",
    HttpPort = 18857,
    TcpPort = 18858,
    EnableEnv = "NOVOLIS_GEOPOLITY_SESSION",
    MarkerPrefix = "novolis-geopolity-session",
    Description = "GeoPolity session: pause, speed, step, snapshot")]
[AgentAction("pause", Summary = "Hard-pause the session clock")]
[AgentAction("resume", Summary = "Resume the session clock")]
[AgentAction("toggle", Summary = "Toggle run / pause")]
[AgentAction("setspeed", Summary = "Set speed preset", Params = "preset|1-5")]
[AgentAction("step", Summary = "Advance N days (paused or running)", Params = "days|1..3650")]
[AgentAction("advanceyears", Summary = "Burst-advance N years", Params = "years|1..100")]
public interface IGeoPolitySession : IAgentHost;

public static class GeoPolitySessionContract
{
    public static AgentSurfaceDefinition Definition { get; } =
        AgentSurfaceDefinition.From<IGeoPolitySession>();
}

public static class GeoPolityActionIds
{
    public const string Pause = "pause";
    public const string Resume = "resume";
    public const string Toggle = "toggle";
    public const string SetSpeed = "setspeed";
    public const string Step = "step";
    public const string AdvanceYears = "advanceyears";
}
