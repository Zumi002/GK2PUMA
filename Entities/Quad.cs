using System.Numerics;

using GK2PUMA.Graphics;

namespace GK2PUMA.Entities;

public class Quad : Entity
{
    protected readonly Mesh _mesh;

    public readonly Transform Transform = new Transform();
    public Vector4 Color = new Vector4(0.8f, 0.2f, 0.2f, 1.0f);

    public float Thickness
    {
        get; private set;
    }

    public Quad(float thickness = 0.005f)
    {
        Thickness = thickness;
        float hZ = thickness / 2.0f;

        var vertices = new Vertex[]
        {
            new Vertex(new Vector3(-1, -1, -hZ), new Vector3(0, 0, -1)),
            new Vertex(new Vector3(-1,  1, -hZ), new Vector3(0, 0, -1)),
            new Vertex(new Vector3( 1,  1, -hZ), new Vector3(0, 0, -1)),
            new Vertex(new Vector3( 1, -1, -hZ), new Vector3(0, 0, -1)),

            new Vertex(new Vector3( 1, -1, hZ), new Vector3(0, 0, 1)),
            new Vertex(new Vector3( 1,  1, hZ), new Vector3(0, 0, 1)),
            new Vertex(new Vector3(-1,  1, hZ), new Vector3(0, 0, 1)),
            new Vertex(new Vector3(-1, -1, hZ), new Vector3(0, 0, 1)),

            new Vertex(new Vector3(-1, 1, -hZ), new Vector3(0, 1, 0)),
            new Vertex(new Vector3(-1, 1,  hZ), new Vector3(0, 1, 0)),
            new Vertex(new Vector3( 1, 1,  hZ), new Vector3(0, 1, 0)),
            new Vertex(new Vector3( 1, 1, -hZ), new Vector3(0, 1, 0)),

            new Vertex(new Vector3(-1, -1,  hZ), new Vector3(0, -1, 0)),
            new Vertex(new Vector3(-1, -1, -hZ), new Vector3(0, -1, 0)),
            new Vertex(new Vector3( 1, -1, -hZ), new Vector3(0, -1, 0)),
            new Vertex(new Vector3( 1, -1,  hZ), new Vector3(0, -1, 0)),

            new Vertex(new Vector3(-1, -1,  hZ), new Vector3(-1, 0, 0)),
            new Vertex(new Vector3(-1,  1,  hZ), new Vector3(-1, 0, 0)),
            new Vertex(new Vector3(-1,  1, -hZ), new Vector3(-1, 0, 0)),
            new Vertex(new Vector3(-1, -1, -hZ), new Vector3(-1, 0, 0)),

            new Vertex(new Vector3(1, -1, -hZ), new Vector3(1, 0, 0)),
            new Vertex(new Vector3(1,  1, -hZ), new Vector3(1, 0, 0)),
            new Vertex(new Vector3(1,  1,  hZ), new Vector3(1, 0, 0)),
            new Vertex(new Vector3(1, -1,  hZ), new Vector3(1, 0, 0)),
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

        var adjIndices = AdjacencyHelper.Build(vertices, indices);
        _mesh = new Mesh(vertices, indices, adjIndices);
    }

    public override void Render(Camera camera)
    {
        GI.Instance.Pipeline.SubmitOpaque(_mesh, Transform.ModelMatrix, Transform.InvModelMatrix, Color);
    }
}