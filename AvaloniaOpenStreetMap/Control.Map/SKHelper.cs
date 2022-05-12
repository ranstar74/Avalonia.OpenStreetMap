using System.IO;
using SkiaSharp;

namespace AvaloniaOpenStreetMap.Control.Map;

public static class SkHelper
{
    public static SKImage ToSkImage(Stream stream)
    {
        var st = new SKManagedStream(stream);
        var codec = SKCodec.Create(st);
        var bitmap = new SKBitmap(codec.Info);
        
        var res = codec.GetPixels(bitmap.Info, bitmap.GetPixels());
        
        var image = SKImage.FromBitmap(bitmap);

        return image;
    }
}