using System.Numerics;

using GK2PUMA.Graphics;

namespace GK2PUMA.Entities;

public sealed class Cylinder : Entity, IDisposable
{
    public const int CylinderLidMeshPrecision = 256;
    public const int CylinderWidthMeshPrecision = 256;

    private readonly Mesh _mesh;
    private readonly ConstantBuffer<ConstantBufferModel> _constantBufferModel;
    private readonly ConstantBuffer<ConstantBufferSurfaceColor> _constantBufferSurfaceColor;
    private bool _constantBufferModelIsDirty = true;

    public readonly Transform Transform = new();

    public Vector4 Color
    {
        get;
    }

    public Cylinder(Vector3 position, Vector4 color)
    {
        Color = color;
        _mesh = MeshGenerator.CreateCylinderMesh(CylinderLidMeshPrecision, CylinderWidthMeshPrecision);
        Transform.Position = position;
        _constantBufferModel = new ConstantBuffer<ConstantBufferModel>();
        _constantBufferModel.Update(
            new ConstantBufferModel { Model = Transform.ModelMatrix, ModelInv = Transform.InvModelMatrix, }
        );
        _constantBufferSurfaceColor = new ConstantBuffer<ConstantBufferSurfaceColor>();
        _constantBufferSurfaceColor.Update(new ConstantBufferSurfaceColor { SurfaceColor = Color });
        Transform.OnMatricesRecalculated += _ => _constantBufferModelIsDirty = true;
    }

    public override void Render(Camera camera)
    {
        var shader = GI.Instance.ShaderManager.GetShader(ShaderManager.ShaderType.BlinnPhong);
        shader.Use();

        if (_constantBufferModelIsDirty)
        {
            _constantBufferModel.Update(
                new ConstantBufferModel { Model = Transform.ModelMatrix, ModelInv = Transform.InvModelMatrix, }
            );
            _constantBufferModelIsDirty = false;
        }

        _constantBufferModel.Bind(0);
        _constantBufferSurfaceColor.Bind(2);
        GI.Instance.LightManager.Bind(3);
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
}