using System.Numerics;

using GK2PUMA.Entities;
using GK2PUMA.Graphics;

using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Glfw;

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
        GlfwWindowing.Use();
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

        if (device == null || context == null || swapChain == null)
        {
            throw new Exception("Failed to create device.");
        }
        
        GI.Instance.Device = device;
        GI.Instance.Context = context;
        GI.Instance.SwapChain = swapChain;

        DepthStencilDescription dssDesc = new()
        {
            DepthEnable = true,
            DepthWriteMask = DepthWriteMask.All,
            DepthFunc = ComparisonFunction.Less,
            StencilEnable = false,
            StencilReadMask = 0xFF,
            StencilWriteMask = 0xFF,
            FrontFace = new()
            {
                StencilFunc = ComparisonFunction.Always,
                StencilDepthFailOp = StencilOperation.Keep,
                StencilPassOp = StencilOperation.Keep,
                StencilFailOp = StencilOperation.Keep
            },
            BackFace = new()
            {
                StencilFunc = ComparisonFunction.Always,
                StencilDepthFailOp = StencilOperation.Keep,
                StencilPassOp = StencilOperation.Keep,
                StencilFailOp = StencilOperation.Keep
            }
        };
        dssDesc.DepthWriteMask = DepthWriteMask.Zero;
        dssDesc.DepthFunc = ComparisonFunction.LessEqual;
        dssDesc.StencilEnable = true;
        dssDesc.StencilWriteMask = 0xFF;
        dssDesc.FrontFace.StencilPassOp = StencilOperation.Replace;
        dssDesc.FrontFace.StencilFunc = ComparisonFunction.Always;
        GI.Instance.DepthStencilStateWrite = device.CreateDepthStencilState(dssDesc);

        dssDesc.BackFace.StencilFunc = ComparisonFunction.Never;
        dssDesc.FrontFace.StencilFunc = ComparisonFunction.Equal;
        // GI.Instance.DepthStencilStateTestNoDepthWrite = device.CreateDepthStencilState(dssDesc)
        
        dssDesc.StencilReadMask = 0xFF;
        dssDesc.BackFace.StencilFunc = ComparisonFunction.Equal;
        dssDesc.DepthWriteMask = DepthWriteMask.All;
        GI.Instance.DepthStencilStateTest = device.CreateDepthStencilState(dssDesc);
        
        GI.Instance.Resize(width, height);
        
        RasterizerDescription rasterizerDesc = new()
        {
            FillMode = FillMode.Solid,
            CullMode = CullMode.Back,
            DepthBias = 0,
            DepthBiasClamp = 0.0f,
            SlopeScaledDepthBias = 0.0f,
            DepthClipEnable = true,
            ScissorEnable = false,
            MultisampleEnable = false,
            AntialiasedLineEnable = false,
        };
        rasterizerDesc.FrontCounterClockwise = true;
        GI.Instance.RasterizerStateCounterClockWise = device.CreateRasterizerState(rasterizerDesc);

        s_window.Resize += (size) =>
        {
            GI.Instance.Resize((uint)size.X, (uint)size.Y);
        };

        var unlitShaderInputElements = new[]
        {
            new InputElementDescription("POSITION", 0, Format.R32G32B32_Float, 0, 0),
            new InputElementDescription("NORMAL", 0, Format.R32G32B32_Float, 12, 0)
        };

        var unlitShader = new Shader($"{ShaderManager.BasePath}unlitVS.hlsl", $"{ShaderManager.BasePath}unlitPS.hlsl",
            unlitShaderInputElements);
        var phongShader = new Shader($"{ShaderManager.BasePath}blinnPhongVS.hlsl",
            $"{ShaderManager.BasePath}blinnPhongPS.hlsl", unlitShaderInputElements);
        GI.Instance.ShaderManager.AddShader(ShaderManager.ShaderType.Unlit, unlitShader);
        GI.Instance.ShaderManager.AddShader(ShaderManager.ShaderType.BlinnPhong, phongShader);

        var input = s_window.CreateInput();
        s_keyboard = input.Keyboards[0];
        s_mouse = input.Mice[0];

        s_camera = new Camera((float)width / height);

        var myQuad = new ReflectiveQuad();
        myQuad.RenderScene = () =>
        {
            foreach (var obj in GameObjects.Where(obj => obj is not ReflectiveQuad))
            {
                obj.Render(s_camera);
            }
        };
        myQuad.Transform.Position = new Vector3(0, -InsideCube.HalfSize + 1f, 2.5f);
        myQuad.Transform.Rotation = new Vector3(30.0f * MathF.PI / 180, 0.0f, 0);
        myQuad.Transform.Scale = 1.0f;
        GameObjects.Add(myQuad);

        Puma.ThetaStep = MathF.PI / 2;
        var puma = new Puma();
        puma.Sheet = myQuad;
        puma.Radius = 0.25f;
        puma.Transform.Position = new Vector3(0, -InsideCube.HalfSize + 1, 1);
        puma.Transform.Rotation = new Vector3(0, myQuad.Transform.Rotation.Y, 0);
        GameObjects.Add(puma);

        var pointLight = new PointLight(
            position: puma.Transform.Position + new Vector3(-1, 3, 0),
            color: new Vector4(1.0f, 1.0f, 1.0f, 1.0f)
        );
        GI.Instance.LightManager.Add(pointLight.Position, pointLight.Color);
        GI.Instance.LightManager.Update();
        GameObjects.Add(pointLight);

        GameObjects.Add(s_camera);
        GameObjects.Add(new InsideCube());
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
        s_camera.UpdateAndBindViewProjBuffer();

        GI.Instance.Context.ClearRenderTargetView(GI.Instance.RenderTargetView, new Color4(0.1f, 0.1f, 0.1f, 1.0f));
        GI.Instance.Context.ClearDepthStencilView(GI.Instance.DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);
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

        GI.Instance.LightManager.Dispose();
        GI.Instance.ShaderManager.DisposeAll();
        GI.Instance.RenderTargetView?.Dispose();
        GI.Instance.SwapChain?.Dispose();
        GI.Instance.Context?.Dispose();
        GI.Instance.Device?.Dispose();
        GI.Instance.DepthStencilStateWrite?.Dispose();
        GI.Instance.DepthStencilStateTest?.Dispose();
        GI.Instance.RasterizerStateCounterClockWise?.Dispose();
    }
}