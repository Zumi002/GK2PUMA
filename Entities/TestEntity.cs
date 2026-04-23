using GK2PUMA.Entities;
using GK2PUMA.Graphics;

using Vortice.Mathematics;

namespace GK2PUMA.Entities;

public class TestObject : Entity
{
    private float _time = 0;

    public override void Update(float dt)
    {
        _time += dt;
    }

    public override void Render(Camera camera)
    {
        var context = GraphicsContext.Instance.Context;
        var rtv = GraphicsContext.Instance.RenderTargetView;

        context.ClearRenderTargetView(rtv, new Color4(
            float.Abs(float.Sin(_time)),
            float.Abs(float.Cos(_time)),
            float.Abs(float.Sin(_time)) * 0.5f + float.Abs(float.Cos(_time)) * 0.5f,
            1.0f));
    }
}