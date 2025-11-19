namespace tema5;

public static class NewtonHelper
{
    public static float[,] CompunePolinomCoefs(List<float[]> points)
    {
        int n = points.Count;
        float[,] a = new float[n, n];
        float[] x = new float[n];

        for (int i = 0; i < n; i++)
        {
            x[i] = points[i][0];
            a[i, 0] = points[i][1];
        }

        for (int j = 1; j < n; j++)
        {
            for (int i = 0; i < n - j; i++)
            {
                a[i, j] = (a[i + 1, j - 1] - a[i, j - 1]) / (x[i + j] - x[i]);
            }
        }

        return a;
    }
    
    public static float EvaluateNewton(List<float[]> points, float[,] a, float x)
    {
        int n = points.Count;

        float result = a[0, n - 1];

        for (int h = n - 2; h >= 0; h--)
        {
            float x0 = points[h][0];
            result = a[0, h] + (x - x0) * result;
        }

        return result;
    }
    
    public static List<float[]> ComputeNewtonCurve(List<float[]> points, int numSamples = 100)
    {
        var curvePoints = new List<float[]>();

        float[,] ddTable = CompunePolinomCoefs(points);

        float xMin = points[0][0];
        float xMax = points[^1][0];

        for (int i = 0; i <= numSamples; i++)
        {
            float x = xMin + i * (xMax - xMin) / numSamples;
            float y = EvaluateNewton(points, ddTable, x);

            curvePoints.Add(new float[] { x, y, 0.0f });
        }

        return curvePoints;
    }
}
