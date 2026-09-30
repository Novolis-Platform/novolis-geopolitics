namespace Novolis.Geopolitics.Core;

/// <summary>Abstract force strength by military domain.</summary>
public sealed class MilitaryForce
{
    public double Land { get; set; }
    public double Air { get; set; }
    public double Naval { get; set; }

    public double Total => Land + Air + Naval;

    public MilitaryForce Clone() => new()
    {
        Land = Land,
        Air = Air,
        Naval = Naval,
    };

    public void Scale(double factor)
    {
        Land *= factor;
        Air *= factor;
        Naval *= factor;
    }

    public void Add(MilitaryForce other)
    {
        Land += other.Land;
        Air += other.Air;
        Naval += other.Naval;
    }
}
