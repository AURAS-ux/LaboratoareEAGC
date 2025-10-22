using System;
using System.Drawing;
using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace tema1;

public class SilkWindow
{
    private static IWindow? _window;
    private static GL? _gl;
    private WindowOptions _options;

    private Vector2D<int> _windowSize = new Vector2D<int>(1280, 720);

    private uint _vao;
    private uint _vbo;
    private uint _shaderProgram;
    private IInputContext _input;
    
    private List<float[]> _points = new();
    private RenderMode _renderMode;

    public SilkWindow()
    {
        _options = WindowOptions.Default;
        _options.Title = "Silk - Point Example";
        _options.Size = _windowSize;
    }

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
        var normalizedX = Utils.NormalizeNumber(pos.X, _windowSize.X, 0);
        var normalizedY = Utils.NormalizeNumber(pos.Y, _windowSize.Y, 0);
        Console.WriteLine($"Normalized coordinates: {normalizedX}, {normalizedY}");
        
        _points.Add([normalizedX,-normalizedY,0.0f]);
    }

    private void OnRender(double dt)
    {
        _gl!.Clear(ClearBufferMask.ColorBufferBit);
        _gl.UseProgram(_shaderProgram);
        _gl.BindVertexArray(_vao);

        switch (_renderMode)
        {
            case RenderMode.Points:
                _gl.PointSize(5.0f);
                _gl.DrawArrays(PrimitiveType.Points, 0, (uint)_points.Count);
                break;
            case RenderMode.Lines:
                if (_points.Count == 2)
                {
                    _gl.DrawArrays(PrimitiveType.LineStrip, 0, (uint)_points.Count);
                }
                break;
            case RenderMode.Polygon:
                if (_points.Count >= 3)
                {
                   _gl.DrawArrays(PrimitiveType.TriangleFan, 0, (uint)_points.Count); 
                }
                break;
        }
        
    }

    private unsafe void OnLoad()
    {
        _gl = _window!.CreateOpenGL();
        Console.WriteLine("Window loaded");

        _input = _window.CreateInput();

        foreach (var inputMouse in _input.Mice)
        {
            inputMouse.MouseDown += OnInputMouseOnMouseDown;
        }

        foreach (var inputKeyboard in _input.Keyboards)
        {
            inputKeyboard.KeyDown += HandleKeyDown;
        }

        _gl.ClearColor(Color.CornflowerBlue);

        // 1. Define the vertex data (a single point)
        float[] vertices =
        {
            0.0f, 0.0f, 0.0f,     // center
        };


        _vao = _gl.GenVertexArray();
        _vbo = _gl.GenBuffer();

        _gl.BindVertexArray(_vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);

        fixed (void* v = vertices)
        {
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)), v, BufferUsageARB.StaticDraw);
        }

        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), null);
        _gl.EnableVertexAttribArray(0);

        // 2. Create and compile shaders
        int success;
        var vertexShader = CreateAndCompileShader(out var fragmentShader);

        // 3. Link shaders to program
        LinkShadersToProgram(vertexShader, fragmentShader);

        // Clean up shader objects (they're now linked into program)
        CleanShaderObjects(vertexShader, fragmentShader);
    }

    private void HandleKeyDown(IKeyboard keyboard, Key keyPressed, int arg3)
    {
        if (keyPressed == Key.Escape)
        {
            Console.WriteLine("Closing window.");
            _window?.Close();
        }

        if (keyPressed == Key.Space)
        {
            Console.WriteLine("clearing points.");
            _points.Clear();
            UpdateVertexBuffer();
        }
    }

    private void OnInputMouseOnMouseDown(IMouse mouse, MouseButton button)
    {
        if (button == MouseButton.Left)
        {
            var pos = mouse.Position;
            Console.WriteLine($"Mouse clicked at: {pos.X}, {pos.Y}");
            AddNewPoint(pos);
            UpdateVertexBuffer();
        }
        
        if(button == MouseButton.Right)
        {
            Console.WriteLine($"Right mouse button clicked. Changing render mode for {_points.Count} points.");
            _renderMode = _renderMode switch
            {
                RenderMode.Points => RenderMode.Lines,
                RenderMode.Lines => RenderMode.Polygon,
                _ => RenderMode.Points
            };
            
            Console.WriteLine($"Render mode changed to: {_renderMode}");
        }
    }

    private unsafe void UpdateVertexBuffer()
    {
        float[] allPoints = _points.SelectMany(p => p).ToArray();
        
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        fixed (void* v = allPoints)
        {
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(allPoints.Length * sizeof(float)), v, BufferUsageARB.StaticDraw);
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
        const string vertexShaderSource = @"
            #version 330 core
            layout (location = 0) in vec3 aPosition;
            void main()
            {
                gl_Position = vec4(aPosition, 1.0);
            }";

        const string fragmentShaderSource = @"
            #version 330 core
            out vec4 FragColor;
            void main()
            {
                FragColor = vec4(1.0, 0.2, 0.2, 1.0);
            }";

        uint vertexShader = _gl.CreateShader(ShaderType.VertexShader);
        _gl.ShaderSource(vertexShader, vertexShaderSource);
        _gl.CompileShader(vertexShader);
        _gl.GetShader(vertexShader, ShaderParameterName.CompileStatus, out int success);
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
}