using System.Numerics;

using GK2PUMA.Graphics;

namespace GK2PUMA.Entities;

/// <summary>
/// A point light source.
/// <remarks>Rendered in debug mode to visualize its position in the scene.</remarks> 
/// </summary>
public sealed class PointLight : Entity
#if DEBUG
    , IDisposable
#endif
{
    public Vector3 Position
    {
        get;
    }

    public Vector4 Color
    {
        get;
    }

#if DEBUG
    public const int SphereMeshPrecision = 16;

    private readonly Mesh _mesh;
    private readonly ConstantBuffer<ConstantBufferModel> _constantBufferModel;
    private readonly ConstantBuffer<ConstantBufferSurfaceColor> _constantBufferSurfaceColor;
#endif

    public PointLight(Vector3 position, Vector4 color)
    {
        Position = position;
        Color = color;
#if DEBUG
        _mesh = MeshGenerator.CreateSphereMesh(SphereMeshPrecision);
        Transform transform = new() { Position = position, Scale = 0.05f };
        _constantBufferModel = new ConstantBuffer<ConstantBufferModel>();
        _constantBufferModel.Update(new ConstantBufferModel
        {
            Model = Matrix4x4.Transpose(transform.ModelMatrix), ModelInvT = transform.InvModelMatrix,
        });

        _constantBufferSurfaceColor = new ConstantBuffer<ConstantBufferSurfaceColor>();
        _constantBufferSurfaceColor.Update(new ConstantBufferSurfaceColor { SurfaceColor = Color });
#endif
    }

#if DEBUG
    public override void Render(Camera camera)
    {
        var shader = GI.Instance.ShaderManager.GetShader(ShaderManager.ShaderType.Unlit);
        shader.Use();

        _constantBufferModel.Bind();
        _constantBufferSurfaceColor.Bind(2);

        _mesh.Bind();
        GI.Instance.Context.DrawIndexed((uint)_mesh.IndexCount, 0, 0);
        _mesh.Unbind();
    }

    public void Dispose()
    {
        _mesh.Dispose();
        _constantBufferModel.Dispose();
        _constantBufferSurfaceColor.Dispose();
    }
#endif
}