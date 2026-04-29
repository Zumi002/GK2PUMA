using System.Numerics;

using GK2PUMA.Entities;

using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.Mathematics;

namespace GK2PUMA.Graphics;

public struct OpaqueCommand
{
    public Mesh Mesh;
    public Matrix4x4 Transform;
    public Matrix4x4 InvTransform;
    public Vector4 SurfaceColor;
    public ID3D11ShaderResourceView? Texture;
    public bool CastsShadows;
}

public struct MirrorCommand
{
    public Mesh Mesh;
    public Matrix4x4 Transform;
    public Matrix4x4 InvTransform;
    public Vector4 SurfaceColor;
    public ID3D11ShaderResourceView? Texture;
    public bool CastsShadows;
}

public struct ParticleCommand
{
    public Mesh Mesh;
    public ID3D11Buffer InstanceBuffer;
    public int InstanceCount;
    public ID3D11ShaderResourceView? Texture;
}

public class RenderingPipeline : IDisposable
{
    private readonly List<MirrorCommand> _mirrors = new();
    private readonly List<OpaqueCommand> _opaques = new();
    private readonly List<ParticleCommand> _particles = new();

    private ConstantBuffer<ConstantBufferModel>? _modelBuffer;
    private ConstantBuffer<ConstantBufferSurfaceColor>? _colorBuffer;
    private ConstantBuffer<ConstantBufferClipPlane>? _clipPlaneBuffer;

    private ID3D11DepthStencilState? _defaultDepthState;
    private ID3D11DepthStencilState? _noDepthState;
    private ID3D11RasterizerState? _cullBackState;
    private ID3D11RasterizerState? _cullFrontState;
    private ID3D11DepthStencilState? _shadowVolumeDepthState;
    private ID3D11DepthStencilState? _lightPassDepthState;
    private ID3D11RasterizerState? _cullNoneState;
    private ID3D11BlendState? _noColorWriteBlendState;
    private ID3D11BlendState? _additiveBlendState;
    private ID3D11BlendState? _alphaBlendState;

    // Mirror-specific states
    private ID3D11DepthStencilState? _mirrorStencilWriteState;
    private ID3D11DepthStencilState? _mirrorGPassDepthState;

    private Camera? _mirrorCamera;

    private static readonly Vector4 NoClipPlane = new(0, 0, 0, 1);

    public void Init()
    {
        var device = GI.Instance.Device;

        var depthDesc = new DepthStencilDescription
        {
            DepthEnable = true,
            DepthWriteMask = DepthWriteMask.All,
            DepthFunc = ComparisonFunction.LessEqual,
            StencilEnable = false
        };
        _defaultDepthState = device.CreateDepthStencilState(depthDesc);

        var noDepthDesc = new DepthStencilDescription
        {
            DepthEnable = false,
            DepthWriteMask = DepthWriteMask.Zero,
            DepthFunc = ComparisonFunction.Always,
            StencilEnable = false
        };
        _noDepthState = device.CreateDepthStencilState(noDepthDesc);

        var cullBackDesc = new RasterizerDescription
        {
            CullMode = CullMode.Back,
            FillMode = FillMode.Solid,
            FrontCounterClockwise = false,
            DepthClipEnable = true
        };
        _cullBackState = device.CreateRasterizerState(cullBackDesc);

        var cullFrontDesc = new RasterizerDescription
        {
            CullMode = CullMode.Front,
            FillMode = FillMode.Solid,
            FrontCounterClockwise = false,
            DepthClipEnable = true
        };

        var shadowDepthDesc = new DepthStencilDescription
        {
            DepthEnable = true,
            DepthWriteMask = DepthWriteMask.Zero,
            DepthFunc = ComparisonFunction.Less,
            StencilEnable = true,
            StencilReadMask = 0xFF,
            StencilWriteMask = 0xFF,
            FrontFace = new DepthStencilOperationDescription
            {
                StencilFailOp = StencilOperation.Keep,
                StencilDepthFailOp = StencilOperation.Decrement,
                StencilPassOp = StencilOperation.Keep,
                StencilFunc = ComparisonFunction.Always
            },
            BackFace = new DepthStencilOperationDescription
            {
                StencilFailOp = StencilOperation.Keep,
                StencilDepthFailOp = StencilOperation.Increment,
                StencilPassOp = StencilOperation.Keep,
                StencilFunc = ComparisonFunction.Always
            }
        };
        _shadowVolumeDepthState = device.CreateDepthStencilState(shadowDepthDesc);

        var cullNoneDesc = new RasterizerDescription
        {
            CullMode = CullMode.None,
            FillMode = FillMode.Solid,
            FrontCounterClockwise = false,
            DepthClipEnable = false
        };
        _cullNoneState = device.CreateRasterizerState(cullNoneDesc);

        var noColorBlendDesc = new BlendDescription();
        noColorBlendDesc.RenderTarget[0].RenderTargetWriteMask = ColorWriteEnable.None;
        _noColorWriteBlendState = device.CreateBlendState(noColorBlendDesc);

        var lightPassDepthDesc = new DepthStencilDescription
        {
            DepthEnable = false,
            DepthWriteMask = DepthWriteMask.Zero,
            DepthFunc = ComparisonFunction.Always,
            StencilEnable = true,
            StencilReadMask = 0xFF,
            StencilWriteMask = 0x00,
            FrontFace = new DepthStencilOperationDescription
            {
                StencilFailOp = StencilOperation.Keep,
                StencilDepthFailOp = StencilOperation.Keep,
                StencilPassOp = StencilOperation.Keep,
                StencilFunc = ComparisonFunction.Equal
            },
            BackFace = new DepthStencilOperationDescription
            {
                StencilFailOp = StencilOperation.Keep,
                StencilDepthFailOp = StencilOperation.Keep,
                StencilPassOp = StencilOperation.Keep,
                StencilFunc = ComparisonFunction.Equal
            }
        };
        _lightPassDepthState = device.CreateDepthStencilState(lightPassDepthDesc);

        var additiveBlendDesc = new BlendDescription();
        additiveBlendDesc.RenderTarget[0] = new RenderTargetBlendDescription
        {
            BlendEnable = true,
            SourceBlend = Blend.One,
            DestinationBlend = Blend.One,
            BlendOperation = BlendOperation.Add,
            SourceBlendAlpha = Blend.Zero,
            DestinationBlendAlpha = Blend.One,
            BlendOperationAlpha = BlendOperation.Add,
            RenderTargetWriteMask = ColorWriteEnable.All
        };
        _additiveBlendState = device.CreateBlendState(additiveBlendDesc);

        var alphaBlendDesc = new BlendDescription();
        alphaBlendDesc.RenderTarget[0] = new RenderTargetBlendDescription
        {
            BlendEnable = true,
            SourceBlend = Blend.SourceAlpha,
            DestinationBlend = Blend.InverseSourceAlpha,
            BlendOperation = BlendOperation.Add,
            SourceBlendAlpha = Blend.One,
            DestinationBlendAlpha = Blend.Zero,
            BlendOperationAlpha = BlendOperation.Add,
            RenderTargetWriteMask = ColorWriteEnable.All
        };
        _alphaBlendState = device.CreateBlendState(alphaBlendDesc);

        // Depth test read-only + stencil write: marks mirror area (StencilPassOp = Replace)
        var mirrorStencilWriteDesc = new DepthStencilDescription
        {
            DepthEnable = true,
            DepthWriteMask = DepthWriteMask.Zero,
            DepthFunc = ComparisonFunction.LessEqual,
            StencilEnable = true,
            StencilReadMask = 0xFF,
            StencilWriteMask = 0xFF,
            FrontFace = new DepthStencilOperationDescription
            {
                StencilFailOp = StencilOperation.Keep,
                StencilDepthFailOp = StencilOperation.Keep,
                StencilPassOp = StencilOperation.Replace,
                StencilFunc = ComparisonFunction.Always
            },
            BackFace = new DepthStencilOperationDescription
            {
                StencilFailOp = StencilOperation.Keep,
                StencilDepthFailOp = StencilOperation.Keep,
                StencilPassOp = StencilOperation.Replace,
                StencilFunc = ComparisonFunction.Always
            }
        };
        _mirrorStencilWriteState = device.CreateDepthStencilState(mirrorStencilWriteDesc);

        // Depth test + write, stencil test == ref (render only inside mirror area)
        var mirrorGPassDepthDesc = new DepthStencilDescription
        {
            DepthEnable = true,
            DepthWriteMask = DepthWriteMask.All,
            DepthFunc = ComparisonFunction.LessEqual,
            StencilEnable = true,
            StencilReadMask = 0xFF,
            StencilWriteMask = 0x00,
            FrontFace = new DepthStencilOperationDescription
            {
                StencilFailOp = StencilOperation.Keep,
                StencilDepthFailOp = StencilOperation.Keep,
                StencilPassOp = StencilOperation.Keep,
                StencilFunc = ComparisonFunction.Equal
            },
            BackFace = new DepthStencilOperationDescription
            {
                StencilFailOp = StencilOperation.Keep,
                StencilDepthFailOp = StencilOperation.Keep,
                StencilPassOp = StencilOperation.Keep,
                StencilFunc = ComparisonFunction.Equal
            }
        };
        _mirrorGPassDepthState = device.CreateDepthStencilState(mirrorGPassDepthDesc);

        _cullFrontState = device.CreateRasterizerState(cullFrontDesc);
        _modelBuffer = new();
        _colorBuffer = new();
        _clipPlaneBuffer = new();
        _mirrorCamera = new Camera(1.0f);
    }

    public void SubmitOpaque(Mesh mesh, Matrix4x4 transform, Matrix4x4 invTransform, Vector4 color, ID3D11ShaderResourceView? texture = null, bool castsShadows = true)
    {
        _opaques.Add(
            new OpaqueCommand {
                Mesh = mesh,
                Transform = transform,
                InvTransform = invTransform,
                SurfaceColor = color,
                Texture = texture,
                CastsShadows = castsShadows
            }
        );
    }

    public void SubmitMirror(Mesh mesh, Matrix4x4 transform, Matrix4x4 invTransform, Vector4 color, ID3D11ShaderResourceView? texture = null, bool castsShadows = true)
    {
        _mirrors.Add(
            new MirrorCommand {
                Mesh = mesh,
                Transform = transform,
                InvTransform = invTransform,
                SurfaceColor = color,
                Texture = texture,
                CastsShadows = castsShadows
            }
        );
    }

    public void SubmitParticle(Mesh mesh, ID3D11Buffer instanceBuffer, int instanceCount, ID3D11ShaderResourceView? texture = null)
    {
        _particles.Add(
            new ParticleCommand {
                Mesh = mesh,
                InstanceBuffer = instanceBuffer,
                InstanceCount = instanceCount,
                Texture = texture
            }
        );
    }

    public void Execute(Camera mainCamera)
    {
        var context = GI.Instance.Context;

        // Clear main RT once here (not per-pass)
        context.OMSetRenderTargets(GI.Instance.RenderTargetView, (ID3D11DepthStencilView?)null);
        context.ClearRenderTargetView(GI.Instance.RenderTargetView, new Color4(0.1f, 0.1f, 0.1f, 1.0f));

        // Ensure no clip for the main G-Pass
        _clipPlaneBuffer.Update(new ConstantBufferClipPlane { ClipPlane = NoClipPlane });
        _clipPlaneBuffer.Bind(4);

        // Main scene
        context.ClearDepthStencilView(
            GraphicsContext.Instance.DepthStencilView,
            DepthStencilClearFlags.Stencil,
            1.0f,
            0
        );

        RenderGPass(context, mainCamera);
        RenderShadowVolume(context, mainCamera);
        RenderLightPass(context, mainCamera);
        RenderParticles(context, mainCamera);

        // Mirror passes — run after main scene so we can read main-scene depth for stencil
        foreach (var mirrorCommand in _mirrors)
        {
            // Clear stencil but keep depth from main G-Pass (used in stencil pass below)
            context.ClearDepthStencilView(
                GraphicsContext.Instance.DepthStencilView,
                DepthStencilClearFlags.Stencil,
                1.0f,
                0
            );

            // Stencil pass uses main camera — ensure it's bound
            mainCamera.UpdateAndBindViewProjBuffer();

            RenderMirrorStencilPass(context, mirrorCommand);

            // Clear depth so mirror camera can render the reflected scene
            context.ClearDepthStencilView(
                GraphicsContext.Instance.DepthStencilView,
                DepthStencilClearFlags.Depth,
                1.0f,
                0
            );

            _mirrorCamera.UpdateAsMirror(mainCamera, mirrorCommand.Transform);
            _mirrorCamera.UpdateAndBindViewProjBuffer();

            RenderMirrorGPass(context, _mirrorCamera, mainCamera, mirrorCommand);
            RenderMirrorShadowVolume(context, _mirrorCamera);
            RenderMirrorLightPass(context, _mirrorCamera);
            RenderMirrorParticles(context, _mirrorCamera);
            RenderMirrorSurface(context, mainCamera, mirrorCommand);
        }

        ClearQueues();
    }

    private void RenderMirrorStencilPass(ID3D11DeviceContext context, MirrorCommand mirrorCommand)
    {
        context.OMSetRenderTargets(GI.Instance.GBufferRTVs, GI.Instance.DepthStencilView);
        context.OMSetBlendState(_noColorWriteBlendState);
        context.OMSetDepthStencilState(_mirrorStencilWriteState, 1);
        context.RSSetState(_cullNoneState);

        var gPassShader = GI.Instance.ShaderManager.GetShader(ShaderManager.ShaderType.GPass);
        gPassShader.Use();

        _modelBuffer.Update(new ConstantBufferModel
        {
            Model = mirrorCommand.Transform,
            ModelInv = mirrorCommand.InvTransform
        });
        _modelBuffer.Bind(0);

        context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        mirrorCommand.Mesh.Bind();
        context.DrawIndexed((uint)mirrorCommand.Mesh.IndexCount, 0, 0);
        mirrorCommand.Mesh.Unbind();

        context.OMSetBlendState(null);
    }

    private void RenderMirrorGPass(ID3D11DeviceContext context, Camera mirrorCamera, Camera mainCamera, MirrorCommand mirrorCommand)
    {
        // Compute clip plane from mirror transform and main camera position
        var mt = mirrorCommand.Transform;
        Vector3 worldOrigin = new(mt.M41, mt.M42, mt.M43);
        Vector3 worldNormal = Vector3.Normalize(new Vector3(-mt.M31, -mt.M32, -mt.M33));
        float planeD = -Vector3.Dot(worldNormal, worldOrigin);
        float cameraSide = Vector3.Dot(mainCamera.Position, worldNormal) + planeD;
        Vector4 clipPlane = cameraSide >= 0
            ? new Vector4(worldNormal, planeD)
            : new Vector4(-worldNormal, -planeD);

        _clipPlaneBuffer.Update(new ConstantBufferClipPlane { ClipPlane = clipPlane });
        _clipPlaneBuffer.Bind(4);

        context.RSSetState(_cullFrontState);
        context.OMSetDepthStencilState(_mirrorGPassDepthState, 1);

        context.OMSetRenderTargets(GI.Instance.GBufferRTVs, GI.Instance.DepthStencilView);

        var clearColor = new Color4(0.0f, 0.0f, 0.0f, 0.0f);
        context.ClearRenderTargetView(GI.Instance.GBufferRTVs[0], clearColor);
        context.ClearRenderTargetView(GI.Instance.GBufferRTVs[1], clearColor);
        context.ClearRenderTargetView(GI.Instance.GBufferRTVs[2], clearColor);

        var gPassShader = GI.Instance.ShaderManager.GetShader(ShaderManager.ShaderType.GPass);
        gPassShader.Use();

        _modelBuffer.Bind(0);
        _colorBuffer.Bind(2);
        context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);

        foreach (var cmd in _opaques)
        {
            _modelBuffer.Update(new ConstantBufferModel
            {
                Model = cmd.Transform,
                ModelInv = cmd.InvTransform
            });
            _colorBuffer.Update(new ConstantBufferSurfaceColor { SurfaceColor = cmd.SurfaceColor });

            if (cmd.Texture != null)
                context.PSSetShaderResources(0, new[] { cmd.Texture });

            cmd.Mesh.Bind();
            context.DrawIndexed((uint)cmd.Mesh.IndexCount, 0, 0);
        }

        // Reset clip plane to no-clip for subsequent passes
        _clipPlaneBuffer.Update(new ConstantBufferClipPlane { ClipPlane = NoClipPlane });
        _clipPlaneBuffer.Bind(4);
    }

    private void RenderMirrorShadowVolume(ID3D11DeviceContext context, Camera mirrorCamera)
    {
        // Shadow volumes in the mirror are left for a future iteration.
        // Without this pass the reflection is fully lit (no shadows in mirror).
    }

    private void RenderMirrorLightPass(ID3D11DeviceContext context, Camera mirrorCamera)
    {
        context.RSSetState(_cullBackState);

        context.OMSetRenderTargets(GI.Instance.RenderTargetView, GI.Instance.DepthStencilView);

        // Ambient sub-pass: stencil test == 1 (mirror area only)
        context.OMSetDepthStencilState(_lightPassDepthState, 1);
        context.OMSetBlendState(null);

        var ambientShader = GI.Instance.ShaderManager.GetShader(ShaderManager.ShaderType.AmbientPass);
        ambientShader.Use();

        context.PSSetShaderResources(0, GI.Instance.GBufferSRVs);
        context.PSSetSamplers(0, new[] { GI.Instance.DefaultSampler });

        context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        context.Draw(3, 0);

        // Light sub-pass: stencil test == 1, additive blend
        context.OMSetDepthStencilState(_lightPassDepthState, 1);
        context.OMSetBlendState(_additiveBlendState);

        var lightPassShader = GI.Instance.ShaderManager.GetShader(ShaderManager.ShaderType.LightPass);
        lightPassShader.Use();

        GI.Instance.LightManager.Bind(3);

        context.Draw(3, 0);

        context.PSSetShaderResources(0, new ID3D11ShaderResourceView[] { null, null, null });
        context.OMSetBlendState(null);
    }

    private void RenderMirrorParticles(ID3D11DeviceContext context, Camera mirrorCamera)
    {

    }

    private void RenderMirrorSurface(ID3D11DeviceContext context, Camera mainCamera, MirrorCommand mirrorCommand)
    {
        mainCamera.UpdateAndBindViewProjBuffer();

        context.OMSetRenderTargets(GI.Instance.RenderTargetView, GI.Instance.DepthStencilView);
        context.OMSetBlendState(_alphaBlendState);
        // No depth test; stencil test == 1 ensures we draw only inside the mirror area
        context.OMSetDepthStencilState(_lightPassDepthState, 1);
        context.RSSetState(_cullNoneState);

        var unlitShader = GI.Instance.ShaderManager.GetShader(ShaderManager.ShaderType.Unlit);
        unlitShader.Use();

        _modelBuffer.Update(new ConstantBufferModel
        {
            Model = mirrorCommand.Transform,
            ModelInv = mirrorCommand.InvTransform
        });
        _modelBuffer.Bind(0);
        _colorBuffer.Update(new ConstantBufferSurfaceColor { SurfaceColor = mirrorCommand.SurfaceColor });
        _colorBuffer.Bind(2);

        var tex = mirrorCommand.Texture ?? GI.Instance.DefaultWhiteTextureSRV;
        context.PSSetShaderResources(0, new[] { tex });
        context.PSSetSamplers(0, new[] { GI.Instance.DefaultSampler });

        context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        mirrorCommand.Mesh.Bind();
        context.DrawIndexed((uint)mirrorCommand.Mesh.IndexCount, 0, 0);
        mirrorCommand.Mesh.Unbind();

        context.OMSetBlendState(null);
        context.OMSetDepthStencilState(null, 0);
        context.RSSetState(null);
    }

    private void RenderGPass(ID3D11DeviceContext context, Camera camera)
    {
        context.RSSetState(_cullBackState);
        context.OMSetDepthStencilState(_defaultDepthState, 0);

        context.OMSetRenderTargets(GI.Instance.GBufferRTVs, GI.Instance.DepthStencilView);

        var clearColor = new Color4(0.0f, 0.0f, 0.0f, 0.0f);
        context.ClearRenderTargetView(GI.Instance.GBufferRTVs[0], clearColor);
        context.ClearRenderTargetView(GI.Instance.GBufferRTVs[1], clearColor);
        context.ClearRenderTargetView(GI.Instance.GBufferRTVs[2], clearColor);
        context.ClearDepthStencilView(GI.Instance.DepthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);

        var gPassShader = GI.Instance.ShaderManager.GetShader(ShaderManager.ShaderType.GPass);
        gPassShader.Use();

        _modelBuffer.Bind(0);
        _colorBuffer.Bind(2);
        context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        foreach (var cmd in _opaques)
        {
            _modelBuffer.Update(new ConstantBufferModel
            {
                Model = cmd.Transform,
                ModelInv = cmd.InvTransform
            });

            _colorBuffer.Update(new ConstantBufferSurfaceColor
            {
                SurfaceColor = cmd.SurfaceColor
            });

            if (cmd.Texture != null)
            {
                context.PSSetShaderResources(0, new[] { cmd.Texture });
            }

            cmd.Mesh.Bind();
            context.DrawIndexed((uint)cmd.Mesh.IndexCount, 0, 0);
        }
    }

    private void RenderLightPass(ID3D11DeviceContext context, Camera camera)
    {
        context.RSSetState(_cullBackState);
        context.OMSetDepthStencilState(_noDepthState, 0);
        context.OMSetBlendState(null);

        context.OMSetRenderTargets(GI.Instance.RenderTargetView, null);
        // Note: main RT is cleared once at the top of Execute(), not here

        var ambientShader = GI.Instance.ShaderManager.GetShader(ShaderManager.ShaderType.AmbientPass);
        ambientShader.Use();

        context.PSSetShaderResources(0, GI.Instance.GBufferSRVs);
        context.PSSetSamplers(0, new[] { GI.Instance.DefaultSampler });

        context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        context.Draw(3, 0);

        context.OMSetDepthStencilState(_lightPassDepthState, 0);
        context.OMSetBlendState(_additiveBlendState);

        context.OMSetRenderTargets(GI.Instance.RenderTargetView, GI.Instance.DepthStencilView);

        var lightPassShader = GI.Instance.ShaderManager.GetShader(ShaderManager.ShaderType.LightPass);
        lightPassShader.Use();

        GI.Instance.LightManager.Bind(3);

        context.Draw(3, 0);

        context.PSSetShaderResources(0, new ID3D11ShaderResourceView[] { null, null, null });
        context.OMSetBlendState(null);
    }

    private void RenderShadowVolume(ID3D11DeviceContext context, Camera camera)
    {
        context.RSSetState(_cullNoneState);
        context.OMSetDepthStencilState(_shadowVolumeDepthState, 0);
        context.OMSetBlendState(_noColorWriteBlendState);

        context.OMSetRenderTargets(GI.Instance.GBufferRTVs, GI.Instance.DepthStencilView);

        var shadowShader = GI.Instance.ShaderManager.GetShader(ShaderManager.ShaderType.ShadowVolume);
        shadowShader.Use();

        _modelBuffer.Bind(0);
        GI.Instance.LightManager.Bind(3);

        context.IASetPrimitiveTopology(PrimitiveTopology.TriangleListAdjacency);

        foreach (var cmd in _opaques.Where(cmd => cmd.CastsShadows))
        {
            _modelBuffer.Update(new ConstantBufferModel
            {
                Model = cmd.Transform,
                ModelInv = cmd.InvTransform
            });

            cmd.Mesh.Bind(useAdjacency: true);
            context.DrawIndexed((uint)cmd.Mesh.AdjacencyIndexCount, 0, 0);
        }

        foreach (var cmd in _mirrors.Where(cmd => cmd.CastsShadows))
        {
            _modelBuffer.Update(new ConstantBufferModel
            {
                Model = cmd.Transform,
                ModelInv = cmd.InvTransform
            });

            cmd.Mesh.Bind(useAdjacency: true);
            context.DrawIndexed((uint)cmd.Mesh.AdjacencyIndexCount, 0, 0);
        }

        context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        context.OMSetBlendState(null);
    }

    private void RenderShadowVolumeDEBUG(ID3D11DeviceContext context, Camera camera)
    {
        context.OMSetRenderTargets(GI.Instance.RenderTargetView, GI.Instance.DepthStencilView);
        context.OMSetBlendState(_additiveBlendState);
        context.OMSetDepthStencilState(_defaultDepthState, 0);

        var shadowShader = GI.Instance.ShaderManager.GetShader(ShaderManager.ShaderType.ShadowVolume);
        shadowShader.Use();

        _modelBuffer.Bind(0);
        _colorBuffer.Bind(2);
        GI.Instance.LightManager.Bind(3);

        context.IASetPrimitiveTopology(PrimitiveTopology.TriangleListAdjacency);

        void DrawVolumes(Vector4 debugColor, ID3D11RasterizerState cullState)
        {
            context.RSSetState(cullState);

            _colorBuffer.Update(new ConstantBufferSurfaceColor
            {
                SurfaceColor = debugColor
            });

            foreach (var cmd in _opaques.Where(cmd => cmd.CastsShadows))
            {
                _modelBuffer.Update(new ConstantBufferModel
                {
                    Model = cmd.Transform,
                    ModelInv = cmd.InvTransform
                });

                cmd.Mesh.Bind(useAdjacency: true);
                context.DrawIndexed((uint)cmd.Mesh.AdjacencyIndexCount, 0, 0);
            }

            foreach (var cmd in _mirrors.Where(cmd => cmd.CastsShadows))
            {
                _modelBuffer.Update(new ConstantBufferModel
                {
                    Model = cmd.Transform,
                    ModelInv = cmd.InvTransform
                });

                cmd.Mesh.Bind(useAdjacency: true);
                context.DrawIndexed((uint)cmd.Mesh.AdjacencyIndexCount, 0, 0);
            }
        }

        // comment one
        DrawVolumes(new Vector4(0.2f, 0.0f, 0.0f, 1.0f), _cullBackState);
        //DrawVolumes(new Vector4(0.0f, 0.2f, 0.0f, 1.0f), _cullFrontState);

        context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        context.OMSetBlendState(null);
    }

    private void RenderParticles(ID3D11DeviceContext context, Camera camera)
    {

    }

    private void ClearQueues()
    {
        _opaques.Clear();
        _mirrors.Clear();
        _particles.Clear();
    }

    public void Dispose()
    {
        _modelBuffer?.Dispose();
        _colorBuffer?.Dispose();
        _clipPlaneBuffer?.Dispose();
        _mirrorCamera?.Dispose();
        _defaultDepthState?.Dispose();
        _noDepthState?.Dispose();
        _cullBackState?.Dispose();
        _cullFrontState?.Dispose();
        _shadowVolumeDepthState?.Dispose();
        _cullNoneState?.Dispose();
        _noColorWriteBlendState?.Dispose();
        _lightPassDepthState?.Dispose();
        _additiveBlendState?.Dispose();
        _alphaBlendState?.Dispose();
        _mirrorStencilWriteState?.Dispose();
        _mirrorGPassDepthState?.Dispose();
    }
}
