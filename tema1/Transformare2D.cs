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

    private static void MirrorPoints(Func<float[], float[]> transform, bool append, List<float[]> points)
    {
        var mirrored = points.Select(p => transform(p)).ToList();
        if (append)
            points.AddRange(mirrored);
        else
        {
            points.Clear();
            points.AddRange(mirrored);
        }
    }
    
    public static void MirrorAcrossOX(List<float[]> points, bool append)
    {
        MirrorPoints(p => new float[]{p[0], -p[1], p[2]}, append, points);
    }
    
    public static void MirrorAcrossOY(List<float[]> points, bool append)
    {
        MirrorPoints(p => new float[]{-p[0], p[1], p[2]}, append, points);
    }
    
    public static void MirrorAcrossOrigin(List<float[]> points, bool append)
    {
        MirrorPoints(p => new float[]{-p[0], -p[1], p[2]}, append, points);
    }
}