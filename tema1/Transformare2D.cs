using Silk.NET.Maths;

namespace tema1;

public class Transformare2D
{
    public static void PerformMouseScaling(Vector2D<float> iMousePos,Vector2D<float> fMousePos,ref float sX, ref float sY)
    {
        if (iMousePos == fMousePos)
        {
            Console.WriteLine("No scaling performed: Initial and final mouse positions are the same.");
        }
        
        sX = Math.Abs(fMousePos.X / iMousePos.X);
        sY = Math.Abs(fMousePos.Y / iMousePos.Y);
    }
}