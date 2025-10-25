using System.Runtime.InteropServices.ComTypes;
using Serilog;

namespace tema3;

public class CurveHelper
{
    private static readonly ILogger _logger = Log.ForContext<CurveHelper>();
    public static void GenerateCurvePoints(ref List<float[]> points,double lowerBound, double upperBound,Func<double,double> f,Func<double,double> g,int pointsCount=100)
    {
        _logger.Information($"Generating {pointsCount} curve points between bounds {lowerBound} and {upperBound}.");
        List<float[]> unNormalizedPoints = new();
        points.Clear();
        double dn = ((upperBound - lowerBound) / pointsCount);
        _logger.Information($"Delta (dn) calculated as {dn}.");
        //pasul 1 - Pi 
        for(int i = 0;i<=pointsCount;i++)
        {
            double ui = (lowerBound + i * dn);
            double x = f(ui);
            double y = g(ui);
            points.Add([(float)x,(float)y, 0.0f]);
            _logger.Debug($"Point {i}: u={ui}, x={x}, y={y}");
        }
        //pasul 2 - P'i
        var (xmin,ymin) = Utils.GetXminYmin(points);
        _logger.Information($"Minimum x: {xmin}, Minimum y: {ymin}");
        for(int i=0;i<=pointsCount;i++)
        {
            double xpi = points[i][0]-xmin;
            double ypi = points[i][1]-ymin;
            points[i][0] = (float)xpi;
            points[i][1] = (float)ypi;
            _logger.Debug($"Adjusted Point {i}: x'={xpi}, y'={ypi}");
        }
        //pasul 3 - P''i
        var (xmax,ymax) = Utils.GetXmaxYmax(points);
        _logger.Information($"Maximum x: {xmax}, Maximum y: {ymax}");
        double sx = (SilkWindow.WindowSize.X-50) / xmax;
        double sy = (SilkWindow.WindowSize.Y-50) / ymax;
        double s = Math.Min(sx, sy);
        _logger.Information($"Scaling factors - sx: {sx}, sy: {sy}, chosen s: {s}");
        
        for (int i = 0; i <= pointsCount; i++)
        {
            double xsi = points[i][0]* s;
            double ysi = points[i][1]* s;
            unNormalizedPoints.Add([(float)xsi,(float)ysi,0.0f]);
            points[i][0] = Utils.NormalizeNumber((float)xsi, SilkWindow.WindowSize.X, 0);
            points[i][1] = Utils.NormalizeNumber((float)ysi, SilkWindow.WindowSize.Y, 0);
            _logger.Debug($"Scaled Point {i}: x''={points[i][0]}, y''={points[i][1]}");
        }
        
        //pasul 4 - p'''i
        double xsecundMax = s*xmax;
        double ysecundMax = s*ymax;
        double tx = (SilkWindow.WindowSize.X - xsecundMax) / 2;
        double ty = (SilkWindow.WindowSize.Y - ysecundMax) / 2;
        _logger.Information($"Translation values - tx: {tx}, ty: {ty}");
        for (int i = 0; i <= pointsCount; i++)
        {
            double xti = unNormalizedPoints[i][0] + tx;
            double yti = unNormalizedPoints[i][1] + ty;
            unNormalizedPoints.Add([(float)xti,(float)yti,0.0f]);
            points[i][0] = Utils.NormalizeNumber((float)xti, SilkWindow.WindowSize.X, 0);
            points[i][1] = Utils.NormalizeNumber((float)yti, SilkWindow.WindowSize.Y, 0);
            _logger.Debug($"Translated Point {i}: x'''={points[i][0]}, y'''={points[i][1]}");
        }
        
        //pasul 5 - vertical flip
        for (int i = 0; i <= pointsCount; i++)
        {
            points[i][1] = Utils.NormalizeNumber(SilkWindow.WindowSize.Y - unNormalizedPoints[i][1],
                SilkWindow.WindowSize.Y, 0);
            _logger.Debug($"Flipped Point {i}: x_final={points[i][0]}, y_final={points[i][1]}");
        }
        _logger.Information("Generated {Count} curve points.", points.Count);
    }
}