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

    private Vector2D<int> _windowSize = new(1280, 720);

    private uint _vao;
    private uint _vbo;
    private uint _shaderProgram;
    private IInputContext _input;
    
    private List<float[]> _points = new();
    private RenderMode _renderMode;
    
    //tema2 
    private Vector2D<float> _initialMousePosition = new(0.0f, 0.0f);
    private Vector2D<float> _finalMousePosition = new(0.0f, 0.0f);
    private bool _isDragging = false;
    
    private float _scaleX = 1.0f;
    private float _scaleY = 1.0f;
    private int _sxLocation;
    private int _syLocation;
    
    // tema 2 rotatie
    private float _rotation = 0.0f;
    private int _uRotationLocation;

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
        
        _gl.Uniform1(_sxLocation, _scaleX);
        _gl.Uniform1(_syLocation, _scaleY);
        
        _gl.Uniform1(_uRotationLocation, _rotation);
        
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
            inputKeyboard.KeyUp += HandleKeyUp;
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
        var vertexShader = CreateAndCompileShader(out var fragmentShader);

        // 3. Link shaders to program
        LinkShadersToProgram(vertexShader, fragmentShader);
        _gl.UseProgram(_shaderProgram);
        
        _sxLocation = _gl.GetUniformLocation(_shaderProgram, "sX");
        _syLocation = _gl.GetUniformLocation(_shaderProgram, "sY");
        
        _uRotationLocation = _gl.GetUniformLocation(_shaderProgram, "uRotation");

        // Clean up shader objects (they're now linked into program)
        CleanShaderObjects(vertexShader, fragmentShader);
    }

    private void HandleKeyUp(IKeyboard keyboard, Key keyRaised, int arg3)
    {
        if (keyRaised == Key.F1)
        {
            Console.WriteLine("stopped dragging");
            Console.WriteLine("stopped scaling");
            _isDragging = false;
            _finalMousePosition.X = _input.Mice[0].Position.X;
            _finalMousePosition.Y = _input.Mice[0].Position.Y;
            Console.WriteLine("Final mouse position: " + _finalMousePosition.X + ", " + _finalMousePosition.Y);
        }

        if (keyRaised == Key.F6)
        {
            Console.WriteLine("Raised F6 key - performing rotation around origin.");
            _finalMousePosition.X = _input.Mice[0].Position.X;
            _finalMousePosition.Y = _input.Mice[0].Position.Y;
            Console.WriteLine("Final mouse position for rotation: " + _finalMousePosition.X + ", " + _finalMousePosition.Y);
            Transformare2D.RotateAcrossOrigin(ref _initialMousePosition, ref _finalMousePosition, ref _rotation);
            Console.WriteLine($"Updated rotation angle: {_rotation} radians");
            _initialMousePosition = _finalMousePosition;
        }
        
        //rotatie
        if (keyRaised == Key.F7)
        {
            Console.WriteLine("Raised F7 key - performing translation.");
            _finalMousePosition.X = _input.Mice[0].Position.X;
            _finalMousePosition.Y = _input.Mice[0].Position.Y;
            Console.WriteLine("Final mouse position for translation: " + _finalMousePosition.X + ", " + _finalMousePosition.Y);
            Transformare2D.TranslatePoints(_initialMousePosition, _finalMousePosition, ref _points, _windowSize);
            UpdateVertexBuffer();
            _initialMousePosition = _finalMousePosition;
        }
    }

    private void HandleKeyDown(IKeyboard keyboard, Key keyPressed, int arg3)
    {
        if (keyPressed == Key.Escape)
        {
            Console.WriteLine("Closing window.");
            _window?.Close();
        }
//TEMA 1 SCALARE 
        if (keyPressed == Key.F1)
        {
            Console.WriteLine("triggered scaling");
            _initialMousePosition.X = _input.Mice[0].Position.X;
            _initialMousePosition.Y = _input.Mice[0].Position.Y;
            _isDragging = true;
            Console.WriteLine($"Initial mouse position: {_initialMousePosition.X}, {_initialMousePosition.Y}");
            Console.WriteLine("Dragging started.");
        }

        if (keyPressed == Key.F2)
        {
            Console.WriteLine("Registered F2 key press. Performing mouse scaling.");
            // PerformMouseScaling();
            Transformare2D.PerformMouseScaling(_initialMousePosition, _finalMousePosition, ref _scaleX, ref _scaleY);
            Console.WriteLine($"Scaling factors applied - X: {_scaleX}, Y: {_scaleY}");
            _initialMousePosition = _finalMousePosition;
        }
        
// FINAL SCALARE
//TEMA 1 Simetrie 

        if (keyPressed == Key.F3)
        {
            Console.WriteLine("triggered ox simmetry");
            Transformare2D.MirrorAcrossOX(_points,false);
            UpdateVertexBuffer();
        }

        if (keyPressed == Key.F4)
        {
            Console.WriteLine("triggered oy simmetry");
            Transformare2D.MirrorAcrossOY(_points,false);
            UpdateVertexBuffer();
        }

        if (keyPressed == Key.F5)
        {
            Console.WriteLine("triggered origin simmetry");
            Transformare2D.MirrorAcrossOrigin(_points, false);
            UpdateVertexBuffer();
        }
//FINAL Simetrie
//TEMA 1 rotatie fata de O

        if (keyPressed == Key.F6)
        {
            _initialMousePosition.X = _input.Mice[0].Position.X;
            _initialMousePosition.Y = _input.Mice[0].Position.Y;
            Console.WriteLine("Initial mouse position for rotation: " + _initialMousePosition.X + ", " + _initialMousePosition.Y);
            Console.WriteLine("Triggered O rotation");
        }

// Final de rotatie

//tema 1 translatie
        if (keyPressed == Key.F7)
        {
            Console.WriteLine("triggered translation");
            _initialMousePosition.X = _input.Mice[0].Position.X;
            _initialMousePosition.Y = _input.Mice[0].Position.Y;
            Console.WriteLine("Initial mouse position for translation: " + _initialMousePosition.X + ", " + _initialMousePosition.Y);
        }
//final translatie

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
        string vertexShaderSource = LoadShaderSource("Shaders/vertex_shader.glsl");

        string fragmentShaderSource = LoadShaderSource("Shaders/fragment_shader.glsl");

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

    private static string LoadShaderSource(string path)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch(Exception e)
        {
            throw new Exception($"Error loading shader source: {path}", e);
        }
    }

    // private void PerformMouseScaling()
    // {
    //     if (_initialMousePosition == _finalMousePosition)
    //     {
    //         Console.WriteLine("No scaling factor");        
    //         return;
    //     }
    //     float scaleX = _finalMousePosition.X / _initialMousePosition.X;
    //     float scaleY = _finalMousePosition.Y / _initialMousePosition.Y;
    //     Console.WriteLine($"Scaling factors - X: {scaleX}, Y: {scaleY}");
    //     _scaleX = Math.Abs(scaleX);
    //     _scaleY = Math.Abs(scaleY);
    // }
}