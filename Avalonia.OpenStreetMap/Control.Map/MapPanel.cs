using Avalonia.Controls;

namespace Avalonia.OpenStreetMap.Control.Map;

/// <summary>
/// Control that allows to position elements on <see cref="MapControl"/>.
/// </summary>
public class MapPanel : Panel
{
    public MapControl MapContext
    {
        get => _mapContext;
        set
        {
            SetAndRaise(MapContextProperty, ref _mapContext, value);

            MapControl.CenterProperty.Changed.AddClassHandler<MapControl>(CenterPropertyChanged);
        }
    }

    private void CenterPropertyChanged(MapControl map, AvaloniaPropertyChangedEventArgs args)
    {
        InvalidateArrange();
    }

    public static readonly DirectProperty<MapPanel, MapControl> MapContextProperty =
        AvaloniaProperty.RegisterDirect<MapPanel, MapControl>(
            nameof(MapContext),
            m => m.MapContext,
            (m, v) => m.MapContext = v);

    public static MapPoint GetMapPosition(IControl element)
    {
        return element.GetValue(MapPositionProperty);
    }

    public static void SetMapPosition(IControl element, MapPoint position)
    {
        element.SetValue(MapPositionProperty, position);
    }

    public static readonly AttachedProperty<MapPoint> MapPositionProperty =
        AvaloniaProperty.RegisterAttached<MapControl, MapPoint>(
            "MapPosition", typeof(MapControl), MapPoint.Zero);

    private MapControl _mapContext;

    static MapPanel()
    {
        AffectsArrange<MapControl>(MapPositionProperty);
        AffectsArrange<MapControl>(MapContextProperty);
    }

    public MapPanel()
    {
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (_mapContext == null)
            return finalSize;

        foreach (var child in Children)
        {
            var pos = _mapContext.MapLayer.WorldPointToScreenPoint(GetMapPosition(child));

            child.Arrange(new Rect(pos, child.DesiredSize));
        }

        return finalSize;
    }
}