using System.Numerics;

using GK2PUMA.Graphics;

namespace GK2PUMA.Entities;

public class Quad : Entity
{
    private readonly Mesh _mesh;

    public readonly Transform Transform = new Transform();
    public Vector4 Color = new Vector4(0.8f, 0.2f, 0.2f, 1.0f);

    public Quad()
    {
        var vertices = new Vertex[]
        {
            new Vertex(new Vector3(-1, -1, 0), new Vector3(0, 0, -1)),
            new Vertex(new Vector3(-1,  1, 0), new Vector3(0, 0, -1)),
            new Vertex(new Vector3( 1,  1, 0), new Vector3(0, 0, -1)),
            new Vertex(new Vector3( 1, -1, 0), new Vector3(0, 0, -1)),
             new Vertex(new Vector3(-1, -1, 0), new Vector3(0, 0, 1)),
            new Vertex(new Vector3(-1,  1, 0), new Vector3(0, 0, 1)),
            new Vertex(new Vector3( 1,  1, 0), new Vector3(0, 0, 1)),
            new Vertex(new Vector3( 1, -1, 0), new Vector3(0, 0, 1))
        };

        var indices = new uint[]
        {
            0, 1, 2,
            //0, 2, 3,
            //4, 6, 5,
            //4, 7, 6,
        };

        _mesh = new Mesh(vertices, indices);
    }

    public override void Render(Camera camera)
    {
       GI.Instance.Pipeline.SubmitOpaque(_mesh, Transform.ModelMatrix, Transform.InvModelMatrix, Color);
    }
}