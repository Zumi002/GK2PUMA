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
    public ID3D11Buffer? AdjacencyIndexBuffer { get; private set; }

    public int IndexCount { get; private set; }
    public int AdjacencyIndexCount { get; }

    public Mesh(Vertex[] vertices, uint[] indices, uint[]? adjacencyIndices = null)
    {
        var device = GI.Instance.Device;

        VertexBuffer = device.CreateBuffer(vertices, BindFlags.VertexBuffer);
        IndexBuffer = device.CreateBuffer(indices, BindFlags.IndexBuffer);
        IndexCount = indices.Length;

        if (adjacencyIndices != null && adjacencyIndices.Length > 0)
        {
            AdjacencyIndexBuffer = device.CreateBuffer(adjacencyIndices, BindFlags.IndexBuffer);
            AdjacencyIndexCount = adjacencyIndices.Length;
        }
    }

    public void Bind(bool useAdjacency = false)
    {
        var context = GI.Instance.Context;

        context.IASetVertexBuffer(0, VertexBuffer, 24, 0);

        if (useAdjacency && AdjacencyIndexBuffer != null)
        {
            context.IASetIndexBuffer(AdjacencyIndexBuffer, Vortice.DXGI.Format.R32_UInt, 0);
        }
        else
        {
            context.IASetIndexBuffer(IndexBuffer, Vortice.DXGI.Format.R32_UInt, 0);
        }
    }

    public void Unbind()
    {
        var context = GI.Instance.Context;
        context.IASetVertexBuffer(0, null, 0, 0);
        context.IASetIndexBuffer(null, Vortice.DXGI.Format.Unknown, 0);
    }

    public void Dispose()
    {
        VertexBuffer?.Dispose();
        IndexBuffer?.Dispose();
    }
}