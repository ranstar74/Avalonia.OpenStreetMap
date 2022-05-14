using Avalonia.Controls;
using Avalonia.Controls.Presenters;

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
    
    public static RelativePoint GetPinPoint(IControl element)
    {
        return element.GetValue(PinPointProperty);
    }

    public static void SetPinPoint(IControl element, RelativePoint position)
    {
        element.SetValue(PinPointProperty, position);
    }

    public static readonly AttachedProperty<MapPoint> MapPositionProperty =
        AvaloniaProperty.RegisterAttached<MapControl, MapPoint>(
            "MapPosition", typeof(MapControl), MapPoint.Zero);
    
    public static readonly AttachedProperty<RelativePoint> PinPointProperty =
        AvaloniaProperty.RegisterAttached<MapControl, RelativePoint>(
            "PinPoint", typeof(MapControl), RelativePoint.TopLeft);

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
            // We assume that if parent is ItemsPresenter then item is generated using ItemsControl,
            // then we need to get actual child from ContentPresenter.Child
            var actualChild = Parent is ItemsPresenter ? (child as ContentPresenter)!.Child : child;

            var pos = _mapContext.MapLayer.WorldPointToScreenPoint(GetMapPosition(actualChild));

            // Shift position to match pin point
            var pinPoint = GetPinPoint(actualChild);
            var pixelOffset = pinPoint.ToPixels(actualChild.Bounds.Size);
            pos -= pixelOffset;
            
            actualChild.Arrange(new Rect(pos, actualChild.DesiredSize));
        }

        return finalSize;
    }
}