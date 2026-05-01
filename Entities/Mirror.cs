namespace GK2PUMA.Entities;
public class Mirror : Quad
{
    public override void Render(Camera camera)
    {
        GI.Instance.Pipeline.SubmitMirror(_mesh, Transform.ModelMatrix, Transform.InvModelMatrix, Color);
    }
}
