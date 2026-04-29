using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace GK2PUMA.Graphics;

public class GraphicsContext
{
    private static GraphicsContext s_instance;
    public static GraphicsContext Instance => s_instance ??= new GraphicsContext();

    public ID3D11Device Device
    {
        get;
        set;
    }

    public ID3D11DeviceContext Context
    {
        get;
        set;
    }

    public IDXGISwapChain SwapChain
    {
        get;
        set;
    }

    public ID3D11RenderTargetView RenderTargetView
    {
        get;
        set;
    }

    public ID3D11DepthStencilView DepthStencilView
    {
        get;
        set;
    }

    public ShaderManager ShaderManager
    {
        get;
        set;
    } = new ShaderManager();

    public LightManager LightManager
    {
        get;
    } = new();

    public ID3D11DepthStencilState DepthStencilStateWrite
    {
        get;
        set;
    }

    public ID3D11DepthStencilState DepthStencilStateTest
    {
        get;
        set;
    }

    public ID3D11RasterizerState RasterizerStateCounterClockWise
    {
        get;
        set;
    }

    public ID3D11RasterizerState RasterizerStateNoCull
    {
        get;
        set;
    }

    public ID3D11BlendState BlendStateAlpha
    {
        get;
        set;
    }

    public ID3D11BlendState BlendStateNoColor
    {
        get;
        set;
    }

    public uint Width
    {
        get;
        private set;
    }

    public uint Height
    {
        get;
        private set;
    }

    private GraphicsContext()
    {
    }

    public void Resize(uint width, uint height)
    {
        if (width == 0 || height == 0 || (width == Width && height == Height))
        {
            return;
        }

        Width = width;
        Height = height;

        RenderTargetView?.Dispose();
        DepthStencilView?.Dispose();

        SwapChain.ResizeBuffers(2, width, height, Format.R8G8B8A8_UNorm, SwapChainFlags.None);

        using var backBuffer = SwapChain.GetBuffer<ID3D11Texture2D>(0);
        RenderTargetView = Device.CreateRenderTargetView(backBuffer);

        var depthDesc = new Texture2DDescription
        {
            Width = width,
            Height = height,
            MipLevels = 1,
            ArraySize = 1,
            Format = Format.D24_UNorm_S8_UInt,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Default,
            BindFlags = BindFlags.DepthStencil
        };

        using var depthBuffer = Device.CreateTexture2D(depthDesc);
        DepthStencilView = Device.CreateDepthStencilView(depthBuffer);

        Context.RSSetViewport(new Viewport(0, 0, width, height));
    }
}