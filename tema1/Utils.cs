using Silk.NET.Maths;

namespace tema1;

public static class Utils
{
    public static float NormalizeNumber(float number,int max,int min, float a=-1, float b=1)
    {
        return (b - a)*((number - min) / (max - min)) + a;
    }
}