using System.Numerics;
using System.Runtime.InteropServices;

using GK2PUMA.Graphics;

using Vortice.Direct3D11;
using Vortice.DXGI;

using StbImageSharp;

namespace GK2PUMA.Entities;

[StructLayout(LayoutKind.Sequential)]
public struct ParticleInstance
{
    public Vector3 CurrentPos;
    public float Age;
    public Vector3 PreviousPos;
    public float MaxAge;
}

public class Particle
{
    public Vector3 Position;
    public Vector3 PreviousPosition;
    public Vector3 Velocity;
    public float Age;
    public float MaxAge;
    public bool IsAlive => Age < MaxAge;
}

public class ParticleEmitter : Entity    
{
    private readonly List<Particle> _sparks = new();
    private readonly Random _rand = new();

    private Mesh _quadMesh;
    private ID3D11Buffer _instanceBuffer;
    private ID3D11ShaderResourceView _particleTexture;

    private const int MaxParticles = 1000;

    public ParticleEmitter()
    {
        var device = GI.Instance.Device;

        var quadVertices = new Vertex[]
        {
            new Vertex(new Vector3(-0.5f, -0.5f, 0), new Vector3(0, 1, 0)),
            new Vertex(new Vector3(-0.5f,  0.5f, 0), new Vector3(0, 0, 0)),
            new Vertex(new Vector3( 0.5f,  0.5f, 0), new Vector3(1, 0, 0)),
            new Vertex(new Vector3( 0.5f, -0.5f, 0), new Vector3(1, 1, 0)), 
        };
        var quadIndices = new uint[] { 0, 1, 2, 0, 2, 3 };
        _quadMesh = new Mesh(quadVertices, quadIndices);

        int instanceSize = Marshal.SizeOf<ParticleInstance>();
        var bufferDesc = new BufferDescription
        {
            Usage = ResourceUsage.Dynamic, 
            ByteWidth = (uint)instanceSize * MaxParticles,
            BindFlags = BindFlags.VertexBuffer,
            CPUAccessFlags = CpuAccessFlags.Write,
            MiscFlags = ResourceOptionFlags.None
        };
        _instanceBuffer = device.CreateBuffer(bufferDesc);

        using Stream textureStream = Resources.GetResourceStream("GK2PUMA.Textures.tex.png"); 
        _particleTexture = LoadTextureFromStream(device, textureStream);
    }

    public override void Update(float dt)
    {
        for (int i = _sparks.Count - 1; i >= 0; i--)
        {
            var spark = _sparks[i];
            spark.PreviousPosition = spark.Position;
            spark.Velocity += new Vector3(0, -9.81f*0.5f, 0) * dt;
            spark.Position += spark.Velocity * dt;
            spark.Age += dt;

            if (!spark.IsAlive)
            {
                _sparks.RemoveAt(i);
            }
        }

        for (int i = 0; i < 5; i++)
        {
            if (_sparks.Count + 1 >= MaxParticles)
            {
                break;
            }

            Vector3 randomOffset = new Vector3(
                (float)_rand.NextDouble() - 0.5f,
                (float)_rand.NextDouble() - 0.5f,
                (float)_rand.NextDouble() - 0.5f
            );

            Vector3 startVelocity = Vector3.Normalize(Vector3.One + randomOffset*3f) * (2.0f + (float)_rand.NextDouble() * 3.0f);

            _sparks.Add(new Particle
            {
                Position = Vector3.Zero,
                PreviousPosition = Vector3.Zero,
                Velocity = startVelocity,
                Age = 0.0f,
                MaxAge = 2.0f + (float)_rand.NextDouble()
            });
        }
    }

    public ParticleInstance[] GetInstanceData()
    {
        var data = new ParticleInstance[_sparks.Count];
        for (int i = 0; i < _sparks.Count; i++)
        {
            data[i] = new ParticleInstance
            {
                CurrentPos = _sparks[i].Position,
                PreviousPos = _sparks[i].PreviousPosition,
                Age = _sparks[i].Age,
                MaxAge = _sparks[i].MaxAge
            };
        }

        return data;
    }

    public override void Render(Camera camera)
    {
        if (_sparks.Count == 0)
        {
            return;
        }

        var context = GI.Instance.Context;
        var instanceData = GetInstanceData();

        unsafe
        {
            var mappedResource = context.Map(_instanceBuffer, 0, MapMode.WriteDiscard);
            fixed (void* ptr = instanceData)
            {
                int bytesToCopy = _sparks.Count * Marshal.SizeOf<ParticleInstance>();
                System.Buffer.MemoryCopy(ptr, mappedResource.DataPointer.ToPointer(), mappedResource.RowPitch, bytesToCopy);
            }

            context.Unmap(_instanceBuffer, 0);
        }

        GI.Instance.Pipeline.SubmitParticle(_quadMesh, _instanceBuffer, _sparks.Count, _particleTexture);
    }

    public void Dispose()
    {
        _quadMesh.Dispose();
        _instanceBuffer.Dispose();
        _particleTexture?.Dispose();
    }

    private ID3D11ShaderResourceView LoadTextureFromStream(ID3D11Device device, Stream stream)
    {
        var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

        var textureDesc = new Texture2DDescription
        {
            Width = (uint)image.Width,
            Height = (uint)image.Height,
            MipLevels = 1,
            ArraySize = 1,
            Format = Format.R8G8B8A8_UNorm,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Immutable,
            BindFlags = BindFlags.ShaderResource,
            CPUAccessFlags = CpuAccessFlags.None,
            MiscFlags = ResourceOptionFlags.None
        };

        unsafe
        {
            fixed (byte* ptr = image.Data)
            {
                var data = new SubresourceData((IntPtr)ptr, (uint)image.Width * 4);
                using var texture = device.CreateTexture2D(textureDesc, new[] { data });
                return device.CreateShaderResourceView(texture);
            }
        }
    }
}