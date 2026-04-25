using System.Numerics;

using GK2PUMA.Graphics;

namespace GK2PUMA.Entities;

public class Quad : Entity
{
    private readonly Mesh _mesh;
    private readonly ConstantBuffer<ConstantBufferModel> _constantBufferModel;
    private readonly ConstantBuffer<ConstantBufferSurfaceColor> _constantBufferSurfaceColor;
    private bool _constantBufferModelIsDirty = true;

    public readonly Transform Transform = new Transform();
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
        _constantBufferModel = new ConstantBuffer<ConstantBufferModel>();
        _constantBufferSurfaceColor = new ConstantBuffer<ConstantBufferSurfaceColor>();
        Transform.OnMatricesRecalculated += _ => _constantBufferModelIsDirty = true;
    }

    public override void Render(Camera camera)
    {
        var shader = GI.Instance.ShaderManager.GetShader(ShaderManager.ShaderType.BlinnPhong);
        shader.Use();

        if (_constantBufferModelIsDirty)
        {
            _constantBufferModel.Update(new ConstantBufferModel
            {
                Model = Transform.ModelMatrix, 
                ModelInv = Transform.InvModelMatrix,
            });
            _constantBufferModelIsDirty = false;
        }

        _constantBufferSurfaceColor.Update(new ConstantBufferSurfaceColor { SurfaceColor = Color });

        _constantBufferModel.Bind(0);
        _constantBufferSurfaceColor.Bind(2);
        GI.Instance.LightManager.Bind(3);
        _mesh.Bind();

        GI.Instance.Context.DrawIndexed((uint)_mesh.IndexCount, 0, 0);

        _mesh.Unbind();
    }
}