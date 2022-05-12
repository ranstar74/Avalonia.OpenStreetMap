using System;
using System.Globalization;
using Avalonia.Utilities;

namespace Avalonia.OpenStreetMap.Control.Map;

public class MapPoint
{
    /// <summary>
    /// Gets horizontal (X) coordinate.
    /// </summary>
    public double Longitude { get; }

    /// <summary>
    /// Gets vertical (Y) coordinate.
    /// </summary>
    public double Latitude { get; }

    public static readonly MapPoint Zero = new MapPoint(0.0, 0.0);

    public MapPoint(double lon, double lat)
    {
        Longitude = lon;
        Latitude = lat;
    }
    
        /// <summary>
        /// Parses Map Point from a string.
        /// </summary>
        /// <param name="s">A string with coordinates, for i.e. "50.5923, 84.5921".</param>
        /// <returns>Converted Map Point.</returns>
        /// <exception cref="FormatException">Throws if given string didn't contain valid coordinates.</exception>
        public static MapPoint Parse(string s)
        {
            const string exceptionMessage = "Invalid Coordinates.";
    
            using var tokenizer = new StringTokenizer(s, CultureInfo.InvariantCulture, exceptionMessage);
    
            if (!tokenizer.TryReadDouble(out var lon)) 
                throw new FormatException(exceptionMessage);
            
            if (!tokenizer.TryReadDouble(out var lat)) 
                throw new FormatException(exceptionMessage);
    
            return new MapPoint(lon, lat);
        }
    
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}, {1}", Longitude, Latitude);
        }
}