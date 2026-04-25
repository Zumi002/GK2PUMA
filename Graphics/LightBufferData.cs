using System.Numerics;
using System.Runtime.InteropServices;

namespace GK2PUMA.Graphics;

[StructLayout(LayoutKind.Sequential)]
public struct LightBufferData
{
    public Vector4 LightPos0;
    public Vector4 LightPos1;
    public Vector4 LightColor0;
    public Vector4 LightColor1;
}
