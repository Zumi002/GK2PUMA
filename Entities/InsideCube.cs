using System.Numerics;

using GK2PUMA.Graphics;

namespace GK2PUMA.Entities;

public class InsideCube : Entity
{
    private readonly Mesh _mesh;
    private readonly ConstantBuffer<ConstantBufferData> _constantBuffer;

    public Matrix4x4 TransformMatrix = Matrix4x4.Identity;
    public Vector4 Color = new Vector4(0.3f, 0.3f, 0.3f, 1.0f);

    public InsideCube()
    {
        var vertices = new Vertex[]
        {
            new Vertex(new Vector3(-5, -5,  5), new Vector3(0, 0, -1)),
            new Vertex(new Vector3(-5,  5,  5), new Vector3(0, 0, -1)),
            new Vertex(new Vector3( 5,  5,  5), new Vector3(0, 0, -1)),
            new Vertex(new Vector3( 5, -5,  5), new Vector3(0, 0, -1)),

            new Vertex(new Vector3(-5, -5, -5), new Vector3(0, 0, 1)),
            new Vertex(new Vector3( 5, -5, -5), new Vector3(0, 0, 1)),
            new Vertex(new Vector3( 5,  5, -5), new Vector3(0, 0, 1)),
            new Vertex(new Vector3(-5,  5, -5), new Vector3(0, 0, 1)),

            new Vertex(new Vector3(-5, -5, -5), new Vector3(1, 0, 0)),
            new Vertex(new Vector3(-5,  5, -5), new Vector3(1, 0, 0)),
            new Vertex(new Vector3(-5,  5,  5), new Vector3(1, 0, 0)),
            new Vertex(new Vector3(-5, -5,  5), new Vector3(1, 0, 0)),

            new Vertex(new Vector3( 5, -5, -5), new Vector3(-1, 0, 0)),
            new Vertex(new Vector3( 5, -5,  5), new Vector3(-1, 0, 0)),
            new Vertex(new Vector3( 5,  5,  5), new Vector3(-1, 0, 0)),
            new Vertex(new Vector3( 5,  5, -5), new Vector3(-1, 0, 0)),

            new Vertex(new Vector3(-5,  5, -5), new Vector3(0, -1, 0)),
            new Vertex(new Vector3( 5,  5, -5), new Vector3(0, -1, 0)),
            new Vertex(new Vector3( 5,  5,  5), new Vector3(0, -1, 0)),
            new Vertex(new Vector3(-5,  5,  5), new Vector3(0, -1, 0)),

            new Vertex(new Vector3(-5, -5, -5), new Vector3(0, 1, 0)),
            new Vertex(new Vector3(-5, -5,  5), new Vector3(0, 1, 0)),
            new Vertex(new Vector3( 5, -5,  5), new Vector3(0, 1, 0)),
            new Vertex(new Vector3( 5, -5, -5), new Vector3(0, 1, 0))
        };

        var indices = new uint[]
        {
            0, 1, 2, 0, 2, 3,
            4, 5, 6, 4, 6, 7,
            8, 9, 10, 8, 10, 11,
            12, 13, 14, 12, 14, 15,
            16, 17, 18, 16, 18, 19,
            20, 21, 22, 20, 22, 23
        };

        _mesh = new Mesh(vertices, indices);
        _constantBuffer = new ConstantBuffer<ConstantBufferData>();
    }

    public override void Render(Camera camera)
    {
        var shader = GraphicsContext.Instance.ShaderManager.GetShader("Unlit");
        shader.Use();

        _constantBuffer.Update(new ConstantBufferData
        {
            Model = Matrix4x4.Transpose(TransformMatrix),
            View = Matrix4x4.Transpose(camera.ViewMatrix),
            Projection = Matrix4x4.Transpose(camera.ProjectionMatrix),
            SurfaceColor = Color
        });

        _constantBuffer.Bind(0);
        _mesh.Bind();

        GraphicsContext.Instance.Context.DrawIndexed((uint)_mesh.IndexCount, 0, 0);

        _mesh.Unbind();
    }
}