namespace Novolis.Geopolitics.Core;

public static class ResourceKinds
{
    public const int Count = 6;

    public static readonly ResourceKind[] All =
    [
        ResourceKind.Food,
        ResourceKind.Energy,
        ResourceKind.Materials,
        ResourceKind.Goods,
        ResourceKind.MilitaryGoods,
        ResourceKind.Rare,
    ];
}
