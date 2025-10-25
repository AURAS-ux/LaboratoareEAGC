using System.Drawing;
using System.Numerics;
using Serilog;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace tema3;

public class SilkWindow
{
    private static IWindow? _window;
    private static GL? _gl;
    private IInputContext _input;

    private readonly ILogger _logger;
    private readonly WindowOptions _options;

    private List<float[]> _points = new();
    private RenderMode _renderMode;
    private uint _shaderProgram;

    private uint _vao;
    private uint _vbo;

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
    }

    private void OnRender(double dt)
    {
        _gl!.Clear(ClearBufferMask.ColorBufferBit);

        _gl.BindVertexArray(_vao);

        switch (_renderMode)
        {
            case RenderMode.Points:
                _gl.PointSize(10.0f);
                _gl.DrawArrays(PrimitiveType.Points, 0, (uint)_points.Count);
                break;
            case RenderMode.Lines:
                if (_points.Count == 2) _gl.DrawArrays(PrimitiveType.LineStrip, 0, (uint)_points.Count);

                break;
            case RenderMode.Polygon:
                if (_points.Count >= 3) _gl.DrawArrays(PrimitiveType.TriangleFan, 0, (uint)_points.Count);

                break;
        }
    }

    private unsafe void OnLoad()
    {
        _gl = _window!.CreateOpenGL();
        _logger.Information("Window loaded-OpenGL context created.");

        _input = _window.CreateInput();

        foreach (var inputMouse in _input.Mice) inputMouse.MouseDown += OnInputMouseOnMouseDown;

        foreach (var inputKeyboard in _input.Keyboards)
        {
            inputKeyboard.KeyDown += HandleKeyDown;
            inputKeyboard.KeyUp += HandleKeyUp;
        }

        _gl.ClearColor(Color.CornflowerBlue);

        // 1. Define the vertex data (a single point)
        float[] vertices =
        {
            0.0f, 0.0f, 0.0f // center
        };


        _vao = _gl.GenVertexArray();
        _vbo = _gl.GenBuffer();

        _gl.BindVertexArray(_vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);

        fixed (void* v = vertices)
        {
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)), v,
                BufferUsageARB.StaticDraw);
        }

        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), null);
        _gl.EnableVertexAttribArray(0);

        // 2. Create and compile shaders
        var vertexShader = CreateAndCompileShader(out var fragmentShader);

        // 3. Link shaders to program
        LinkShadersToProgram(vertexShader, fragmentShader);
        _gl.UseProgram(_shaderProgram);

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
            UpdateVertexBuffer();
        }

        //point generation
        if (keyPressed == Key.F1)
        {
            _logger.Information("F1 key pressed. Generating curve points for ellipse.");
            CurveHelper.GenerateCurvePoints(ref _points, 0, 2 * Math.PI,
                t => 0.5 * Math.Cos(t), 
                t => 0.27 * Math.Sin(t),
                300);
            UpdateVertexBuffer();
        }
        if (keyPressed == Key.F2)
        {
            _logger.Information("F2 key pressed. Generating curve points for Lissajous curve.");
            CurveHelper.GenerateCurvePoints(
                ref _points, 0, 2 * Math.PI,
                t => 0.4 * Math.Sin(3 * t),
                t => 0.4 * Math.Cos(2 * t),
                200
            );            
            UpdateVertexBuffer();
        }

        if (keyPressed == Key.F3)
        {
            _logger.Information("F3 key pressed. Generating curve points for Epitrochoid curve.");
            double bigR = 0.3, r = 0.1, d = 0.2;
            CurveHelper.GenerateCurvePoints(
                ref _points, 0, 2 * Math.PI,
                t => (bigR + r) * Math.Cos(t) - d * Math.Cos((bigR + r) * t / r),
                t => (bigR + r) * Math.Sin(t) - d * Math.Sin((bigR + r) * t / r),
                300
            );          
            UpdateVertexBuffer();
        }

        if (keyPressed == Key.F4)
        {
            _logger.Information("F4 key pressed. Generating curve points for Hypotrochoid curve.");
            double bigR = 0.35, r = 0.15, d = 0.1;
            CurveHelper.GenerateCurvePoints(
                ref _points, 0, 2 * Math.PI,
                t => (bigR - r) * Math.Cos(t) + d * Math.Cos((bigR - r) * t / r),
                t => (bigR - r) * Math.Sin(t) - d * Math.Sin((bigR - r) * t / r),
                300
            );         
            UpdateVertexBuffer();
        }
        
        if(keyPressed == Key.F5)
        {
            _logger.Information("F5 key pressed. Generating curve points for Butterfly Curve.");
            CurveHelper.GenerateCurvePoints(
                ref _points, 0, 12 * Math.PI,
                t => 0.15 * Math.Sin(t) * (Math.Exp(Math.Cos(t)) - 2 * Math.Cos(4 * t) - Math.Pow(Math.Sin(t / 12), 5)),
                t => 0.15 * Math.Cos(t) * (Math.Exp(Math.Cos(t)) - 2 * Math.Cos(4 * t) - Math.Pow(Math.Sin(t / 12), 5)),
                500
            );        
            UpdateVertexBuffer();
        }
        
        if(keyPressed == Key.F6)
        {
            _logger.Information("F6 key pressed. Generating curve points for Spiral.");
            CurveHelper.GenerateCurvePoints(
                ref _points, 0, 6 * Math.PI,
                t => 0.02 * t * Math.Cos(t),
                t => 0.02 * t * Math.Sin(t),
                300
            );   
            UpdateVertexBuffer();
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
    }

    private unsafe void UpdateVertexBuffer()
    {
        var allPoints = _points.SelectMany(p => p).ToArray();

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        fixed (void* v = allPoints)
        {
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(allPoints.Length * sizeof(float)), v,
                BufferUsageARB.StaticDraw);
        }
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
}