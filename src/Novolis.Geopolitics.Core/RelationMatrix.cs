namespace Novolis.Geopolitics.Core;

/// <summary>Symmetric relation scores in [-100, 100] keyed by unordered polity pair.</summary>
public sealed class RelationMatrix
{
    private readonly Dictionary<long, double> _scores = new();

    public static long PairKey(PolityId a, PolityId b)
    {
        var x = a.Value;
        var y = b.Value;
        if (x > y)
        {
            (x, y) = (y, x);
        }

        return ((long)x << 32) | (uint)y;
    }

    public double Get(PolityId a, PolityId b)
    {
        if (a.Value == b.Value)
        {
            return 100.0;
        }

        return _scores.TryGetValue(PairKey(a, b), out var v) ? v : 0.0;
    }

    public void Set(PolityId a, PolityId b, double value)
    {
        if (a.Value == b.Value)
        {
            return;
        }

        _scores[PairKey(a, b)] = Math.Clamp(value, -100.0, 100.0);
    }

    public void Adjust(PolityId a, PolityId b, double delta) =>
        Set(a, b, Get(a, b) + delta);

    public IReadOnlyDictionary<long, double> Snapshot => _scores;
}
