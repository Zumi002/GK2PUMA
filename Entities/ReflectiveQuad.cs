using Vortice.Direct3D11;

namespace GK2PUMA.Entities;

public class ReflectiveQuad : Quad, IDisposable
{
    public ID3D11ShaderResourceView? Texture;

    public override void Render(Camera camera)
    {
        // Submit as opaque so the mirror quad gets depth in the main G-Pass.
        // This prevents shadow volumes from passing through the mirror surface.
        // castsShadows=false because shadow volumes for a flat quad are degenerate.
        GI.Instance.Pipeline.SubmitOpaque(_mesh, Transform.ModelMatrix, Transform.InvModelMatrix, Color, null, castsShadows: false);
        GI.Instance.Pipeline.SubmitMirror(_mesh, Transform.ModelMatrix, Transform.InvModelMatrix, Color, Texture);
    }

    public void Dispose() => Texture?.Dispose();
}