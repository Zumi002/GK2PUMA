using System.Numerics;

using GK2PUMA.Graphics;

namespace GK2PUMA.Entities;

public class Quad : Entity
{
    private readonly Mesh _mesh;
    private readonly ConstantBuffer<ConstantBufferData> _constantBuffer;

    public Transform Transform = new Transform();
    public Vector4 Color = new Vector4(0.8f, 0.2f, 0.2f, 1.0f);
    public Quad()
    {
        var vertices = new Vertex[]
        {
            new Vertex(new Vector3(-1, -1, 0), new Vector3(0, 0, -1)),
            new Vertex(new Vector3(-1,  1, 0), new Vector3(0, 0, -1)),
            new Vertex(new Vector3( 1,  1, 0), new Vector3(0, 0, -1)),
            new Vertex(new Vector3( 1, -1, 0), new Vector3(0, 0, -1))
        };

        var indices = new uint[]
        {
            0, 1, 2,
            0, 2, 3
        };

        _mesh = new Mesh(vertices, indices);
        _constantBuffer = new ConstantBuffer<ConstantBufferData>();
    }

    public override void Render(Camera camera)
    {
        var shader = GI.Instance.ShaderManager.GetShader("Unlit");
        shader.Use();

        _constantBuffer.Update(new ConstantBufferData
        {
            Model = Matrix4x4.Transpose(Transform.ModelMatrix),
            View = Matrix4x4.Transpose(camera.ViewMatrix),
            Projection = Matrix4x4.Transpose(camera.ProjectionMatrix),
            SurfaceColor = Color
        });

        _constantBuffer.Bind(0);
        _mesh.Bind();

        GI.Instance.Context.DrawIndexed((uint)_mesh.IndexCount, 0, 0);

        _mesh.Unbind();
    }
}