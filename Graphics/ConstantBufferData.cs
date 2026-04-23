using System.Numerics;
using System.Runtime.InteropServices;

namespace GK2PUMA.Graphics;

[StructLayout(LayoutKind.Sequential)]
public struct ConstantBufferData
{
    public Matrix4x4 Model;
    public Matrix4x4 View;
    public Matrix4x4 Projection;
    public Vector4 SurfaceColor;
}