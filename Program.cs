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

        var dssDesc = Descriptions.NewDepthStencilDescription();
        dssDesc.DepthWriteMask = DepthWriteMask.Zero;
        dssDesc.DepthFunc = ComparisonFunction.LessEqual;
        dssDesc.StencilEnable = true;
        dssDesc.StencilWriteMask = 0xFF;
        dssDesc.FrontFace.StencilPassOp = StencilOperation.Replace;
        dssDesc.FrontFace.StencilFunc = ComparisonFunction.Always;
        dssDesc.BackFace.StencilPassOp = StencilOperation.Replace;
        dssDesc.BackFace.StencilFunc = ComparisonFunction.Always;
        GI.Instance.DepthStencilStateWrite = device.CreateDepthStencilState(dssDesc);

        // dssDesc.BackFace.StencilFunc = ComparisonFunction.Never;
        dssDesc.FrontFace.StencilFunc = ComparisonFunction.Equal;
        // GI.Instance.DepthStencilStateTestNoDepthWrite = device.CreateDepthStencilState(dssDesc)
        
        dssDesc.StencilReadMask = 0xFF;
        dssDesc.BackFace.StencilFunc = ComparisonFunction.Equal;
        dssDesc.DepthWriteMask = DepthWriteMask.All;
        GI.Instance.DepthStencilStateTest = device.CreateDepthStencilState(dssDesc);
        
        GI.Instance.Resize(width, height);
        
        var rasterizerDesc = Descriptions.NewRasterizerDescription();
        rasterizerDesc.FrontCounterClockwise = true;
        GI.Instance.RasterizerStateCounterClockWise = device.CreateRasterizerState(rasterizerDesc);

        rasterizerDesc = Descriptions.NewRasterizerDescription();
        rasterizerDesc.CullMode = CullMode.None;
        GI.Instance.RasterizerStateNoCull = device.CreateRasterizerState(rasterizerDesc);
        
        var bsDesc = Descriptions.NewBlendDescription();
        bsDesc.RenderTarget[0].RenderTargetWriteMask = ColorWriteEnable.None;
        GI.Instance.BlendStateNoColor = device.CreateBlendState(bsDesc);
        
        bsDesc.RenderTarget[0].RenderTargetWriteMask = ColorWriteEnable.All;
        bsDesc.RenderTarget[0].BlendEnable = true;
        bsDesc.RenderTarget[0].SourceBlend = Blend.SourceAlpha;
        bsDesc.RenderTarget[0].DestinationBlend = Blend.InverseSourceAlpha;
        bsDesc.RenderTarget[0].BlendOperation = BlendOperation.Add;
        GI.Instance.BlendStateAlpha = device.CreateBlendState(bsDesc);

        s_window.Resize += size =>
        {
            GI.Instance.Resize((uint)size.X, (uint)size.Y);
        };

        GI.Instance.Pipeline.Init();

        var positionNormalInputElements = new[]
        {
            new InputElementDescription("POSITION", 0, Format.R32G32B32_Float, 0, 0),
            new InputElementDescription("NORMAL", 0, Format.R32G32B32_Float, 12, 0)
        };

        var unlitShader = new Shader($"{ShaderManager.BasePath}unlitVS.hlsl", $"{ShaderManager.BasePath}unlitPS.hlsl",
            positionNormalInputElements);

        var phongShader = new Shader($"{ShaderManager.BasePath}blinnPhongVS.hlsl",
            $"{ShaderManager.BasePath}blinnPhongPS.hlsl", positionNormalInputElements);

        var gpassShader = new Shader($"{ShaderManager.BasePath}gPassVS.hlsl",
            $"{ShaderManager.BasePath}gPassPS.hlsl", positionNormalInputElements);

        var lightPassShader = new Shader($"{ShaderManager.BasePath}lightPassVS.hlsl",
            $"{ShaderManager.BasePath}lightPassPS.hlsl", []);

        var ambientPassShader = new Shader($"{ShaderManager.BasePath}lightPassVS.hlsl",
           $"{ShaderManager.BasePath}ambientPassPS.hlsl", []);

        var shadowVolumeShader = new Shader(
            $"{ShaderManager.BasePath}shadowVolumeVS.hlsl",
            $"{ShaderManager.BasePath}unlitPS.hlsl",
            positionNormalInputElements,
            $"{ShaderManager.BasePath}shadowVolumeGS.hlsl"
        );

        GI.Instance.ShaderManager.AddShader(ShaderManager.ShaderType.Unlit, unlitShader);
        GI.Instance.ShaderManager.AddShader(ShaderManager.ShaderType.BlinnPhong, phongShader);
        GI.Instance.ShaderManager.AddShader(ShaderManager.ShaderType.GPass, gpassShader);
        GI.Instance.ShaderManager.AddShader(ShaderManager.ShaderType.LightPass, lightPassShader);
        GI.Instance.ShaderManager.AddShader(ShaderManager.ShaderType.ShadowVolume, shadowVolumeShader);
        GI.Instance.ShaderManager.AddShader(ShaderManager.ShaderType.AmbientPass, ambientPassShader);

        var input = s_window.CreateInput();
        s_keyboard = input.Keyboards[0];
        s_mouse = input.Mice[0];

        s_camera = new Camera((float)width / height);

        var myQuad = new ReflectiveQuad { 
            RenderScene = () =>
            {
                foreach (var obj in GameObjects.Where(obj => obj is not ReflectiveQuad))
                {
                    obj.Render(s_camera);
                }
            },
            Transform =
            {
                Position = new Vector3(0, -InsideCube.HalfSize + 1f, 2.5f),
                Rotation = new Vector3(30.0f * MathF.PI / 180, 0.0f, 0),
                Scale = 1.0f
            }
        };
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

        foreach (var obj in GameObjects)
        {
            obj.Render(s_camera);
        }

        GI.Instance.Pipeline.Execute(s_camera);

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
        GI.Instance.RasterizerStateNoCull?.Dispose();
        GI.Instance.BlendStateAlpha?.Dispose();
        GI.Instance.BlendStateNoColor?.Dispose();
    }
}