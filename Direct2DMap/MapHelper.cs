using System;
using Avalonia;

namespace MapRender;

public static class MapHelper
{
    /// <summary>
    /// Gets number of tiles per one axis at specified zoom.
    /// To get total number of tiles it needs to be raised to second power. 
    /// </summary>
    /// <param name="zoom">Zoom level.</param>
    /// <returns>An integer, representing number of tiles per axis.</returns>
    public static int GetNumberOfTilesAtZoom(int zoom)
    {
        return (int)Math.Pow(2, zoom);
    }

    public static Point WorldToTilePos(MapPoint point, int zoom)
    {
        double x = (point.Longitude + 180.0) / 360.0 * (1 << zoom);
        double y = (
            1.0 - Math.Log(Math.Tan(point.Latitude * Math.PI / 180.0) +
                           1.0 / Math.Cos(point.Latitude * Math.PI / 180.0)) / Math.PI) / 2.0 * (1 << zoom);

        return new Point(x, y);
    }

    public static MapPoint TileToWorldPos(Point point, double zoom)
    {
        int roundZoom = (int)Math.Floor(zoom);

        double n = Math.PI - 2.0 * Math.PI * point.Y / Math.Pow(2.0, roundZoom);

        double lon = (float)(point.X / Math.Pow(2.0, roundZoom) * 360.0 - 180.0);
        double lat = (float)(180.0 / Math.PI * Math.Atan(Math.Sinh(n)));

        return new MapPoint(lon, lat);
    }
}