using System.Numerics;

namespace GK2PUMA.Graphics;

public class LightManager : IDisposable
{
    public const int MaxLights = 2;

    private readonly (Vector3 pos, Vector4 color)[] _lights = new (Vector3, Vector4)[MaxLights];
    private int _count;

    private ConstantBuffer<LightBufferData>? _buffer;
    private ConstantBuffer<LightBufferData> Buffer => _buffer ??= new ConstantBuffer<LightBufferData>();

    public void Clear() => _count = 0;

    public void Add(Vector3 position, Vector4 color)
    {
        if (_count < MaxLights)
        {
            _lights[_count++] = (position, color);
        }
    }

    public void Bind(int slot)
    {
        var data = new LightBufferData();
        if (_count > 0)
        {
            data.LightPos0 = new Vector4(_lights[0].pos, 1f);
            data.LightColor0 = _lights[0].color;
        }

        if (_count > 1)
        {
            data.LightPos1 = new Vector4(_lights[1].pos, 1f);
            data.LightColor1 = _lights[1].color;
        }

        Buffer.Update(data);
        Buffer.Bind(slot);
    }

    public void Dispose() => _buffer?.Dispose();
}