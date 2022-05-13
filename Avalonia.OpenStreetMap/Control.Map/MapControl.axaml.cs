using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.OpenStreetMap.Control.Map.Shapes;

namespace Avalonia.OpenStreetMap.Control.Map;

public class MapControl : TemplatedControl
{
    public AvaloniaList<MapShape> Shapes { get; set; } = new();

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

    public MapLayer MapLayer { get; private set; }

    public MapOverlay MapOverlay { get; private set; }

    public static readonly StyledProperty<int> ZoomProperty =
        AvaloniaProperty.Register<MapControl, int>(nameof(Zoom));

    public static readonly StyledProperty<MapPoint> CenterProperty =
        AvaloniaProperty.Register<MapControl, MapPoint>(nameof(Center));

    private Button _partZoomInButton;
    private Button _partZoomOutButton;

    public MapControl()
    {
        
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        MapLayer = e.NameScope.Find<MapLayer>("PART_MapLayer");
        MapLayer.Zoom = Zoom;
        MapLayer.Center = Center;

        MapOverlay = e.NameScope.Find<MapOverlay>("PART_MapOverlay");
        MapOverlay.MapContext = this;

        _partZoomInButton = e.NameScope.Find<Button>("PART_ZoomInButton");
        _partZoomOutButton = e.NameScope.Find<Button>("PART_ZoomOutButton");
        _partZoomInButton.Click += (_, __) => MapLayer.Zoom++;
        _partZoomOutButton.Click += (_, __) => MapLayer.Zoom--;

        MapLayer.OnCenterChanged += () =>
        {
            Center = MapLayer.Center;
            MapOverlay.InvalidateVisual();
        };
        MapLayer.OnZoomChanged += () =>
        {
            Zoom = MapLayer.Zoom;
            MapOverlay.InvalidateVisual();
        };

        InvalidateArrange();
    }
}