using System.Numerics;

using GK2PUMA.Graphics;

namespace GK2PUMA.Entities;

public class PointLight : Entity, IDisposable
{
    public readonly Transform Transform = new();
    public Vector4 Color;

    private const int Precision = 16;

    private readonly Mesh _mesh;
    private readonly ConstantBuffer<ConstantBufferData> _constantBuffer;

    public PointLight(Vector3 position, Vector4 color)
    {
        Transform.Position = position;
        Transform.Scale = 0.05f;
        Color = color;
        _mesh = CreateSphereMesh();
        _constantBuffer = new ConstantBuffer<ConstantBufferData>();
    }

    private static Mesh CreateSphereMesh()
    {
        var vertices = new List<Vertex>();
        var indices = new List<uint>();

        for (int i = 0; i <= Precision; i++)
        {
            double lat = Math.PI * (-0.5 + (double)i / Precision);
            double sinLat = Math.Sin(lat);
            double cosLat = Math.Cos(lat);

            for (int j = 0; j <= Precision; j++)
            {
                double lon = 2 * Math.PI * (j == Precision ? 0 : j) / Precision;
                float x = (float)(Math.Cos(lon) * cosLat);
                float y = (float)sinLat;
                float z = (float)(Math.Sin(lon) * cosLat);

                vertices.Add(new Vertex(new Vector3(x, y, z), new Vector3(x, y, z)));
            }
        }

        for (uint i = 0; i < Precision; i++)
        {
            for (uint j = 0; j < Precision; j++)
            {
                uint first = i * (Precision + 1) + j;
                uint second = first + (Precision + 1);

                indices.Add(first);
                indices.Add(second);
                indices.Add(first + 1);
                indices.Add(second);
                indices.Add(second + 1);
                indices.Add(first + 1);
            }
        }

        return new Mesh(vertices, indices);
    }

    public override void Render(Camera camera)
    {
        GI.Instance.LightManager.Add(Transform.Position, Color);

        var shader = GI.Instance.ShaderManager.GetShader(ShaderManager.UnlitShaderName);
        shader.Use();

        var model = Transform.ModelMatrix;
        Matrix4x4.Invert(model, out var invModel);
        _constantBuffer.Update(new ConstantBufferData
        {
            Model = Matrix4x4.Transpose(model),
            ModelInvT = invModel,
            View = Matrix4x4.Transpose(camera.ViewMatrix),
            Projection = Matrix4x4.Transpose(camera.ProjectionMatrix),
            SurfaceColor = Color,
            CameraPos = new Vector4(camera.Position, 1f),
        });
        _constantBuffer.Bind(0);
        GI.Instance.LightManager.Bind(1);

        _mesh.Bind();
        GI.Instance.Context.DrawIndexed((uint)_mesh.IndexCount, 0, 0);
        _mesh.Unbind();
    }

    public void Dispose()
    {
        _mesh.Dispose();
        _constantBuffer.Dispose();
    }
}