using Vortice.Direct3D11;
using Vortice.DXGI;

namespace GK2PUMA.Graphics;

public class GraphicsContext
{
    private static GraphicsContext s_instance;
    public static GraphicsContext Instance => s_instance ??= new GraphicsContext();

    public ID3D11Device Device { get; set; }
    public ID3D11DeviceContext Context { get; set; }
    public IDXGISwapChain SwapChain { get; set; }
    public ID3D11RenderTargetView RenderTargetView { get; set; }
    public ID3D11DepthStencilView DepthStencilView { get; set; }
    public ShaderManager ShaderManager { get; set; } = new ShaderManager();

    private GraphicsContext() { }
}