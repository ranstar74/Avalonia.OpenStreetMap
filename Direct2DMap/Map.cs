using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Skia;
using Avalonia.Threading;
using MapRender;
using SkiaSharp;

namespace Direct2DMap;

public class Map : Control
{
    private int RenderWidth => (int)Bounds.Width;
    private int RenderHeight => (int)Bounds.Height;

    private RenderTargetBitmap _rt;
    private ISkiaDrawingContextImpl _skContext;

    private readonly SemaphoreSlim _updateSemaphore = new(1, 1);

    private MapPoint _centerPoint = new MapPoint(37.617187, 55.755508);
    private int _zoom = 8;

    private bool _isDragging;
    private Point _prevDragPos;

    private readonly SKPaint _whitePaint;

    public Map()
    {
        BoundsProperty.Changed.AddClassHandler<Map>(ResizeMap);

        PointerPressed += PointerPressedHandler;
        PointerMoved += PointerMoveHandler;
        PointerReleased += PointerReleasedHandler;
        PointerWheelChanged += PointerWheelHandler;

        _whitePaint = new SKPaint();
        _whitePaint.Color = SKColors.White;

        MapCache.OnDownloadFinished += async () => await RenderMap();
    }

    private async void PointerWheelHandler(object sender, PointerWheelEventArgs e)
    {
        int delta = (int)e.Delta.Y;

        _zoom += delta;

        await RenderMap();
    }

    private async void ResizeMap(Map map, AvaloniaPropertyChangedEventArgs args)
    {
        // Skip until we have arranged bounds
        if (RenderWidth == 0 || RenderHeight == 0) return;

        _rt = new RenderTargetBitmap(new PixelSize(RenderWidth, RenderHeight), new Vector(96, 96));
        _skContext = (_rt.CreateDrawingContext(null) as ISkiaDrawingContextImpl)!;

        await RenderMap();
    }

    private async void PointerMoveHandler(object sender, PointerEventArgs e)
    {
        if (!_isDragging)
            return;

        var pos = e.GetPosition(this);

        var posDelta = PixelToTiles(pos) - PixelToTiles(_prevDragPos);

        var centerPointTile = MapHelper.WorldToTilePos(_centerPoint, _zoom);
        centerPointTile -= posDelta;

        _centerPoint = MapHelper.TileToWorldPos(centerPointTile, _zoom);
        _prevDragPos = pos;

        await RenderMap();
    }

    private void PointerReleasedHandler(object sender, PointerReleasedEventArgs e)
    {
        _isDragging = false;
    }

    private async void PointerPressedHandler(object sender, PointerPressedEventArgs e)
    {
        _prevDragPos = e.GetPosition(this);
        _isDragging = true;
    }

    private static Point PixelToTiles(Point point)
    {
        return point / 256;
    }

    private CancellationTokenSource _renderCancellation;

    private async Task RenderMap()
    {
        _renderCancellation?.Cancel();
        await _updateSemaphore.WaitAsync();
        try
        {
            _renderCancellation = new CancellationTokenSource();
            await RenderMap(RenderWidth, RenderHeight, _renderCancellation);
        }
        finally
        {
            _updateSemaphore.Release();
        }
    }

    public override void Render(DrawingContext context)
    {
        context.DrawImage(_rt,
            new Rect(0, 0, _rt.PixelSize.Width, _rt.PixelSize.Height),
            new Rect(0, 0, Bounds.Width, Bounds.Height));
    }

    public async Task RenderMap(int width, int height, CancellationTokenSource token)
    {
        var center = _centerPoint;

        int zoomNumTiles = MapHelper.GetNumberOfTilesAtZoom(_zoom);
        var centerTilePoint = MapHelper.WorldToTilePos(center, _zoom);

        // xStart = floor(center.X - width / 512)
        // yStart = floor(center.Y - height / 512)
        // xNum = celling((width + offset) / 256)
        // xNum = celling((height + offset) / 256)
        // xOffset = mod(center.X - width / 512, 1) * 256
        // yOffset = mod(center.Y - height / 512, 1) * 256

        double xs = centerTilePoint.X - width / 512.0;
        double ys = centerTilePoint.Y - height / 512.0;

        var (xStart, xOffset) = ((int)Math.Floor(xs), (int)(xs % 1 * 256.0));
        var (yStart, yOffset) = ((int)Math.Floor(ys), (int)(ys % 1 * 256.0));
        int xNum = (int)Math.Ceiling((width + xOffset) / 256.0);
        int yNum = (int)Math.Ceiling((height + yOffset) / 256.0);

        List<Task> getImageTask = new();
        for (int x = 0; x < xNum; x++)
        {
            for (int y = 0; y < yNum; y++)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                int xTile = x + xStart;
                int yTile = y + yStart;
                int y1 = y;
                int x1 = x;
                getImageTask.Add(Task.Run(async () =>
                {
                    int xPos = x1 * 256 - xOffset;
                    int yPos = y1 * 256 - yOffset;
                    int xIndex = xTile.Wrap(zoomNumTiles);
                    int yIndex = yTile.Wrap(zoomNumTiles);
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (!MapCache.TryGetImage(xIndex, yIndex, _zoom, out var image))
                        {
                            _skContext.SkCanvas.DrawRect(xPos, yPos, 256, 256, _whitePaint);
                        }
                        else
                        {
                            _skContext.SkCanvas.DrawImage(image, xPos, yPos);
                        }
                    });

                    try
                    {
                        // For some reason may on dictionary duplicate key?
                        InvalidateVisual();
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }));
            }
        }

        await Task.WhenAll(getImageTask);
    }
}