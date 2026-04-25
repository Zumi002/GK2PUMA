using System.Numerics;

using GK2PUMA.Graphics;

namespace GK2PUMA.Entities;

public class InsideCube : Entity
{
    private readonly Mesh _mesh;
    private readonly ConstantBuffer<ConstantBufferModel> _constantBufferModel;
    private readonly ConstantBuffer<ConstantBufferSurfaceColor> _constantBufferSurfaceColor;
    private bool _constantBufferModelIsDirty = true;

    public readonly Transform Transform = new();
    public Vector4 Color
    {
        get;
    } = new (0.3f, 0.3f, 0.3f, 1.0f);
    public const float HalfSize = 5.0f;

    public InsideCube()
    {
        var vertices = new Vertex[]
        {
            new (new (-5, -5,  5), new (0, 0, -1)),
            new (new (-5,  5,  5), new (0, 0, -1)),
            new (new ( 5,  5,  5), new (0, 0, -1)),
            new (new ( 5, -5,  5), new (0, 0, -1)),

            new (new (-5, -5, -5), new (0, 0, 1)),
            new (new ( 5, -5, -5), new (0, 0, 1)),
            new (new ( 5,  5, -5), new (0, 0, 1)),
            new (new (-5,  5, -5), new (0, 0, 1)),

            new (new (-5, -5, -5), new (1, 0, 0)),
            new (new (-5,  5, -5), new (1, 0, 0)),
            new (new (-5,  5,  5), new (1, 0, 0)),
            new (new (-5, -5,  5), new (1, 0, 0)),

            new (new ( 5, -5, -5), new (-1, 0, 0)),
            new (new ( 5, -5,  5), new (-1, 0, 0)),
            new (new ( 5,  5,  5), new (-1, 0, 0)),
            new (new ( 5,  5, -5), new (-1, 0, 0)),

            new (new (-5,  5, -5), new (0, -1, 0)),
            new (new ( 5,  5, -5), new (0, -1, 0)),
            new (new ( 5,  5,  5), new (0, -1, 0)),
            new (new (-5,  5,  5), new (0, -1, 0)),

            new (new (-5, -5, -5), new (0, 1, 0)),
            new (new (-5, -5,  5), new (0, 1, 0)),
            new (new ( 5, -5,  5), new (0, 1, 0)),
            new (new ( 5, -5, -5), new (0, 1, 0))
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

        _mesh = new (vertices, indices);
        _constantBufferModel = new ConstantBuffer<ConstantBufferModel>();
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
            _constantBufferModel.Update(new ConstantBufferModel
            {
                Model = Matrix4x4.Transpose(Transform.ModelMatrix), 
                ModelInvT = Transform.InvModelMatrix,
            });
            _constantBufferModelIsDirty = false;
        }
        
        _constantBufferModel.Bind(0);
        _constantBufferSurfaceColor.Bind(2);
        GI.Instance.LightManager.Bind(3);
        _mesh.Bind();

        GI.Instance.Context.DrawIndexed((uint)_mesh.IndexCount, 0, 0);

        _mesh.Unbind();
    }
}