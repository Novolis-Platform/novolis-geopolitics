namespace Novolis.Geopolitics.Core;

/// <summary>Stable supranational organization identifier.</summary>
public readonly record struct SupranationalId(long Value)
{
    public override string ToString() => Value.ToString();
}
