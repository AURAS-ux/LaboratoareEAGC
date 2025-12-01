using System.Numerics;

namespace tema6;

public static class CoonsHelper
{
    /// <summary>
    ///     Generates Coons (cubic Hermite) curve points for each pair of control points.
    ///     Control points are expected in the order:
    ///     P0 (curve point), P1 (tangent handle for P0), P2 (curve point), P3 (tangent handle for P2), ...
    /// </summary>
    /// <param name="controlPoints">Normalized control points as float[x,y,(z)].</param>
    /// <param name="samplesPerSegment">Number of subdivisions per segment (inclusive of endpoints).</param>
    /// <returns>List of 3D float arrays ready to be uploaded to the VBO.</returns>
    public static List<float[]> BuildCurve(List<float[]> controlPoints, int samplesPerSegment = 64)
    {
        var result = new List<float[]>();

        if (controlPoints.Count < 4 || controlPoints.Count % 2 != 0)
            return result;

        for (var i = 0; i + 3 < controlPoints.Count; i += 2)
        {
            var a = ToVec2(controlPoints[i]);
            var aPrime = ToVec2(controlPoints[i + 1]);
            var b = ToVec2(controlPoints[i + 2]);
            var bPrime = ToVec2(controlPoints[i + 3]);

            var segmentPoints = GenerateSegment(a, aPrime, b, bPrime, samplesPerSegment);

            // avoid duplicating the start point of the next segment
            if (i > 0 && segmentPoints.Count > 0)
                segmentPoints.RemoveAt(0);

            result.AddRange(segmentPoints);
        }

        return result;
    }

    /// <summary>
    ///     Generates a single Coons (cubic Hermite) segment between two points with their tangents.
    /// </summary>
    private static List<float[]> GenerateSegment(Vector2 a, Vector2 aPrime, Vector2 b, Vector2 bPrime,
        int samplesPerSegment)
    {
        var result = new List<float[]>();

        // Tangent vectors from handle points
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

    /// <summary>
    ///     Evaluates the Coons (cubic Hermite) curve at parameter u in [0,1].
    /// </summary>
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
