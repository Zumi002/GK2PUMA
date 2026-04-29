using System.Numerics;
using System.Runtime.InteropServices;

namespace GK2PUMA.Graphics;

[StructLayout(LayoutKind.Sequential)]
public struct ConstantBufferClipPlane
{
    public Vector4 ClipPlane;
}
