using Avalonia.Media;

namespace Avalonia.OpenStreetMap.Control.Map;

public class MapOverlay : Controls.Control
{
    public MapControl MapContext { get; set; }

    public MapOverlay()
    {
        
    }

    public override void Render(DrawingContext dc)
    {
        if(MapContext == null)
            return;
        
        foreach (var shape in MapContext.Shapes)
        {
            shape.Draw(MapContext, dc);
        }
    }
}