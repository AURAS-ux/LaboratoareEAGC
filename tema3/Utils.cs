using Silk.NET.Maths;

namespace tema3;

public static class Utils
{
    public static float NormalizeNumber(float number,int max,int min, float a=-1, float b=1)
    {
        return (b - a)*((number - min) / (max - min)) + a;
    }
    
    public static (double,double) GetAngleForTwoPoints(Vector2D<float> point1, Vector2D<float> point2)
    {
        float deltaY = point2.Y - point1.Y;
        float deltaX = point2.X - point1.X;
        double angleInRadians = Math.Atan2(deltaY, deltaX);
        double angleInDegrees = angleInRadians * (180.0 / Math.PI);
        return (angleInRadians, angleInDegrees);
    }
}