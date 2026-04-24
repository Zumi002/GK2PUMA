using Vortice.D3DCompiler;
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

        string vsSource = Resources.ReadResource(vsPath);
        string psSource = Resources.ReadResource(psPath);

        try
        {
            var vsBlob = Compiler.Compile(vsSource, "VS", vsPath, "vs_5_0");
            VertexShader = device.CreateVertexShader(vsBlob.Span);

            var psBlob = Compiler.Compile(psSource, "PS", psPath, "ps_5_0");
            PixelShader = device.CreatePixelShader(psBlob.Span);

            InputLayout = device.CreateInputLayout(inputElements, vsBlob.Span);
        }
        catch (Exception ex)
        {
            throw new Exception($"Shader compilation failed:\n{ex.Message}", ex);
        }
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