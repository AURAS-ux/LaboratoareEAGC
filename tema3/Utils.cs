namespace tema3;

public static class Utils
{
    public static float NormalizeNumber(float number, int max, int min, float a = -1, float b = 1)
    {
        return (b - a) * ((number - min) / (max - min)) + a;
    }

    public static (float, float) GetXminYmin(List<float[]> points)
    {
        var xmin = float.MaxValue;
        var ymin = float.MaxValue;
        foreach (var point in points)
        {
            if (point[0] < xmin)
                xmin = point[0];
            if (point[1] < ymin)
                ymin = point[1];
        }

        return (xmin, ymin);
    }

    public static (float, float) GetXmaxYmax(List<float[]> points)
    {
        var xmax = float.MinValue;
        var ymax = float.MinValue;
        foreach (var point in points)
        {
            if (point[0] > xmax)
                xmax = point[0];
            if (point[1] > ymax)
                ymax = point[1];
        }

        return (xmax, ymax);
    }
}