using System.Numerics;
using System.Runtime.InteropServices;

namespace GK2PUMA.Graphics;

[StructLayout(LayoutKind.Sequential)]
public struct ConstantBufferViewProj
{
    public Matrix4x4 View;
    public Matrix4x4 Projection;
    public Vector4 ClipPlane;
    public Vector3 Pos;
}