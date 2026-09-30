namespace Novolis.Geopolitics.Core;

public sealed class GeoEvent
{
    public required int Day { get; init; }
    public required GeoEventKind Kind { get; init; }
    public required string Message { get; init; }
    public PolityId? PolityA { get; init; }
    public PolityId? PolityB { get; init; }
    public ProvinceId? Province { get; init; }
}
