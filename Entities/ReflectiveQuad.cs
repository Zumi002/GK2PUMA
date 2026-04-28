using System.Diagnostics;
using System.Numerics;

using GK2PUMA.Graphics;

using Vortice.Direct3D11;

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
        
        DrawReflectedScene(camera);
        GI.Instance.Context.ClearDepthStencilView(GI.Instance.DepthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);

        unsafe
        {
            GI.Instance.Context.OMSetBlendState(GI.Instance.BlendStateAlpha, null, 0xFFFFFFFF);
        }
        _constantBufferModel.Bind();
        _constantBufferSurfaceColor.Bind(2);
        GI.Instance.LightManager.Bind(3);
        _mesh.Bind();
        GI.Instance.Context.DrawIndexed((uint)_mesh.IndexCount, 0, 0);
        _mesh.Unbind();
        unsafe
        {
            GI.Instance.Context.OMSetBlendState(null, null, 0xFFFFFFFF);
        }
    }

    private void DrawReflectedScene(Camera camera)
    {
        if (RenderScene is null)
        {
            Debug.WriteLine("RenderScene is null; did you forget to set it?");
            return;
        }
       
        // set up the stencil buffer for mirror drawing
        unsafe
        {
            GI.Instance.Context.OMSetBlendState(GI.Instance.BlendStateNoColor, null, 0xFFFFFFFF);
        }
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
        unsafe
        {
            GI.Instance.Context.OMSetBlendState(null, null, 0xFFFFFFFF);
        }
        
        // draw the mirrored scene
        GI.Instance.Context.ClearDepthStencilView(GI.Instance.DepthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);
        GI.Instance.Context.OMSetDepthStencilState(GI.Instance.DepthStencilStateTest, 1);
        GI.Instance.Context.RSSetState(GI.Instance.RasterizerStateCounterClockWise);
        Matrix4x4 mirror = MirrorTransform.MirrorMatrix;
        Matrix4x4 modifiedView = mirror * camera.ViewMatrix;
        camera.UpdateAndBindViewProjBuffer(modifiedView);
        RenderScene.Invoke();

        // reset the stencil buffer
        _constantBufferSurfaceColor.Update(new ConstantBufferSurfaceColor
        {
            SurfaceColor = Color,
        });
        GI.Instance.Context.OMSetDepthStencilState(null);
        GI.Instance.Context.RSSetState(null);
        camera.UpdateAndBindViewProjBuffer();
    }
}