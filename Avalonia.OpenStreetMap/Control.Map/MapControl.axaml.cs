using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Avalonia.OpenStreetMap.Control.Map;

public class MapControl : TemplatedControl
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

    public static readonly StyledProperty<int> ZoomProperty =
        AvaloniaProperty.Register<MapControl, int>(nameof(Zoom));

    public static readonly StyledProperty<MapPoint> CenterProperty =
        AvaloniaProperty.Register<MapControl, MapPoint>(nameof(Center));

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        _partMapLayer = e.NameScope.Find<MapLayer>("PART_MapLayer");
        _partZoomInButton = e.NameScope.Find<Button>("PART_ZoomInButton");
        _partZoomOutButton = e.NameScope.Find<Button>("PART_ZoomOutButton");

        _partZoomInButton.Click += (_, __) => _partMapLayer.Zoom++;
        _partZoomOutButton.Click += (_, __) => _partMapLayer.Zoom--;
    }
}