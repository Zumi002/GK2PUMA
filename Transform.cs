using System.Numerics;

namespace GK2PUMA;

public class Transform
{
    public Vector3 Position = Vector3.Zero;
    public Vector3 Rotation = Vector3.Zero;
    public float Scale = 1.0f;

    public Matrix4x4 ModelMatrix
    {
        get
        {
            return Matrix4x4.CreateScale(Scale) *
                   Matrix4x4.CreateFromYawPitchRoll(Rotation.Y, Rotation.X, Rotation.Z) *
                   Matrix4x4.CreateTranslation(Position);
        }
    }
}