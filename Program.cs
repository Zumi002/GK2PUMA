using System.Collections.Generic;

using GK2PUMA.Entities;
using GK2PUMA.Graphics;

using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace GK2PUMA;

internal class Program
{
    private static IWindow s_window;
    private static readonly List<Entity> GameObjects = new List<Entity>();
    private static IKeyboard s_keyboard;
    private static IMouse s_mouse;
    private static Camera s_camera;

    static void Main(string[] args)
    {
        var options = WindowOptions.Default;
        Silk.NET.Windowing.Glfw.GlfwWindowing.Use();
        options.API = GraphicsAPI.None;
        options.Size = new Vector2D<int>(1280, 720);
        options.Title = "GK2PUMA";

        s_window = Window.Create(options);

        s_window.Load += OnLoad;
        s_window.Update += OnUpdate;
        s_window.Render += OnRender;
        s_window.Closing += OnClosing;

        s_window.Run();
    }

    private static void OnLoad()
    {
        var hwnd = s_window.Native.Win32.Value.Hwnd;
        uint width = (uint)s_window.Size.X;
        uint height = (uint)s_window.Size.Y;

        var swapChainDesc = new SwapChainDescription()
        {
            BufferCount = 2,
            BufferDescription = new ModeDescription(width, height, Format.R8G8B8A8_UNorm),
            Windowed = true,
            OutputWindow = hwnd,
            SampleDescription = new SampleDescription(1, 0),
            SwapEffect = SwapEffect.FlipDiscard,
            BufferUsage = Usage.RenderTargetOutput
        };

        D3D11.D3D11CreateDeviceAndSwapChain(
            null,
            DriverType.Hardware,
            DeviceCreationFlags.BgraSupport,
            new[] { FeatureLevel.Level_11_0 },
            swapChainDesc,
            out var swapChain,
            out var device,
            out _,
            out var context);

        GI.Instance.Device = device;
        GI.Instance.Context = context;
        GI.Instance.SwapChain = swapChain;

        GI.Instance.Resize(width, height);

        s_window.Resize += (size) =>
        {
            GI.Instance.Resize((uint)size.X, (uint)size.Y);
        };

        var unlitShaderInputElements = new[]
        {
            new InputElementDescription("POSITION", 0, Format.R32G32B32_Float, 0, 0),
            new InputElementDescription("NORMAL", 0, Format.R32G32B32_Float, 12, 0)
        };

        const string basePath = "GK2PUMA.Shaders.";
        var unlitShader = new Shader($"{basePath}unlitVS.hlsl", $"{basePath}unlitPS.hlsl", unlitShaderInputElements);
        var phongShader = new Shader($"{basePath}phongVS.hlsl", $"{basePath}phongPS.hlsl", unlitShaderInputElements);
        GI.Instance.ShaderManager.AddShader(ShaderManager.UnlitShaderName, unlitShader);
        GI.Instance.ShaderManager.AddShader(ShaderManager.PhongShaderName, phongShader);

        var input = s_window.CreateInput();
        s_keyboard = input.Keyboards[0];
        s_mouse = input.Mice[0];

        s_camera = new Camera((float)width / height);

        GameObjects.Add(s_camera);
        GameObjects.Add(new InsideCube());
        var myQuad = new Quad();

        myQuad.Transform.Position = new System.Numerics.Vector3(0, -1, 2);
        myQuad.Transform.Rotation = new System.Numerics.Vector3(1.0f, 0.5f, 0);
        myQuad.Transform.Scale = 2.0f;

        GameObjects.Add(myQuad);

        var puma = new Puma();
        puma.Transform.Position = new System.Numerics.Vector3(0, 0, 1);
        GameObjects.Add(puma);
    }

    private static void OnUpdate(double deltaTime)
    {
        float dt = (float)deltaTime;

        foreach (var obj in GameObjects)
        {
            obj.HandleInput(s_keyboard, s_mouse, dt);
        }

        foreach (var obj in GameObjects)
        {
            obj.Update(dt);
        }
    }

    private static void OnRender(double deltaTime)
    {
        GI.Instance.Context.ClearRenderTargetView(GI.Instance.RenderTargetView, new Color4(0.1f, 0.1f, 0.1f, 1.0f));
        GI.Instance.Context.ClearDepthStencilView(GI.Instance.DepthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);
        GI.Instance.Context.OMSetRenderTargets(GI.Instance.RenderTargetView, GI.Instance.DepthStencilView);

        foreach (var obj in GameObjects)
        {
            obj.Render(s_camera);
        }

        GI.Instance.SwapChain.Present(1, PresentFlags.None);
    }

    private static void OnClosing()
    {
        foreach (var obj in GameObjects)
        {
            (obj as IDisposable)?.Dispose();
        }

        GI.Instance.ShaderManager.DisposeAll();
        GI.Instance.RenderTargetView?.Dispose();
        GI.Instance.SwapChain?.Dispose();
        GI.Instance.Context?.Dispose();
        GI.Instance.Device?.Dispose();
    }
}