using StbImageSharp;

using Vortice.Direct3D11;
using Vortice.DXGI;

namespace GK2PUMA.Graphics;

public static class TextureLoader
{
    public static ID3D11ShaderResourceView Load(string path)
    {
        ImageResult image;
        using (var stream = File.OpenRead(path))
        {
            image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        }

        var device = GI.Instance.Device;

        var texDesc = new Texture2DDescription
        {
            Width = (uint)image.Width,
            Height = (uint)image.Height,
            MipLevels = 1,
            ArraySize = 1,
            Format = Format.R8G8B8A8_UNorm,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Immutable,
            BindFlags = BindFlags.ShaderResource
        };

        unsafe
        {
            fixed (byte* p = image.Data)
            {
                var initData = new SubresourceData((nint)p, (uint)(image.Width * 4), 0);
                using var tex = device.CreateTexture2D(texDesc, new[] { initData });
                return device.CreateShaderResourceView(tex);
            }
        }
    }
}
