using System.Numerics;

namespace tema8;

public record BezierCurve(List<float[]> CurvePoints, List<List<float[]>> Segments);

public static class BezierHelper
{
    public static BezierCurve BuildCurve(List<float[]> controlPoints, int samples = 128)
    {
        var curvePoints = new List<float[]>();
        var segments = new List<List<float[]>>();

        if (controlPoints.Count < 2)
            return new BezierCurve(curvePoints, segments);

        var controlVectors = controlPoints.Select(ToVec2).ToList();
        var segmentPoints = GenerateCurve(controlVectors, samples);

        segments.Add(segmentPoints);
        curvePoints.AddRange(segmentPoints);

        return new BezierCurve(curvePoints, segments);
    }

    private static List<float[]> GenerateCurve(IReadOnlyList<Vector2> controlPoints, int samples)
    {
        var degree = controlPoints.Count - 1;
        var binomial = ComputeBinomialCoefficients(degree);
        var result = new List<float[]>(samples + 1);

        for (var i = 0; i <= samples; i++)
        {
            var u = i / (float)samples;
            var point = Evaluate(controlPoints, binomial, degree, u);
            result.Add([point.X, point.Y, 0.0f]);
        }

        return result;
    }

    private static Vector2 Evaluate(IReadOnlyList<Vector2> controlPoints, IReadOnlyList<float> binomial, int degree,
        float u)
    {
        var oneMinusU = 1.0f - u;
        var sum = Vector2.Zero;

        for (var i = 0; i <= degree; i++)
        {
            var bernstein = binomial[i] * MathF.Pow(u, i) * MathF.Pow(oneMinusU, degree - i);
            sum += bernstein * controlPoints[i];
        }

        return sum;
    }

    private static List<float> ComputeBinomialCoefficients(int n)
    {
        var coefficients = new List<float>(n + 1);
        long current = 1;

        for (var k = 0; k <= n; k++)
        {
            if (k == 0)
            {
                current = 1;
            }
            else
            {
                current = current * (n - k + 1) / k;
            }

            coefficients.Add(current);
        }

        return coefficients;
    }

    private static Vector2 ToVec2(IReadOnlyList<float> p) => new(p[0], p[1]);
}


/*
 - BuildCurve(List<float[]> controlPoints, int samples = 128): Entry point. Validates at least 2
      control points, converts them to Vector2, samples the full Bezier curve with GenerateCurve, and
      returns BezierCurve containing the merged sampled points and a single segment list.
    - GenerateCurve(...): Precomputes binomial coefficients for degree n = controlPoints.Count - 1,
      then for samples + 1 evenly spaced u values in [0,1] evaluates the curve and stores [x, y, 0]
      for OpenGL.
    - Evaluate(...): Core Bernstein evaluation. For each control point P_i, computes B_i^n(u) = C(n,i)
      * u^i * (1-u)^(n-i) and accumulates sum += B_i^n(u) * P_i, yielding the 2D point on the curve at
      parameter u.
    - ComputeBinomialCoefficients(int n): Builds the C(n,k) list iteratively (safe in long then cast
      to float) for reuse during sampling.
    - ToVec2(...): Utility to go from [x, y, z?] float array to a Vector2.

    Overall flow: given control points, the helper samples the Bezier curve defined by those points
    using Bernstein polynomials and returns a list of 3D float vertices ready for rendering as a line
    strip.
    
    */