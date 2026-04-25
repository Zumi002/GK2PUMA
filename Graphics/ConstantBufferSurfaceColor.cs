using System.Numerics;
using System.Runtime.InteropServices;

namespace GK2PUMA.Graphics;

[StructLayout(LayoutKind.Sequential)]
public struct ConstantBufferSurfaceColor
{
    public Vector4 SurfaceColor;
}
