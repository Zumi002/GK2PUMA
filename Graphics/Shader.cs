using System.Text;

using SharpGen.Runtime;
using Vortice.D3DCompiler;
using Vortice.Direct3D;
using Vortice.Direct3D11;

namespace GK2PUMA.Graphics;

public class Shader : IDisposable
{
    public ID3D11VertexShader VertexShader { get; private set; }
    public ID3D11PixelShader PixelShader { get; private set; }
    public ID3D11InputLayout InputLayout { get; private set; }

    public Shader(string vsPath, string psPath, InputElementDescription[] inputElements)
    {
        var device = GraphicsContext.Instance.Device;

        int lastDot = vsPath.LastIndexOf('.');
        int secondLastDot = lastDot > 0 ? vsPath.LastIndexOf('.', lastDot - 1) : -1;
        string resourcePrefix = secondLastDot >= 0 ? vsPath[..(secondLastDot + 1)] : string.Empty;

        using var include = new EmbeddedInclude(resourcePrefix);

        try
        {
            var vsBlob = Compiler.Compile(Resources.ReadResource(vsPath), null, include, "VS", vsPath, "vs_5_0");
            VertexShader = device.CreateVertexShader(vsBlob.Span);

            var psBlob = Compiler.Compile(Resources.ReadResource(psPath), null, include, "PS", psPath, "ps_5_0");
            PixelShader = device.CreatePixelShader(psBlob.Span);

            InputLayout = device.CreateInputLayout(inputElements, vsBlob.Span);
        }
        catch (Exception ex)
        {
            throw new Exception($"Shader compilation failed:\n{ex.Message}", ex);
        }
    }

    private sealed class EmbeddedInclude : CallbackBase, Include
    {
        private readonly string _prefix;

        public EmbeddedInclude(string prefix)
        {
            _prefix = prefix;
        }

        public Stream Open(IncludeType type, string fileName, Stream? parentStream)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(Resources.ReadResource(_prefix + fileName)));
        }

        public void Close(Stream stream) => stream.Dispose();
    }

    public void Use()
    {
        var context = GI.Instance.Context;

        context.IASetInputLayout(InputLayout);
        context.VSSetShader(VertexShader);
        context.PSSetShader(PixelShader);
    }

    public void Dispose()
    {
        VertexShader?.Dispose();
        PixelShader?.Dispose();
        InputLayout?.Dispose();
    }
}