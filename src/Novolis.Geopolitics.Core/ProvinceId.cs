namespace Novolis.Geopolitics.Core;

/// <summary>Stable province identifier (0-based index into WorldState.Provinces).</summary>
public readonly record struct ProvinceId(int Value) : IComparable<ProvinceId>
{
    public int CompareTo(ProvinceId other) => Value.CompareTo(other.Value);
    public override string ToString() => Value.ToString();
}
