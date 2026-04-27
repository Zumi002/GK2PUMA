using System.Numerics;

namespace GK2PUMA;

public class Transform
{
    public Transform()
    {
        OnMadeDirty += _ => _isDirty = true;
        OnMatricesRecalculated += _ => _isDirty = false;
    }
    
    private Vector3 _position = Vector3.Zero;
    private Vector3 _rotation = Vector3.Zero;
    private float _scale = 1.0f;

    private bool _isDirty = true;
    private Matrix4x4 _modelMatrix;
    private Matrix4x4 _invModelMatrix;

    public Vector3 Position
    {
        get
        {
            return _position;
        }

        set
        {
            if (_position != value)
            {
                _position = value;
                OnMadeDirty.Invoke(this);
            }
        }
    }

    public Vector3 Rotation
    {
        get
        {
            return _rotation;
        }

        set
        {
            if (_rotation != value)
            {
                _rotation = value;
                OnMadeDirty.Invoke(this);
            }
        }
    }

    public float Scale
    {
        get
        {
            return _scale;
        }

        set
        {
            if (Math.Abs(value - _scale) > 0.001f)
            {
                _scale = value;
                OnMadeDirty.Invoke(this);
            }
        }
    }

    public Matrix4x4 ModelMatrix
    {
        get
        {
            if (_isDirty)
            {
                RecacheMatrices();
            }

            return _modelMatrix;
        }
    }

    public Matrix4x4 InvModelMatrix
    {
        get
        {
            if (_isDirty)
            {
                RecacheMatrices();
            }

            return _invModelMatrix;
        }
    }

    public delegate void MatricesRecalculated(Transform transform);

    public delegate void MadeDirty(Transform transform);

    public event MatricesRecalculated? OnMatricesRecalculated = delegate
    {
    };

    public event MadeDirty OnMadeDirty = delegate
    {
    };

    private void RecacheMatrices()
    {
        _modelMatrix = Matrix4x4.CreateScale(Scale) *
                       Matrix4x4.CreateFromYawPitchRoll(Rotation.Y, Rotation.X, Rotation.Z) *
                       Matrix4x4.CreateTranslation(Position);
        Matrix4x4.Invert(_modelMatrix, out _invModelMatrix);
        OnMatricesRecalculated?.Invoke(this);
    }
}