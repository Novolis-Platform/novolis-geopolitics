namespace Novolis.Geopolitics.Core;

public sealed class War
{
    public required long Id { get; init; }
    public required PolityId Attacker { get; init; }
    public required PolityId Defender { get; init; }
    public int StartedDay { get; init; }
    public bool Active { get; set; } = true;
    public int EndedDay { get; set; } = -1;
    public int ProvincesTakenByAttacker { get; set; }
    public int ProvincesTakenByDefender { get; set; }
}
