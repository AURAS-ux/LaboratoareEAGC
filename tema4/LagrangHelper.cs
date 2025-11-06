namespace tema3;

public static class LagrangHelper
{
    public static List<float[]> ComputeLagrangeCurve(List<float[]> points, int numSamples = 100)
    {
        var curvePoints = new List<float[]>();
    
        float xMin = points[0][0];
        float xMax = points[^1][0];
    
        for (int i = 0; i <= numSamples; i++)
        {
            float x = xMin + i * (xMax - xMin) / numSamples;
            float y = EvaluateLagrange(points, x);
        
            curvePoints.Add([x, y,0.0f]);
        }
    
        return curvePoints;
    }

    private static float EvaluateLagrange(List<float[]> points, float x)
    {
        float result = 0;
    
        for (int i = 0; i < points.Count; i++)
        {
            float xi = points[i][0];
            float yi = points[i][1];
        
            float li = 1.0f; // ℓ_i(x)
            for (int j = 0; j < points.Count; j++)
            {
                if (i == j) continue;
                float xj = points[j][0];
                li *= (x - xj) / (xi - xj);
            }
        
            result += yi * li;
        }
    
        return result;
    }
}