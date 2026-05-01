using System.Numerics;
using System.Runtime.InteropServices;

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
    public ID3D11ShaderResourceView[]? Textures;
}

public class RenderingPipeline : IDisposable
{
    private readonly List<MirrorCommand> _mirrors = new();
    private readonly List<OpaqueCommand> _opaques = new();
    private readonly List<ParticleCommand> _particles = new();

    private ConstantBuffer<ConstantBufferModel>? _modelBuffer;
    private ConstantBuffer<ConstantBufferSurfaceColor>? _colorBuffer;

    private ID3D11DepthStencilState? _defaultDepthState;
    private ID3D11DepthStencilState? _noDepthState;
    private ID3D11DepthStencilState? _noDepthWriteState;
    private ID3D11RasterizerState? _cullBackState;
    private ID3D11RasterizerState? _cullFrontState;
    private ID3D11DepthStencilState? _shadowVolumeDepthState;
    private ID3D11DepthStencilState? _lightPassDepthState;
    private ID3D11RasterizerState? _cullNoneState;
    private ID3D11BlendState? _noColorWriteBlendState;
    private ID3D11BlendState? _additiveBlendState;
    private ID3D11BlendState? _alphaBlendState;

    private Camera? _mirrorCamera;

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

        var noDepthWriteDesc = new DepthStencilDescription
        {
            DepthEnable = true,
            DepthWriteMask = DepthWriteMask.Zero,
            DepthFunc = ComparisonFunction.LessEqual,
            StencilEnable = false
        };
        _noDepthWriteState = device.CreateDepthStencilState(noDepthWriteDesc);

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

        var alphaBlendState = new BlendDescription();
        alphaBlendState.RenderTarget[0] = new RenderTargetBlendDescription
        {
            BlendEnable = true,
            SourceBlend = Blend.SourceAlpha,
            DestinationBlend = Blend.DestinationAlpha,
            BlendOperation = BlendOperation.Add,
            SourceBlendAlpha = Blend.SourceAlpha,
            DestinationBlendAlpha = Blend.DestinationAlpha,
            BlendOperationAlpha = BlendOperation.Add,
            RenderTargetWriteMask = ColorWriteEnable.All
        };
        _alphaBlendState = device.CreateBlendState(alphaBlendState);

        _cullFrontState = device.CreateRasterizerState(cullFrontDesc);
        _modelBuffer = new();
        _colorBuffer = new();
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

    public void SubmitParticle(Mesh mesh, ID3D11Buffer instanceBuffer, int instanceCount, ID3D11ShaderResourceView[]? textures = null)
    {
        _particles.Add(
            new ParticleCommand {
                Mesh = mesh,
                InstanceBuffer = instanceBuffer,
                InstanceCount = instanceCount,
                Textures = textures
            }
        );
    }

    public void Execute(Camera mainCamera)
    {
        var context = GI.Instance.Context;

        context.ClearRenderTargetView(GI.Instance.RenderTargetView, new Color4(0.1f, 0.1f, 0.1f, 1.0f));
        context.ClearDepthStencilView(GraphicsContext.Instance.DepthStencilView, DepthStencilClearFlags.Stencil, 1.0f, 0);
        context.ClearDepthStencilView(GraphicsContext.Instance.DepthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);

        foreach (var mirrorCommand in _mirrors)
        {
            _mirrorCamera.UpdateAsMirror(mainCamera, mirrorCommand.Transform);
            _mirrorCamera.UpdateAndBindViewProjBuffer();

            context.ClearDepthStencilView(GraphicsContext.Instance.DepthStencilView, DepthStencilClearFlags.Stencil, 1.0f, 0);

            RenderGPass(context, _mirrorCamera, GI.Instance.DepthStencilMirrorView, inMirror: true);
            RenderShadowVolume(context, _mirrorCamera, GI.Instance.DepthStencilMirrorView);
            RenderMirrorStencilPass(context, mainCamera, mirrorCommand, 1);
            RenderLightPass(context, _mirrorCamera, 1);
            RenderParticles(context, _mirrorCamera, 1);
        }

        context.ClearDepthStencilView(GraphicsContext.Instance.DepthStencilView, DepthStencilClearFlags.Stencil, 1.0f, 0);

        mainCamera.ClipPlane = new Vector4(0, 0, 0, 1.0f);
        mainCamera.UpdateAndBindViewProjBuffer();

        RenderGPass(context, mainCamera, GI.Instance.DepthStencilView);
        RenderShadowVolume(context, mainCamera, GI.Instance.DepthStencilView);
        RenderLightPass(context, mainCamera);
        RenderMirrorSurfaces(context, mainCamera);
        RenderParticles(context, mainCamera);

        context.OMSetRenderTargets(0, null, null);

        ClearQueues();
    }

    private void RenderMirrorStencilPass(ID3D11DeviceContext context, Camera mainCamera, MirrorCommand mirrorCommand, uint stencilRef)
    {
        context.OMSetRenderTargets(GI.Instance.RenderTargetView, GI.Instance.DepthStencilView);
        context.OMSetDepthStencilState(_defaultDepthState, stencilRef);
        context.RSSetState(_cullBackState);

        _modelBuffer.Bind(0);
        _modelBuffer.Update(new ConstantBufferModel
        {
            Model = mirrorCommand.Transform,
            ModelInv = mirrorCommand.InvTransform
        });

        mirrorCommand.Mesh.Bind();
        context.DrawIndexed((uint)mirrorCommand.Mesh.IndexCount, 0, 0);
    }

    private void RenderMirrorSurfaces(ID3D11DeviceContext context, Camera mainCamera)
    {
        context.OMSetRenderTargets(GI.Instance.RenderTargetView, GI.Instance.DepthStencilView);
        context.OMSetBlendState(_alphaBlendState);
        context.RSSetState(_cullBackState);
        context.OMSetDepthStencilState(_defaultDepthState, 0);

        var blinnPhong = GI.Instance.ShaderManager.GetShader(ShaderManager.ShaderType.BlinnPhong);
        blinnPhong.Use();
        _modelBuffer.Bind(0);
        _colorBuffer.Bind(2);
        foreach (var cmd in _mirrors)
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

    private void RenderGPass(ID3D11DeviceContext context, Camera camera, ID3D11DepthStencilView depthStencilView, bool inMirror = false)
    {
        context.RSSetState(inMirror ? _cullFrontState : _cullBackState);
        context.OMSetDepthStencilState(_defaultDepthState, 0);
        context.OMSetBlendState(null);

        context.OMSetRenderTargets(GI.Instance.GBufferRTVs, depthStencilView);

        var clearColor = new Color4(0.0f, 0.0f, 0.0f, 0.0f);
        context.ClearRenderTargetView(GI.Instance.GBufferRTVs[0], clearColor);
        context.ClearRenderTargetView(GI.Instance.GBufferRTVs[1], clearColor);
        context.ClearRenderTargetView(GI.Instance.GBufferRTVs[2], clearColor);
        context.ClearDepthStencilView(GI.Instance.DepthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);
        context.ClearDepthStencilView(GI.Instance.DepthStencilView, DepthStencilClearFlags.Stencil, 1.0f, 0);

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
            else
            {
                context.PSSetShaderResources(0, new ID3D11ShaderResourceView?[] { null });
            }

            cmd.Mesh.Bind();
            context.DrawIndexed((uint)cmd.Mesh.IndexCount, 0, 0);
        }

        _colorBuffer.Update(new ConstantBufferSurfaceColor
        {
            SurfaceColor = new Vector4(0, 0, 0, 0)
        });

        foreach (var cmd in _mirrors)
        {
            _modelBuffer.Update(new ConstantBufferModel
            {
                Model = cmd.Transform,
                ModelInv = cmd.InvTransform
            });

            if (inMirror)
            {
                _colorBuffer.Update(new ConstantBufferSurfaceColor
                {
                    SurfaceColor = cmd.SurfaceColor
                });
            }

            if (cmd.Texture != null)
            {
                context.PSSetShaderResources(0, new[] { cmd.Texture });
            }
            else
            {
                context.PSSetShaderResources(0, new ID3D11ShaderResourceView?[] { null });
            }

            cmd.Mesh.Bind();
            context.DrawIndexed((uint)cmd.Mesh.IndexCount, 0, 0);
        }
    }

    private void RenderLightPass(ID3D11DeviceContext context, Camera camera, uint stencilRef = 0)
    {
        context.RSSetState(_cullBackState);
        context.OMSetDepthStencilState(_noDepthState, stencilRef);
        context.OMSetBlendState(null); 

        context.OMSetRenderTargets(GI.Instance.RenderTargetView, GI.Instance.DepthStencilView);

        var ambientShader = GI.Instance.ShaderManager.GetShader(ShaderManager.ShaderType.AmbientPass);
        ambientShader.Use();

        context.PSSetShaderResources(0, GI.Instance.GBufferSRVs);
        context.PSSetSamplers(0, [GI.Instance.DefaultSampler]);

        context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        context.Draw(3, 0);

        context.OMSetDepthStencilState(_lightPassDepthState, 0);
        context.OMSetBlendState(_additiveBlendState);

        var lightPassShader = GI.Instance.ShaderManager.GetShader(ShaderManager.ShaderType.LightPass);
        lightPassShader.Use();

        GI.Instance.LightManager.Bind(3);

        context.Draw(3, 0);

        context.PSSetShaderResources(0, new ID3D11ShaderResourceView[] { null, null, null });
        context.OMSetBlendState(null);
    }

    private void RenderShadowVolume(ID3D11DeviceContext context, Camera camera, ID3D11DepthStencilView depthStencilView)
    {
        context.RSSetState(_cullNoneState);
        context.OMSetDepthStencilState(_shadowVolumeDepthState, 0);
        context.OMSetBlendState(_noColorWriteBlendState);

        context.OMSetRenderTargets(GI.Instance.GBufferRTVs, depthStencilView);

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

    private void RenderParticles(ID3D11DeviceContext context, Camera camera, uint stencilRef = 0)
    {
        if (_particles.Count == 0)
        {
            return;
        }

        camera.UpdateAndBindViewProjBuffer();
        context.RSSetState(_cullNoneState);
        context.OMSetDepthStencilState(_noDepthWriteState, stencilRef);
        context.OMSetBlendState(_additiveBlendState);
        context.OMSetRenderTargets(GI.Instance.RenderTargetView, GI.Instance.DepthStencilView);

        var particleShader = GI.Instance.ShaderManager.GetShader(ShaderManager.ShaderType.Particle);
        particleShader.Use();

        foreach (var cmd in _particles)
        {
            if (cmd.InstanceCount == 0)
            {
                continue;
            }

            if (cmd.Textures != null)
            {
                context.PSSetShaderResources(0, cmd.Textures);
                context.PSSetSamplers(0, new[] { GI.Instance.DefaultSampler });
            }

            cmd.Mesh.Bind();
            context.IASetVertexBuffers(1, new[] { cmd.InstanceBuffer }, new[] { (uint)Marshal.SizeOf<ParticleInstance>() }, new[] { 0u });
            context.DrawIndexedInstanced((uint)cmd.Mesh.IndexCount, (uint)cmd.InstanceCount, 0, 0, 0);
        }

        context.IASetVertexBuffers(1, new ID3D11Buffer[] { null }, new[] { 0u }, new[] { 0u });
        context.OMSetBlendState(null);
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
        _mirrorCamera?.Dispose();
        _defaultDepthState?.Dispose();
        _noDepthState?.Dispose();
        _noDepthWriteState?.Dispose();
        _cullBackState?.Dispose();
        _cullFrontState?.Dispose();
        _shadowVolumeDepthState?.Dispose();
        _cullNoneState?.Dispose();
        _noColorWriteBlendState?.Dispose();
        _lightPassDepthState?.Dispose();
        _additiveBlendState?.Dispose();
        _alphaBlendState?.Dispose();
    }
}
