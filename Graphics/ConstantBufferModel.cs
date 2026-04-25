using System.Numerics;
using System.Runtime.InteropServices;

namespace GK2PUMA.Graphics;

[StructLayout(LayoutKind.Sequential)]
public struct ConstantBufferModel
{
    public Matrix4x4 Model;
    public Matrix4x4 ModelInvT;
}