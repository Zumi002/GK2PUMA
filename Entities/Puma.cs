// #define DANCE

using System.Numerics;

using GK2PUMA.Entities.PumaParser;
using GK2PUMA.Graphics;

using Silk.NET.Input;

namespace GK2PUMA.Entities;

public sealed class Puma : Entity, IDisposable
{
    public const float RotationSpeed = 0.01f;

    private static readonly Vector3[] PivotPoints =
    [
        new(0.0f, 0.0f, 0.0f),
        new(0.0f, 0.27f, 0.0f),
        new(0.0f, 0.27f, 0.0f),
        new(-0.91f, 0.27f, -0.26f),
        new(-2.05f, 0.27f, -0.26f),
        new(-1.72f, 0.27f, -0.26f)
    ];

    public const int PartCount = 6;

    public Vector4 Color
    {
        get;
    } = new(0.7f, 0.5f, 0.3f, 1.0f);

    public Transform Transform => Transforms[0];
    public readonly Transform[] Transforms = new Transform[PartCount];

    private readonly Mesh[] _meshes = new Mesh[PartCount];

    private readonly ConstantBuffer<ConstantBufferModel>[] _constantBufferModel =
        new ConstantBuffer<ConstantBufferModel>[PartCount];

    private readonly ConstantBuffer<ConstantBufferSurfaceColor> _constantBufferSurfaceColor = new();

    public Puma(string meshFolder = "Meshes")
    {
        for (int i = 0; i < PartCount; i++)
        {
            var part = new PumaPart();
            part.Load(Path.Combine(meshFolder, $"mesh{i + 1}.txt"));
            _meshes[i] = part.BuildMesh(PivotPoints[i]);

            Transforms[i] = new Transform();
            if (i > 0)
            {
                Transforms[i].Position = PivotPoints[i] - PivotPoints[i - 1];
            }

            _constantBufferModel[i] = new ConstantBuffer<ConstantBufferModel>();
        }

        _constantBufferSurfaceColor.Update(new ConstantBufferSurfaceColor { SurfaceColor = Color });
    }

    public override void Render(Camera camera)
    {
        var shader = GI.Instance.ShaderManager.GetShader(ShaderManager.ShaderType.BlinnPhong);
        shader.Use();

        _constantBufferSurfaceColor.Bind(2);
        GI.Instance.LightManager.Bind(3);

        var chainedMatrix = Transforms[0].ModelMatrix;
        for (int i = 0; i < PartCount; i++)
        {
            if (i > 0)
            {
                chainedMatrix = Transforms[i].ModelMatrix * chainedMatrix;
            }

            Matrix4x4.Invert(chainedMatrix, out var invChained);
            _constantBufferModel[i].Update(new ConstantBufferModel
            {
                Model = chainedMatrix, 
                ModelInv = invChained,
            });

            _constantBufferModel[i].Bind();
            _meshes[i].Bind();
            GI.Instance.Context.DrawIndexed((uint)_meshes[i].IndexCount, 0, 0);
        }

        _meshes[PartCount - 1].Unbind();
    }

    public override void Update(float deltaTime)
    {
#if DANCE 
        Transforms[1].Rotation += new Vector3(0.0f, 0.025f, 0.0f);
        Transforms[2].Rotation += new Vector3(0.0f, 0.0f, 0.025f);
        Transforms[3].Rotation += new Vector3(0.0f, 0.0f, 0.025f);
        Transforms[4].Rotation += new Vector3(0.025f, 0.0f, 0.0f);
        Transforms[5].Rotation += new Vector3(0.0f, 0.0f, 0.025f);
#endif
    }

    public override void HandleInput(IKeyboard keyboard, IMouse mouse, float dt)
    {
        if (keyboard.IsKeyPressed(Key.R))
        {
            Transforms[1].Rotation += new Vector3(0.0f, RotationSpeed, 0.0f);
        }
        
        if (keyboard.IsKeyPressed(Key.T))
        {
            Transforms[2].Rotation += new Vector3(0.0f, 0.0f, RotationSpeed);
        }
        
        if (keyboard.IsKeyPressed(Key.Y))
        {
            Transforms[3].Rotation += new Vector3(0.0f, 0.0f, RotationSpeed);
        }
        
        if (keyboard.IsKeyPressed(Key.U))
        {
            Transforms[4].Rotation += new Vector3(RotationSpeed, 0.0f, 0.0f);
        }
        
        if (keyboard.IsKeyPressed(Key.I))
        {
            Transforms[5].Rotation += new Vector3(0.0f, 0.0f, RotationSpeed);
        }
        
        if (keyboard.IsKeyPressed(Key.F))
        {
            Transforms[1].Rotation -= new Vector3(0.0f, RotationSpeed, 0.0f);
        }
        
        if (keyboard.IsKeyPressed(Key.G))
        {
            Transforms[2].Rotation -= new Vector3(0.0f, 0.0f, RotationSpeed);
        }
        
        if (keyboard.IsKeyPressed(Key.H))
        {
            Transforms[3].Rotation -= new Vector3(0.0f, 0.0f, RotationSpeed);
        }
        
        if (keyboard.IsKeyPressed(Key.J))
        {
            Transforms[4].Rotation -= new Vector3(RotationSpeed, 0.0f, 0.0f);
        }
        
        if (keyboard.IsKeyPressed(Key.K))
        {
            Transforms[5].Rotation -= new Vector3(0.0f, 0.0f, RotationSpeed);
        }
    }

    public void Dispose()
    {
        for (int i = 0; i < PartCount; i++)
        {
            _meshes[i].Dispose();
            _constantBufferModel[i].Dispose();
        }

        _constantBufferSurfaceColor.Dispose();
    }
}