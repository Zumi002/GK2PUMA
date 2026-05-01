using Vortice.Direct3D11;

namespace GK2PUMA.Entities;
public class Mirror : Quad
{
    private readonly ID3D11ShaderResourceView? _mirrorTexture;

    public Mirror()
    {
        _mirrorTexture = GI.Instance.LoadTextureFromStream(Resources.GetResourceStream($"{GI.TextureBasePath}corrugated_iron_02_diff_4k.jpg"));
    }

    public override void Render(Camera camera)
    {
        GI.Instance.Pipeline.SubmitMirror(_mesh, Transform.ModelMatrix, Transform.InvModelMatrix, Color, Thickness, texture: _mirrorTexture);
    }
}
