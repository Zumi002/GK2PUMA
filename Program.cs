using Silk.NET.Maths;
using Silk.NET.Windowing;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace GK2PUMA
{
    internal class Program
    {
        private static IWindow _window;
        private static List<Entity> _gameObjects = new List<Entity>();

        static void Main(string[] args)
        {
            var options = WindowOptions.Default;
            Silk.NET.Windowing.Glfw.GlfwWindowing.Use();
            options.API = GraphicsAPI.None;
            options.Size = new Vector2D<int>(1280, 720);
            options.Title = "GK2PUMA";

            _window = Window.Create(options);

            _window.Load += OnLoad;
            _window.Update += OnUpdate;
            _window.Render += OnRender;
            _window.Closing += OnClosing;

            _window.Run();
        }

        private static void OnLoad()
        {
            var hwnd = _window.Native.Win32.Value.Hwnd;
            uint width = (uint)_window.Size.X;
            uint height = (uint)_window.Size.Y;

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

            var backBuffer = swapChain.GetBuffer<ID3D11Texture2D>(0);
            var renderTargetView = device.CreateRenderTargetView(backBuffer);
            backBuffer.Dispose();

            Graphics.Instance.Device = device;
            Graphics.Instance.Context = context;
            Graphics.Instance.SwapChain = swapChain;
            Graphics.Instance.RenderTargetView = renderTargetView;

            _gameObjects.Add(new TestObject());
        }

        private static void OnUpdate(double deltaTime)
        {
            float dt = (float)deltaTime;

            foreach (var obj in _gameObjects)
            {
                obj.HandleInput();
            }

            foreach (var obj in _gameObjects)
            {
                obj.Update(dt);
            }
        }

        private static void OnRender(double deltaTime)
        {
            Graphics.Instance.Context.OMSetRenderTargets(Graphics.Instance.RenderTargetView);

            foreach (var obj in _gameObjects)
            {
                obj.Render();
            }

            Graphics.Instance.SwapChain.Present(1, PresentFlags.None);
        }

        private static void OnClosing()
        {
            Graphics.Instance.RenderTargetView?.Dispose();
            Graphics.Instance.SwapChain?.Dispose();
            Graphics.Instance.Context?.Dispose();
            Graphics.Instance.Device?.Dispose();
        }
    }
}