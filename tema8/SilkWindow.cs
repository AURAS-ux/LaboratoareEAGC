using System.Drawing;
using System.Numerics;
using Serilog;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace tema8;

public class SilkWindow
{
    private static IWindow? _window;
    private static GL _gl = null!;
    private IInputContext _input = null!;

    private readonly ILogger _logger;
    private readonly WindowOptions _options;

    private List<float[]> _points = new();
    private readonly List<List<float[]>> _bezierSegments = new();
    private RenderMode _renderMode;
    private uint _shaderProgram;
    private int _colorUniformLocation;

    private uint _vao;
    private uint _vbo;
    private uint _vaoInside;
    private uint _vaoOutside;
    private uint _vboInside;
    private uint _vboOutside;

    private readonly List<float[]> _insidePoints = new();
    private readonly List<float[]> _outsidePoints = new();

    private static readonly Vector3 DefaultColor = new(0f, 237f / 255f, 4f / 255f);
    private static readonly Vector3 InsideColor = new(0.0f, 1.0f, 0.0f);
    private static readonly Vector3 OutsideColor = new(1.0f, 0.0f, 0.0f);

    private static readonly Vector3[] SegmentColors =
    {
        new(0.94f, 0.33f, 0.31f),
        new(0.23f, 0.70f, 0.96f),
        new(0.98f, 0.76f, 0.19f),
        new(0.56f, 0.35f, 0.92f),
        new(0.11f, 0.80f, 0.55f),
        new(0.95f, 0.58f, 0.77f)
    };

    public SilkWindow()
    {
        _options = WindowOptions.Default;
        _options.Title = "Silk - Point Example";
        _options.Size = WindowSize;
        _logger = Log.ForContext<SilkWindow>();
    }

    public static Vector2D<int> WindowSize { get; } = new(1280, 720);

    public void Start()
    {
        _window = Window.Create(_options);

        _window.Load += OnLoad;
        _window.Render += OnRender;
        _window.Update += OnUpdate;

        _window.Run();
    }

    private void OnUpdate(double dt)
    {
        // handle input or physics here if needed
    }

    private void AddNewPoint(Vector2 pos)
    {
        var normalizedX = Utils.NormalizeNumber(pos.X, WindowSize.X, 0);
        var normalizedY = Utils.NormalizeNumber(pos.Y, WindowSize.Y, 0);
        Console.WriteLine($"Normalized coordinates: {normalizedX}, {normalizedY}");
        _points.Add([normalizedX, -normalizedY, 0.0f]);
        _bezierSegments.Clear();
        ClearClassification();
    }

    private void OnRender(double dt)
    {
        _gl!.Clear(ClearBufferMask.ColorBufferBit);

        if (_renderMode == RenderMode.Lines && _bezierSegments.Count > 0)
        {
            _gl.BindVertexArray(_vao);
            DrawBezierSegments();
            return;
        }

        _gl.BindVertexArray(_vao);

        SetColor(DefaultColor);

        switch (_renderMode)
        {
            case RenderMode.Points:
                _gl.PointSize(10.0f);
                _gl.DrawArrays(PrimitiveType.Points, 0, (uint)_points.Count);
                break;
            case RenderMode.Lines:
                if (_points.Count >= 2) _gl.DrawArrays(PrimitiveType.LineStrip, 0, (uint)_points.Count);

                break;
            case RenderMode.Polygon:
                if (_points.Count >= 3) _gl.DrawArrays(PrimitiveType.TriangleFan, 0, (uint)_points.Count);

                break;
        }

        DrawClassificationPoints();
    }

    private unsafe void OnLoad()
    {
        _gl = _window!.CreateOpenGL();
        _logger.Information("Window loaded-OpenGL context created.");

        _input = _window!.CreateInput();

        foreach (var inputMouse in _input.Mice) inputMouse.MouseDown += OnInputMouseOnMouseDown;

        foreach (var inputKeyboard in _input.Keyboards)
        {
            inputKeyboard.KeyDown += HandleKeyDown;
            inputKeyboard.KeyUp += HandleKeyUp;
        }

        _gl.ClearColor(Color.CornflowerBlue);

        _vao = _gl.GenVertexArray();
        _vbo = _gl.GenBuffer();
        _vaoInside = _gl.GenVertexArray();
        _vaoOutside = _gl.GenVertexArray();
        _vboInside = _gl.GenBuffer();
        _vboOutside = _gl.GenBuffer();

        ConfigureVertexArray(_vao, _vbo);
        ConfigureVertexArray(_vaoInside, _vboInside);
        ConfigureVertexArray(_vaoOutside, _vboOutside);

        // 2. Create and compile shaders
        var vertexShader = CreateAndCompileShader(out var fragmentShader);

        // 3. Link shaders to program
        LinkShadersToProgram(vertexShader, fragmentShader);
        _gl.UseProgram(_shaderProgram);

        _colorUniformLocation = _gl.GetUniformLocation(_shaderProgram, "uColor");
        if (_colorUniformLocation == -1)
            _logger.Warning("Failed to locate uColor uniform. Segment coloring will not be applied.");
        else
            SetColor(DefaultColor);

        UpdateVertexBuffer();
        ClearClassification();

        // Clean up shader objects (they're now linked into program)
        CleanShaderObjects(vertexShader, fragmentShader);
    }

    private void HandleKeyUp(IKeyboard keyboard, Key keyRaised, int arg3)
    {
    }

    private void HandleKeyDown(IKeyboard keyboard, Key keyPressed, int arg3)
    {
        if (keyPressed == Key.Escape)
        {
            _logger.Information("Escape key pressed. Closing window.");
            _window?.Close();
        }

        if (keyPressed == Key.Space)
        {
            _logger.Information("Space key pressed. Clearing all points.");
            _points.Clear();
            _bezierSegments.Clear();
            ClearClassification();
            UpdateVertexBuffer();
        }

        if (keyPressed == Key.F)
        {
            ReportOrientation();
        }

        if (keyPressed == Key.O)
        {
            ReportOppositeSides();
        }

        if (keyPressed == Key.V)
        {
            ReportConvexQuadrilateral();
        }

        if (keyPressed == Key.I)
        {
            ReportSegmentIntersection();
        }

        if (keyPressed == Key.P)
        {
            ClassifyWholeWindow();
        }
    }

    private void OnInputMouseOnMouseDown(IMouse mouse, MouseButton button)
    {
        if (button == MouseButton.Left)
        {
            var pos = mouse.Position;
            _logger.Information($"Left mouse button clicked at: {pos.X}, {pos.Y}");
            AddNewPoint(pos);
            UpdateVertexBuffer();
        }

        if (button == MouseButton.Right)
        {
            _logger.Information($"Right mouse button clicked. Changing render mode for {_points.Count} points.");
            _renderMode = _renderMode switch
            {
                RenderMode.Points => RenderMode.Lines,
                RenderMode.Lines => RenderMode.Polygon,
                _ => RenderMode.Points
            };

            _logger.Information($"Render mode changed to: {_renderMode}");
        }

        if (button == MouseButton.Middle)
        {
            if (_points.Count < 2)
            {
                _logger.Warning("At least two control points are required to build a Bezier curve.");
                return;
            }

            _logger.Information("Middle mouse button clicked. Generating Bezier curve.");
            var bezierCurve = BezierHelper.BuildCurve(_points, 128);

            if (bezierCurve.CurvePoints.Count == 0)
            {
                _logger.Warning("Bezier curve generation failed. Please check the control points.");
                _bezierSegments.Clear();
                return;
            }

            _renderMode = RenderMode.Lines;
            _points = bezierCurve.CurvePoints;
            _bezierSegments.Clear();
            _bezierSegments.AddRange(bezierCurve.Segments);
            ClearClassification();
            _logger.Information(
                $"Bezier curve generated with {_points.Count} sampled points across {_bezierSegments.Count} segments. Updating vertex buffer.");
            UpdateVertexBuffer();
        }
    }

    private void UpdateVertexBuffer()
    {
        UploadPointsToBuffer(_vbo, _points);
    }

    private static void CleanShaderObjects(uint vertexShader, uint fragmentShader)
    {
        _gl.DeleteShader(vertexShader);
        _gl.DeleteShader(fragmentShader);
    }

    private void LinkShadersToProgram(uint vertexShader, uint fragmentShader)
    {
        int success;
        _shaderProgram = _gl.CreateProgram();
        _gl.AttachShader(_shaderProgram, vertexShader);
        _gl.AttachShader(_shaderProgram, fragmentShader);
        _gl.LinkProgram(_shaderProgram);
        _gl.GetProgram(_shaderProgram, GLEnum.LinkStatus, out success);
        if (success == 0)
            throw new Exception($"Program link error: {_gl.GetProgramInfoLog(_shaderProgram)}");
    }

    private static uint CreateAndCompileShader(out uint fragmentShader)
    {
        var vertexShaderSource = LoadShaderSource("Shaders/vertex_shader.glsl");

        var fragmentShaderSource = LoadShaderSource("Shaders/fragment_shader.glsl");

        var vertexShader = _gl.CreateShader(ShaderType.VertexShader);
        _gl.ShaderSource(vertexShader, vertexShaderSource);
        _gl.CompileShader(vertexShader);
        _gl.GetShader(vertexShader, ShaderParameterName.CompileStatus, out var success);
        if (success == 0)
            throw new Exception($"Vertex shader error: {_gl.GetShaderInfoLog(vertexShader)}");

        fragmentShader = _gl.CreateShader(ShaderType.FragmentShader);
        _gl.ShaderSource(fragmentShader, fragmentShaderSource);
        _gl.CompileShader(fragmentShader);
        _gl.GetShader(fragmentShader, ShaderParameterName.CompileStatus, out success);
        if (success == 0)
            throw new Exception($"Fragment shader error: {_gl.GetShaderInfoLog(fragmentShader)}");
        return vertexShader;
    }

    private static string LoadShaderSource(string path)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch (Exception e)
        {
            throw new Exception($"Error loading shader source: {path}", e);
        }
    }

    private void SetColor(Vector3 color)
    {
        if (_colorUniformLocation != -1)
            _gl!.Uniform3(_colorUniformLocation, color.X, color.Y, color.Z);
    }

    private static Vector2 ToVector2(IReadOnlyList<float> p) => new(p[0], p[1]);

    private static string FormatPoint(Vector2 p) => $"({p.X:F3}, {p.Y:F3})";

    private unsafe void ConfigureVertexArray(uint vao, uint vbo)
    {
        _gl!.BindVertexArray(vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), null);
        _gl.EnableVertexAttribArray(0);
    }

    private unsafe void UploadPointsToBuffer(uint bufferId, List<float[]> points)
    {
        var allPoints = points.SelectMany(p => p).ToArray();
        var size = (nuint)(allPoints.Length * sizeof(float));

        _gl!.BindBuffer(BufferTargetARB.ArrayBuffer, bufferId);

        void* ptr;
        fixed (void* dataPtr = allPoints)
        {
            ptr = allPoints.Length == 0 ? null : dataPtr;
            _gl.BufferData(BufferTargetARB.ArrayBuffer, size, ptr, BufferUsageARB.DynamicDraw);
        }
    }

    private void ReportOrientation()
    {
        if (_points.Count < 3)
        {
            _logger.Warning("Need at least 3 points to compute F(A, B, C).");
            return;
        }

        var a = ToVector2(_points[0]);
        var b = ToVector2(_points[1]);
        var c = ToVector2(_points[2]);

        var value = Geometry2D.F(a, b, c);
        _logger.Information($"F(A, B, C) = {value:F6} for A={FormatPoint(a)}, B={FormatPoint(b)}, C={FormatPoint(c)}");
    }

    private void ReportOppositeSides()
    {
        if (_points.Count < 4)
        {
            _logger.Warning("Need at least 4 points to test if A and B are on opposite sides of CD.");
            return;
        }

        var a = ToVector2(_points[0]);
        var b = ToVector2(_points[1]);
        var c = ToVector2(_points[2]);
        var d = ToVector2(_points[3]);

        var result = Geometry2D.AreOnOppositeSides(a, b, c, d);
        _logger.Information(
            $"A={FormatPoint(a)} and B={FormatPoint(b)} {(result ? "are" : "are not")} on opposite sides of segment CD (C={FormatPoint(c)}, D={FormatPoint(d)}).");
    }

    private void ReportConvexQuadrilateral()
    {
        if (_points.Count < 4)
        {
            _logger.Warning("Need at least 4 points to test convex quadrilateral.");
            return;
        }

        var a = ToVector2(_points[0]);
        var b = ToVector2(_points[1]);
        var c = ToVector2(_points[2]);
        var d = ToVector2(_points[3]);

        var result = Geometry2D.AreVerticesOfConvexQuadrilateral(a, b, c, d);
        _logger.Information(
            $"Points A={FormatPoint(a)}, B={FormatPoint(b)}, C={FormatPoint(c)}, D={FormatPoint(d)} {(result ? "form" : "do not form")} a convex quadrilateral (given in order).");
    }

    private void ReportSegmentIntersection()
    {
        if (_points.Count < 4)
        {
            _logger.Warning("Need at least 4 points to test intersection between AB and CD.");
            return;
        }

        var a = ToVector2(_points[0]);
        var b = ToVector2(_points[1]);
        var c = ToVector2(_points[2]);
        var d = ToVector2(_points[3]);

        var result = Geometry2D.SegmentsIntersectStrictlyInside(a, b, c, d);
        _logger.Information(
            $"Segments AB (A={FormatPoint(a)}, B={FormatPoint(b)}) and CD (C={FormatPoint(c)}, D={FormatPoint(d)}) {(result ? "intersect" : "do not intersect")} in their interior.");
    }

    private void ClassifyWholeWindow()
    {
        if (_points.Count < 3)
        {
            _logger.Warning("Draw at least three vertices before classifying points inside/outside the polygon.");
            return;
        }

        var polygon = _points.Select(ToVector2).ToList();
        _insidePoints.Clear();
        _outsidePoints.Clear();

        for (var y = 0; y < WindowSize.Y; y++)
        for (var x = 0; x < WindowSize.X; x++)
        {
            var nx = Utils.NormalizeNumber(x, WindowSize.X, 0);
            var ny = -Utils.NormalizeNumber(y, WindowSize.Y, 0);
            var p = new Vector2(nx, ny);

            if (Geometry2D.IsPointInsidePolygon(polygon, p))
                _insidePoints.Add([nx, ny, 0.0f]);
            else
                _outsidePoints.Add([nx, ny, 0.0f]);
        }

        UploadPointsToBuffer(_vboInside, _insidePoints);
        UploadPointsToBuffer(_vboOutside, _outsidePoints);

        _renderMode = RenderMode.Polygon;

        var total = WindowSize.X * WindowSize.Y;
        _logger.Information(
            $"Point-in-polygon classification done: {_insidePoints.Count} inside, {_outsidePoints.Count} outside (total sampled {total}).");
    }

    private void DrawClassificationPoints()
    {
        if (_insidePoints.Count == 0 && _outsidePoints.Count == 0)
            return;

        _gl.PointSize(1.0f);

        if (_outsidePoints.Count > 0)
        {
            _gl.BindVertexArray(_vaoOutside);
            SetColor(OutsideColor);
            _gl.DrawArrays(PrimitiveType.Points, 0, (uint)_outsidePoints.Count);
        }

        if (_insidePoints.Count > 0)
        {
            _gl.BindVertexArray(_vaoInside);
            SetColor(InsideColor);
            _gl.DrawArrays(PrimitiveType.Points, 0, (uint)_insidePoints.Count);
        }
    }

    private void ClearClassification()
    {
        _insidePoints.Clear();
        _outsidePoints.Clear();

        if (_gl == null)
            return;

        UploadPointsToBuffer(_vboInside, _insidePoints);
        UploadPointsToBuffer(_vboOutside, _outsidePoints);
    }

    private Vector3 GetSegmentColor(int index) => SegmentColors[index % SegmentColors.Length];

    private unsafe void DrawBezierSegments()
    {
        for (var i = 0; i < _bezierSegments.Count; i++)
        {
            var segment = _bezierSegments[i];
            if (segment.Count == 0)
                continue;

            SetColor(GetSegmentColor(i));

            var allPoints = segment.SelectMany(p => p).ToArray();

            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
            fixed (void* v = allPoints)
            {
                _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(allPoints.Length * sizeof(float)), v,
                    BufferUsageARB.DynamicDraw);
            }

            _gl.DrawArrays(PrimitiveType.LineStrip, 0, (uint)segment.Count);
        }
    }
}
