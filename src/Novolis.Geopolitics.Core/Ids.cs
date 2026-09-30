namespace Novolis.Geopolitics.Core;

/// <summary>Stable polity identifier (0-based index into WorldState.Polities).</summary>
public readonly record struct PolityId(int Value) : IComparable<PolityId>
{
    public int CompareTo(PolityId other) => Value.CompareTo(other.Value);
    public override string ToString() => Value.ToString();
}
