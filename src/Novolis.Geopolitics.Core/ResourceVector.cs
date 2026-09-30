namespace Novolis.Geopolitics.Core;

/// <summary>Per-kind resource quantities (production, consumption, or stock).</summary>
public sealed class ResourceVector
{
    private readonly double[] _v = new double[ResourceKinds.Count];

    public double this[ResourceKind kind]
    {
        get => _v[(int)kind];
        set => _v[(int)kind] = value;
    }

    public double Sum => _v.Sum();

    public ResourceVector Clone()
    {
        var c = new ResourceVector();
        Array.Copy(_v, c._v, _v.Length);
        return c;
    }

    public void Add(ResourceVector other)
    {
        for (var i = 0; i < _v.Length; i++)
        {
            _v[i] += other._v[i];
        }
    }

    public void Scale(double factor)
    {
        for (var i = 0; i < _v.Length; i++)
        {
            _v[i] *= factor;
        }
    }

    public double[] ToArray() => (double[])_v.Clone();

    public static ResourceVector FromArray(double[]? values)
    {
        var v = new ResourceVector();
        if (values is null)
        {
            return v;
        }

        var n = Math.Min(values.Length, ResourceKinds.Count);
        for (var i = 0; i < n; i++)
        {
            v._v[i] = values[i];
        }

        return v;
    }
}
