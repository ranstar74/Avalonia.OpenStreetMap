namespace MapRender;

public struct MapTilePoint
{
    public double X { get; }
    public double Y { get; }

    public MapTilePoint(double x, double y)
    {
        X = x;
        Y = y;
    }
}