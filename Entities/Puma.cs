namespace GK2PUMA.Entities;

using System.Globalization;
using System.Numerics;
using GK2PUMA.Graphics;
using Vortice.Mathematics;

public sealed record Triangle
{
    public int VertexIdxIdx1;
    public int VertexIdxIdx2;
    public int VertexIdxIdx3;

    public static Triangle Parse(string s)
    {
        var nums = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (nums.Length != 3)
        {
            throw new FormatException($"Triangle: expected 3 values, got {nums.Length}.");
        }

        return new Triangle
        {
            VertexIdxIdx1 = int.Parse(nums[0]),
            VertexIdxIdx2 = int.Parse(nums[1]),
            VertexIdxIdx3 = int.Parse(nums[2]),
        };
    }
}

public sealed record Edge
{
    public int VertexPosIdx1;
    public int VertexPosIdx2;
    public int TriangleIdx1;
    public int TriangleIdx2;

    public static Edge Parse(string s)
    {
        var nums = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (nums.Length != 4)
        {
            throw new FormatException($"Edge: expected 4 values, got {nums.Length}.");
        }

        return new Edge
        {
            VertexPosIdx1 = int.Parse(nums[0]),
            VertexPosIdx2 = int.Parse(nums[1]),
            TriangleIdx1 = int.Parse(nums[2]),
            TriangleIdx2 = int.Parse(nums[3]),
        };
    }
}

public sealed class PumaPart
{
    public readonly List<Double3> VertexPositions = [];
    public readonly List<int> VertexPosIndices = [];
    public readonly List<Double3> VertexNormals = [];
    public readonly List<Triangle> Triangles = [];
    public readonly List<Edge> Edges = [];

    public void Load(string path)
    {
        using var reader = new StreamReader(path);

        int posCount = int.Parse(reader.ReadLine()!.Trim());
        for (int i = 0; i < posCount; i++)
        {
            VertexPositions.Add(ParseDouble3(reader.ReadLine()!));
        }

        int vertCount = int.Parse(reader.ReadLine()!.Trim());
        for (int i = 0; i < vertCount; i++)
        {
            var parts = reader.ReadLine()!.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 4)
            {
                throw new FormatException($"Vertex {i}: expected 4 values, got {parts.Length}.");
            }

            VertexPosIndices.Add(int.Parse(parts[0]));
            VertexNormals.Add(new Double3
            {
                X = double.Parse(parts[1], CultureInfo.InvariantCulture),
                Y = double.Parse(parts[2], CultureInfo.InvariantCulture),
                Z = double.Parse(parts[3], CultureInfo.InvariantCulture),
            });
        }

        int triCount = int.Parse(reader.ReadLine()!.Trim());
        for (int i = 0; i < triCount; i++)
        {
            Triangles.Add(Triangle.Parse(reader.ReadLine()!));
        }

        int edgeCount = int.Parse(reader.ReadLine()!.Trim());
        for (int i = 0; i < edgeCount; i++)
        {
            Edges.Add(Edge.Parse(reader.ReadLine()!));
        }
    }

    private static Double3 ParseDouble3(string s)
    {
        var parts = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
        {
            throw new FormatException($"Position: expected 3 values, got {parts.Length}.");
        }

        return new Double3
        {
            X = double.Parse(parts[0], CultureInfo.InvariantCulture),
            Y = double.Parse(parts[1], CultureInfo.InvariantCulture),
            Z = double.Parse(parts[2], CultureInfo.InvariantCulture),
        };
    }

    public Mesh BuildMesh()
    {
        var vertices = new Vertex[VertexPosIndices.Count];
        for (int i = 0; i < VertexPosIndices.Count; i++)
        {
            var pos = VertexPositions[VertexPosIndices[i]];
            var norm = VertexNormals[i];
            vertices[i] = new Vertex(
                new Vector3((float)pos.X, (float)pos.Y, (float)pos.Z),
                new Vector3((float)norm.X, (float)norm.Y, (float)norm.Z)
            );
        }

        var indices = new uint[Triangles.Count * 3];
        for (int t = 0; t < Triangles.Count; t++)
        {
            indices[t * 3 + 0] = (uint)Triangles[t].VertexIdxIdx1;
            indices[t * 3 + 1] = (uint)Triangles[t].VertexIdxIdx2;
            indices[t * 3 + 2] = (uint)Triangles[t].VertexIdxIdx3;
        }

        return new Mesh(vertices, indices);
    }
}

public sealed class Puma : Entity, IDisposable
{
    public const int PartCount = 6;

    public Transform Transform = new();
    public Vector4 Color = new(0.7f, 0.5f, 0.3f, 1.0f);

    private readonly Mesh[] _meshes = new Mesh[PartCount];
    private readonly ConstantBuffer<ConstantBufferData> _constantBuffer = new();

    public Puma(string meshFolder = "Meshes")
    {
        for (int i = 0; i < PartCount; i++)
        {
            var part = new PumaPart();
            part.Load(Path.Combine(meshFolder, $"mesh{i + 1}.txt"));
            _meshes[i] = part.BuildMesh();
        }
    }

    public override void Render(Camera camera)
    {
        var shader = GI.Instance.ShaderManager.GetShader(ShaderManager.PhongShaderName);
        shader.Use();

        Matrix4x4.Invert(Transform.ModelMatrix, out var invModel);
        _constantBuffer.Update(new ConstantBufferData
        {
            Model = Matrix4x4.Transpose(Transform.ModelMatrix),
            ModelInvT = invModel,
            View = Matrix4x4.Transpose(camera.ViewMatrix),
            Projection = Matrix4x4.Transpose(camera.ProjectionMatrix),
            SurfaceColor = Color,
            CameraPos = new Vector4(camera.Position, 1f),
        });
        _constantBuffer.Bind(0);

        foreach (var mesh in _meshes)
        {
            mesh.Bind();
            GI.Instance.Context.DrawIndexed((uint)mesh.IndexCount, 0, 0);
            mesh.Unbind();
        }
    }

    public void Dispose()
    {
        foreach (var mesh in _meshes)
        {
            mesh?.Dispose();
        }

        _constantBuffer?.Dispose();
    }
}
