using Avalonia.Media;

namespace Avalonia.OpenStreetMap.Control.Map.Shapes;

public class MapLine : MapShape
{
    public MapPoint StartPoint { get; set; }
    public MapPoint EndPoint { get; set; }

    public IPen Pen { get; set; }

    public MapLine()
    {
        
    }
    
    public MapLine(IPen pen, MapPoint startPoint, MapPoint endPoint)
    {
        Pen = pen;
        StartPoint = startPoint;
        EndPoint = endPoint;
    }

    public override void Draw(MapControl mapContext, DrawingContext dc)
    {
        var p1 = mapContext.MapLayer.WorldPointToScreenPoint(StartPoint);
        var p2 = mapContext.MapLayer.WorldPointToScreenPoint(EndPoint);

        dc.DrawLine(Pen, p1, p2);
    }
}