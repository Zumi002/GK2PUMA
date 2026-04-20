using Vortice.Direct3D11;
using Vortice.DXGI;

namespace GK2PUMA
{
    public class Graphics
    {
        private static Graphics _instance;
        public static Graphics Instance => _instance ??= new Graphics();

        public ID3D11Device Device { get; set; }
        public ID3D11DeviceContext Context { get; set; }
        public IDXGISwapChain SwapChain { get; set; }
        public ID3D11RenderTargetView RenderTargetView { get; set; }

        private Graphics() { }
    }
}
