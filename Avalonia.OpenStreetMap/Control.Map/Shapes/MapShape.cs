using Avalonia.Media;

namespace Avalonia.OpenStreetMap.Control.Map.Shapes;

public abstract class MapShape
{
    public abstract void Draw(MapControl mapContext, DrawingContext dc);
}