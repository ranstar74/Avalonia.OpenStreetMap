namespace MapRender;

public class MapPoint
{
    /// <summary>
    /// Gets horizontal (X) coordinate.
    /// </summary>
    public double Longitude { get; }

    /// <summary>
    /// Gets vertical (Y) coordinate.
    /// </summary>
    public double Latitude { get; }

    public static readonly MapPoint Zero = new MapPoint(0.0, 0.0);

    public MapPoint(double lon, double lat)
    {
        Longitude = lon;
        Latitude = lat;
    }
}