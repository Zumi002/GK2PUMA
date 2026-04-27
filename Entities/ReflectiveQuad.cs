using System.Diagnostics;
using System.Numerics;

using GK2PUMA.Graphics;

namespace GK2PUMA.Entities;

public class ReflectiveQuad : Quad
{
    public Action? RenderScene = null;
    public readonly MirrorTransform MirrorTransform;

    public ReflectiveQuad()
    {
        MirrorTransform = new(Transform);
    }

    public override void Render(Camera camera)
    {
        var shader = GI.Instance.ShaderManager.GetShader(ShaderManager.ShaderType.BlinnPhong);
        shader.Use();

        if (_constantBufferModelIsDirty)
        {
            _constantBufferModel.Update(new ConstantBufferModel
            {
                Model = Transform.ModelMatrix, ModelInv = Transform.InvModelMatrix,
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
        
        DrawReflectedScene(camera);
    }

    private void DrawReflectedScene(Camera camera)
    {
        if (RenderScene is null)
        {
            Debug.WriteLine("RenderScene is null; did you forget to set it?");
            return;
        }
       
        // set up the stencil buffer for mirror drawing
        GI.Instance.Context.OMSetDepthStencilState(GI.Instance.DepthStencilStateWrite, 1);
        Vector4 white = new(1, 1, 1, 1);
        _constantBufferSurfaceColor.Update(new ConstantBufferSurfaceColor
        {
            SurfaceColor = white,
        });
        _constantBufferModel.Bind(0);
        _constantBufferSurfaceColor.Bind(2);
        GI.Instance.LightManager.Bind(3);
        _mesh.Bind();
        GI.Instance.Context.DrawIndexed((uint)_mesh.IndexCount, 0, 0);
        _mesh.Unbind();
        
        // draw the mirrored scene
        GI.Instance.Context.OMSetDepthStencilState(GI.Instance.DepthStencilStateTest, 1);
        GI.Instance.Context.RSSetState(GI.Instance.RasterizerStateCounterClockWise);
        Matrix4x4 mirror = MirrorTransform.MirrorMatrix;
        Matrix4x4 modifiedView = mirror * camera.ViewMatrix;
        camera.UpdateAndBindViewProjBuffer(modifiedView);
        RenderScene.Invoke();

        _constantBufferSurfaceColor.Update(new ConstantBufferSurfaceColor
        {
            SurfaceColor = Color,
        });
        GI.Instance.Context.OMSetDepthStencilState(null);
        GI.Instance.Context.RSSetState(null);
        camera.UpdateAndBindViewProjBuffer();
    }
}