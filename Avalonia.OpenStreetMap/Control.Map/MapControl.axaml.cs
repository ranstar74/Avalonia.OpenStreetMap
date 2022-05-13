using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Avalonia.OpenStreetMap.Control.Map;

public class MapControl : TemplatedPanel
{
    private Button _partZoomInButton;
    private Button _partZoomOutButton;
    private MapLayer _partMapLayer;

    public int Zoom
    {
        get => GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    public MapPoint Center
    {
        get => GetValue(CenterProperty);
        set => SetValue(CenterProperty, value);
    }

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

    public static readonly StyledProperty<int> ZoomProperty =
        AvaloniaProperty.Register<MapControl, int>(nameof(Zoom));

    public static readonly StyledProperty<MapPoint> CenterProperty =
        AvaloniaProperty.Register<MapControl, MapPoint>(nameof(Center));

    static MapControl()
    {
        AffectsArrange<MapControl>(MapPositionProperty);
    }

    public MapControl()
    {
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (_partMapLayer == null)
            return finalSize;

        foreach (var child in Children)
        {
            var pos = _partMapLayer.WorldPointToScreenPoint(GetMapPosition(child));

            child.Arrange(new Rect(pos, child.DesiredSize));
        }

        return finalSize;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        _partMapLayer = e.NameScope.Find<MapLayer>("PART_MapLayer");
        _partZoomInButton = e.NameScope.Find<Button>("PART_ZoomInButton");
        _partZoomOutButton = e.NameScope.Find<Button>("PART_ZoomOutButton");

        _partZoomInButton.Click += (_, __) => _partMapLayer.Zoom++;
        _partZoomOutButton.Click += (_, __) => _partMapLayer.Zoom--;

        InvalidateArrange();
    }
}