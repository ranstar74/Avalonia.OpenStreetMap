namespace Direct2DMap.Control.Map;

public static class DoubleExtensions
{
    public static int Wrap(this int value, int by)
    {
        value %= by;
        if (value < 0)
            value += by;
        return value;
    }
    
    
    public static double Remap(this double value, double from1, double to1, double from2, double to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }
}