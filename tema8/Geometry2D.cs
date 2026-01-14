using System.Numerics;

namespace tema8;

public static class Geometry2D
{
    private const float Epsilon = 1e-6f;

    public static float F(Vector2 a, Vector2 b, Vector2 c)
    {
        return (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
    }

    public static bool AreOnOppositeSides(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        var f1 = F(c, d, a);
        var f2 = F(c, d, b);
        return f1 * f2 < 0.0f;
    }

    public static bool AreVerticesOfConvexQuadrilateral(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        var pts = new[] { a, b, c, d };

        if (HasDuplicatePoints(pts))
            return false;

        if (SegmentsIntersect(a, b, c, d, true) || SegmentsIntersect(b, c, d, a, true))
            return false;

        var firstSign = 0;

        for (var i = 0; i < 4; i++)
        {
            var cross = F(pts[i], pts[(i + 1) % 4], pts[(i + 2) % 4]);
            var sign = SignWithEpsilon(cross);
            if (sign == 0)
                return false;

            if (firstSign == 0)
                firstSign = sign;
            else if (sign != firstSign)
                return false;
        }

        return true;
    }

    public static bool SegmentsIntersectStrictlyInside(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        if (IsPointOnSegment(a, c, d) || IsPointOnSegment(b, c, d) || IsPointOnSegment(c, a, b) ||
            IsPointOnSegment(d, a, b))
            return false;

        var o1 = F(a, b, c);
        var o2 = F(a, b, d);
        var o3 = F(c, d, a);
        var o4 = F(c, d, b);

        return o1 * o2 < 0.0f && o3 * o4 < 0.0f;
    }

    public static bool IsPointInsidePolygon(IReadOnlyList<Vector2> polygon, Vector2 point)
    {
        var inside = false;

        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            var pi = polygon[i];
            var pj = polygon[j];

            if (IsPointOnSegment(point, pj, pi))
                return true;

            var intersects = (pi.Y > point.Y) != (pj.Y > point.Y) &&
                             point.X <
                             (pj.X - pi.X) * (point.Y - pi.Y) / (pj.Y - pi.Y) + pi.X;

            if (intersects)
                inside = !inside;
        }

        return inside;
    }

    public static bool SegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2, bool includeEndpoints)
    {
        var o1 = SignWithEpsilon(F(p1, p2, q1));
        var o2 = SignWithEpsilon(F(p1, p2, q2));
        var o3 = SignWithEpsilon(F(q1, q2, p1));
        var o4 = SignWithEpsilon(F(q1, q2, p2));

        var proper = o1 * o2 < 0 && o3 * o4 < 0;
        if (proper)
            return true;

        if (!includeEndpoints)
            return false;

        return o1 == 0 && IsPointOnSegment(q1, p1, p2) ||
               o2 == 0 && IsPointOnSegment(q2, p1, p2) ||
               o3 == 0 && IsPointOnSegment(p1, q1, q2) ||
               o4 == 0 && IsPointOnSegment(p2, q1, q2);
    }

    public static bool IsPointOnSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        var cross = F(a, b, p);
        if (MathF.Abs(cross) > Epsilon)
            return false;

        var dot = Vector2.Dot(p - a, b - a);
        if (dot < -Epsilon)
            return false;

        var squaredLen = Vector2.DistanceSquared(a, b);
        return dot <= squaredLen + Epsilon;
    }

    private static int SignWithEpsilon(float value)
    {
        if (MathF.Abs(value) < Epsilon)
            return 0;
        return value > 0 ? 1 : -1;
    }

    private static bool HasDuplicatePoints(IReadOnlyList<Vector2> pts)
    {
        for (var i = 0; i < pts.Count; i++)
        for (var j = i + 1; j < pts.Count; j++)
            if (Vector2.DistanceSquared(pts[i], pts[j]) < Epsilon)
                return true;

        return false;
    }
}
