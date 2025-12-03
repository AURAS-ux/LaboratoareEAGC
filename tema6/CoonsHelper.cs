using System.Numerics;

namespace tema6;

public record CoonsCurve(List<float[]> CurvePoints, List<List<float[]>> Segments);

public static class CoonsHelper
{
    public static CoonsCurve BuildCurve(List<float[]> controlPoints, int samplesPerSegment = 64)
    {
        var curvePoints = new List<float[]>();
        var segments = new List<List<float[]>>();

        if (controlPoints.Count < 4 || controlPoints.Count % 2 != 0)
            return new CoonsCurve(curvePoints, segments);

        for (var i = 0; i + 3 < controlPoints.Count; i += 2)
        {
            var a = ToVec2(controlPoints[i]);
            var aPrime = ToVec2(controlPoints[i + 1]);
            var b = ToVec2(controlPoints[i + 2]);
            var bPrime = ToVec2(controlPoints[i + 3]);

            var segmentPoints = GenerateSegment(a, aPrime, b, bPrime, samplesPerSegment);
            segments.Add(segmentPoints);

            var pointsForMergedCurve = new List<float[]>(segmentPoints);
            if (i > 0 && pointsForMergedCurve.Count > 0)
                pointsForMergedCurve.RemoveAt(0);

            curvePoints.AddRange(pointsForMergedCurve);
        }

        return new CoonsCurve(curvePoints, segments);
    }

    private static List<float[]> GenerateSegment(Vector2 a, Vector2 aPrime, Vector2 b, Vector2 bPrime,
        int samplesPerSegment)
    {
        var result = new List<float[]>();

        var aTangent = aPrime - a;
        var bTangent = bPrime - b;

        for (var i = 0; i <= samplesPerSegment; i++)
        {
            var u = i / (float)samplesPerSegment;
            var point = EvaluateCoons(a, aTangent, b, bTangent, u);
            result.Add(new[] { point.X, point.Y, 0.0f });
        }

        return result;
    }

    private static Vector2 EvaluateCoons(Vector2 a, Vector2 aTangent, Vector2 b, Vector2 bTangent, float u)
    {
        var u2 = u * u;
        var u3 = u2 * u;

        var h00 = 2 * u3 - 3 * u2 + 1;
        var h10 = u3 - 2 * u2 + u;
        var h01 = -2 * u3 + 3 * u2;
        var h11 = u3 - u2;

        return h00 * a + h10 * aTangent + h01 * b + h11 * bTangent;
    }

    private static Vector2 ToVec2(IReadOnlyList<float> p) => new(p[0], p[1]);
}
