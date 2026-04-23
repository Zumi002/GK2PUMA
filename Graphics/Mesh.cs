using System.Numerics;

using Vortice.Direct3D;
using Vortice.Direct3D11;

namespace GK2PUMA.Graphics;

public struct Vertex
{
    public Vector3 Position;
    public Vector3 Normal;

    public Vertex(Vector3 position, Vector3 normal)
    {
        Position = position;
        Normal = normal;
    }
}

public class Mesh : IDisposable
{
    public ID3D11Buffer VertexBuffer { get; private set; }
    public ID3D11Buffer IndexBuffer { get; private set; }
    public int IndexCount { get; private set; }

    public Mesh(IEnumerable<Vertex> vertices, IEnumerable<uint> indices)
    {
        var device = GraphicsContext.Instance.Device;

        var vArray = vertices.ToArray();
        var iArray = indices.ToArray();

        VertexBuffer = device.CreateBuffer(vArray, BindFlags.VertexBuffer);
        IndexBuffer = device.CreateBuffer(iArray, BindFlags.IndexBuffer);
        IndexCount = iArray.Length;
    }

    public void Bind()
    {
        var context = GraphicsContext.Instance.Context;

        context.IASetVertexBuffer(0, VertexBuffer, 24, 0);
        context.IASetIndexBuffer(IndexBuffer, Vortice.DXGI.Format.R32_UInt, 0);
        context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
    }

    public void Unbind()
    {
        var context = GraphicsContext.Instance.Context;
        context.IASetVertexBuffer(0, null, 0, 0);
        context.IASetIndexBuffer(null, Vortice.DXGI.Format.Unknown, 0);
    }

    public void Dispose()
    {
        VertexBuffer?.Dispose();
        IndexBuffer?.Dispose();
    }
}