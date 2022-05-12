using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using SkiaSharp;

namespace Avalonia.OpenStreetMap.Control.Map;

public static class MapCache
{
    private static readonly object CacheLock = new();
    private static readonly HttpClient HttpClient = new();
    private static readonly DirectoryInfo CacheDir = new("Cache");
    private static readonly Dictionary<string, SKImage> CachedImages = new();
    private static readonly HashSet<string> PendingDownloads = new();
    private static readonly object PendingDownloadsLock = new();

    public delegate void OnDownloadFinishedHandler();

    public static event OnDownloadFinishedHandler OnDownloadFinished;

    public static bool CacheOnDrive { get; set; } = false;

    static MapCache()
    {
        CacheDir.Create();
        foreach (var fileName in Directory.GetFiles(CacheDir.FullName))
        {
            string name = Path.GetFileName(fileName);
            var image = SkHelper.ToSkImage(File.OpenRead(fileName));

            CachedImages.Add(name, image);
        }
    }

    public static bool TryGetImage(int x, int y, int zoom, out SKImage image)
    {
        image = null;

        string name = GetTileFileName(x, y, zoom);
        lock (CacheLock)
        {
            if (CachedImages.ContainsKey(name))
            {
                image = CachedImages[name];
                return true;
            }
        }

        RequestTile(x, y, zoom);
        return false;
    }
    
    private static async void RequestTile(int x, int y, int zoom)
    {
        string name = GetTileFileName(x, y, zoom);

        lock (PendingDownloadsLock)
        {
            if (PendingDownloads.Contains(name))
                return;

            PendingDownloads.Add(name);
        }

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

        // May throw on downloading and converting to SKImage
        try
        {
            var result = await HttpClient.SendAsync(request);
            var contentStream = await result.Content.ReadAsStreamAsync();

            if (CacheOnDrive)
            {
                // Save to disk
                string fileName = $"{CacheDir.FullName}/{name}";
                await File.WriteAllBytesAsync(fileName, await result.Content.ReadAsByteArrayAsync());
            }

            var image = SkHelper.ToSkImage(contentStream);
            lock (CacheLock)
            {
                CachedImages.Add(name, image);
            }

            OnDownloadFinished?.Invoke();
        }
        catch (Exception)
        {
            // ignored
        }

        lock (PendingDownloadsLock)
        {
            PendingDownloads.Remove(name);
        }
    }
    
    private static string GetTileFileName(int x, int y, int zoom)
    {
        return $"{x}_{y}_{zoom}.png";
    }
}