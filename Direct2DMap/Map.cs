using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation.Animators;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Skia;
using Avalonia.Threading;
using MapRender;
using SkiaSharp;
using Bitmap = Avalonia.Media.Imaging.Bitmap;
using Image = Avalonia.Controls.Image;
using Point = Avalonia.Point;

namespace Direct2DMap;

public class Map : Control
{
    private RenderTargetBitmap RenderTarget;
    private ISkiaDrawingContextImpl SkiaContext;
    private SKPaint SKBrush;

    private static int _downloadCounter;
    private static readonly DirectoryInfo _cacheDir = new("Cache");
    private static readonly Dictionary<string, SKImage> CachedImages = new();

    private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private MapPoint mapPoint = MapPoint.Zero;
    private int zoom = 4;

    public Map()
    {
        _cacheDir.Create();
        foreach (var fileName in Directory.GetFiles(_cacheDir.FullName))
        {
            CachedImages.Add(Path.GetFileName(fileName), SKImage.FromBitmap(SKBitmap.Decode(File.OpenRead(fileName))));
        }

        SKBrush = new SKPaint();
        SKBrush.IsAntialias = true;
        SKBrush.Color = new SKColor(0, 0, 0);
        SKBrush.Shader = SKShader.CreateColor(SKBrush.Color);

        BoundsProperty.Changed.AddClassHandler<Map>(async (x, args) =>
        {
            if (Bounds.Width == 0 || Bounds.Height == 0)
                return;

            RenderTarget =
                new RenderTargetBitmap(new PixelSize((int)Bounds.Width, (int)Bounds.Height), new Vector(96, 96));

            var context = RenderTarget.CreateDrawingContext(null);
            SkiaContext = (context as ISkiaDrawingContextImpl);
            SkiaContext.SkCanvas.Clear(new SKColor(255, 255, 255));

            await RenderMap();
        });

        PointerPressed += DrawingCanvas_PointerPressed;
        PointerMoved += OnPointerMoved;
        
        PointerReleased += OnPointerReleased;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        isdrag = false;
    }

    private async void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!isdrag)
            return;

        var pos = Convert(e.GetPosition(this));

        var mapPos = MapHelper.WorldToTilePos(mapPoint, zoom);

        var newMapPos = new MapTilePoint(
            mapPos.X - (pos.X - prevMousePos.X),
            mapPos.Y - (pos.Y - prevMousePos.Y));

        mapPoint = MapHelper.TileToWorldPos(newMapPos, zoom);

        prevMousePos = pos;
        
        await RenderMap();

    }

    private bool isdrag = false;
    private MapTilePoint prevMousePos;
    private async void DrawingCanvas_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        isdrag = true;
        prevMousePos = Convert(e.GetPosition(this));
        
        //await RenderMap();
    }

    private MapTilePoint Convert(Point mousepos)
    {
        var center = mapBounds.Center;
        var mouseX = Remap(mousepos.X, 0, Bounds.Width, mapBounds.Left, mapBounds.Right);
        var mouseY = Remap(mousepos.Y, 0, Bounds.Height, mapBounds.Top, mapBounds.Bottom);
        
        return new MapTilePoint(mouseX, mouseY);
    }

    public static double Remap(double value, double from1, double to1, double from2, double to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }

    private Rect mapBounds;

    private async Task RenderMap()
    {
        await _semaphore.WaitAsync();
        try
        {
            await RenderMap((int)Bounds.Width, (int)Bounds.Height);
            InvalidateVisual();
        }
        finally
        {
            _semaphore.Release();
        }

        //var p = e.GetPosition(this);
        //SkiaContext?.SkCanvas.DrawCircle((float)p.X, (float)p.Y, 25, SKBrush);
    }

    public override void Render(DrawingContext context)
    {
        context.DrawImage(RenderTarget,
            new Rect(0, 0, RenderTarget.PixelSize.Width, RenderTarget.PixelSize.Height),
            new Rect(0, 0, Bounds.Width, Bounds.Height));
    }

    public async Task RenderMap(int width, int height)
    {
        await Dispatcher.UIThread.InvokeAsync((() => { SkiaContext.SkCanvas.Clear(new SKColor(255, 255, 255)); }));
        var center = mapPoint;

        int zoomNumTiles = MapHelper.GetNumberOfTilesAtZoom(zoom);
        var centerTilePoint = MapHelper.WorldToTilePos(center, zoom);

        // xStart = floor(center.X - width / 512)
        // yStart = floor(center.Y - height / 512)
        // xNum = width / 256 + 1
        // yNum = height / 256 + 1
        // xOffset = mod(center.X - width / 512, 1) * 256
        // yOffset = mod(center.Y - height / 512, 1) * 256

        double xs = centerTilePoint.X - width / 512.0;
        double ys = centerTilePoint.Y - height / 512.0;

        var (xStart, xOffset) = ((int)Math.Floor(xs), (int)(xs % 1 * 256.0));
        var (yStart, yOffset) = ((int)Math.Floor(ys), (int)(ys % 1 * 256.0));
        int xNum = (int)(width / 256.0) + 1;
        int yNum = (int)(height / 256.0) + 1;

        mapBounds = new Rect(xs, ys, width / 256.0, height / 256.0);

        List<Task> getImageTask = new();
        for (int x = 0; x < xNum; x++)
        {
            for (int y = 0; y < yNum; y++)
            {
                int xTile = x + xStart;
                int yTile = y + yStart;
                int y1 = y;
                int x1 = x;
                getImageTask.Add(Task.Run(async () =>
                {
                    var image = await GetTileImage(
                        Wrap(xTile, zoomNumTiles),
                        Wrap(yTile, zoomNumTiles), zoom);

                    // lock (graphicsLock)
                    // {
                    //     // graphics.DrawImage(image, new Point(
                    //     //     x1 * 256 - xOffset,
                    //     //     y1 * 256 - yOffset));
                    // }
                    await Dispatcher.UIThread.InvokeAsync((() =>
                    {
                        SkiaContext.SkCanvas.DrawImage(image, x1 * 256 - xOffset, y1 * 256 - yOffset);
                    }));
                }));
            }
        }

        await Task.WhenAll(getImageTask);

        // Export
        //await using var fs = File.Create(@"C:\Users\falco\Desktop\Map.png");
        //bitmap.Save(fs, ImageFormat.Png);
    }

    private static int Wrap(int value, int by)
    {
        value %= by;
        if (value < 0)
            value += by;
        return value;
    }

    private static readonly HttpClient HttpClient = new();

    private static async Task<SKImage> GetTileImage(int x, int y, int zoom)
    {
        string name = $"{x}_{y}_{zoom}.png";
        string fileName = $"{_cacheDir.FullName}/{name}";

        Stream stream;
        if (CachedImages.ContainsKey(name))
        {
            return CachedImages[name];
        }

        _downloadCounter++;
        // We have to set user agent because otherwise we will get 403 http error
        // https://stackoverflow.com/questions/46604840/403-response-with-httpclient-but-not-with-browser
        var url = $"https://tile.openstreetmap.org/{zoom}/{x}/{y}.png";
        var request = new HttpRequestMessage(HttpMethod.Get, url)
        {
            Headers =
            {
                {
                    "User-Agent",
                    "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.11 (KHTML, like Gecko) " +
                    "Chrome/23.0.1271.95 Safari/537.11"
                }
            }
        };
        var result = await HttpClient.SendAsync(request);
        byte[] imgByte = await result.Content.ReadAsByteArrayAsync();

        stream = new MemoryStream(imgByte);

        // Save to disk
        await using var fs = File.Create(fileName);
        await stream.CopyToAsync(fs);

        try
        {
            // var image = SKImage.FromBitmap(SKBitmap.Decode(stream));
            var st = new SKMemoryStream(imgByte);

            // create the codec
            var codec = SKCodec.Create(st);

            // we need a place to store the bytes
            var bitmap = new SKBitmap(codec.Info);

            // decode!
            // result should be SKCodecResult.Success, but you may get more information
            var res = codec.GetPixels(bitmap.Info, bitmap.GetPixels());

            var image = SKImage.FromBitmap(bitmap);
            CachedImages.Add(name, image);

            return image;
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            return SKImage.Create(new SKImageInfo(256, 256));
        }
    }


    // For smooth transition - Interpolate between images, or maybe using some avalonia stock
    // stuff for that 
}