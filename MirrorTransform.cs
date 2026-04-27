using System.Numerics;

namespace GK2PUMA;

public class MirrorTransform
{
    public Transform BaseTransform { get; }
    public Matrix4x4 ModelMatrix => BaseTransform.ModelMatrix;
    
    public MirrorTransform(Transform baseTransform)
    {
        BaseTransform = baseTransform;
        BaseTransform.OnMadeDirty += _ => _mirrorDirty = true;
        OnMirrorMatrixRecalculated += _ => _mirrorDirty = false;
    }
    
    public delegate void MatrixRecalculated(MirrorTransform mirrorTransform);
    public event MatrixRecalculated? OnMirrorMatrixRecalculated = delegate
    {
    };
    
    private bool _mirrorDirty = true;
    private Matrix4x4 _mirrorMatrix;

    public Matrix4x4 MirrorMatrix
    {
        get
        {
            if (_mirrorDirty)
            {
                RecacheMirrorMatrix();
                OnMirrorMatrixRecalculated?.Invoke(this);
            }
            return _mirrorMatrix;
        }
    }
    
    private void RecacheMirrorMatrix()
    {
        Vector3 localOrigin = new(0.0f, 0.0f, 0.0f);
        Vector3 worldOrigin = Vector3.Transform(localOrigin, ModelMatrix);
        Vector3 localNormal = new(0.0f, 0.0f, -1.0f);
        Vector3 worldNormal = Vector3.TransformNormal(localNormal, ModelMatrix);
        
        float distance = -Vector3.Dot(worldNormal, worldOrigin);
        Plane worldPlane = new(worldNormal, distance);
        _mirrorMatrix = Matrix4x4.CreateReflection(worldPlane);
    }
}