using System.Diagnostics;
using System.Numerics;

using GK2PUMA.Graphics;

using Vortice.Direct3D11;

namespace GK2PUMA.Entities;

public class ReflectiveQuad : Quad, IDisposable
{
    public Action? RenderScene = null;
    public readonly MirrorTransform MirrorTransform;

    private readonly ConstantBuffer<ConstantBufferClipPlane> _clipPlaneBuffer = new();

    public ReflectiveQuad()
    {
        MirrorTransform = new(Transform);
        _clipPlaneBuffer.Update(new ConstantBufferClipPlane { ClipPlane = new Vector4(0, 0, 0, 1) });
    }

    public void Dispose() => _clipPlaneBuffer.Dispose();

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
        _clipPlaneBuffer.Bind(4);

        DrawReflectedScene(camera);
        GI.Instance.Context.ClearDepthStencilView(GI.Instance.DepthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);

        unsafe
        {
            GI.Instance.Context.OMSetBlendState(GI.Instance.BlendStateAlpha, null, 0xFFFFFFFF);
        }
        GI.Instance.Context.RSSetState(GI.Instance.RasterizerStateNoCull);
        _constantBufferModel.Bind();
        _constantBufferSurfaceColor.Bind(2);
        GI.Instance.LightManager.Bind(3);
        _mesh.Bind();
        GI.Instance.Context.DrawIndexed((uint)_mesh.IndexCount, 0, 0);
        _mesh.Unbind();
        GI.Instance.Context.RSSetState(null);
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
        GI.Instance.Context.RSSetState(GI.Instance.RasterizerStateNoCull);
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

        Vector3 worldOrigin = Vector3.Transform(Vector3.Zero, Transform.ModelMatrix);
        Vector3 worldNormal = Vector3.Normalize(Vector3.TransformNormal(new Vector3(0, 0, -1), Transform.ModelMatrix));
        float planeD = -Vector3.Dot(worldNormal, worldOrigin);
        float cameraSide = Vector3.Dot(camera.Position, worldNormal) + planeD;
        Vector4 clipPlane = cameraSide >= 0
            ? new Vector4(worldNormal, planeD)
            : new Vector4(-worldNormal, -planeD);
        _clipPlaneBuffer.Update(new ConstantBufferClipPlane { ClipPlane = clipPlane });

        RenderScene.Invoke();

        _clipPlaneBuffer.Update(new ConstantBufferClipPlane { ClipPlane = new Vector4(0, 0, 0, 1) });

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