public abstract class Entity
{
    public virtual void HandleInput() { }
    public virtual void Update(float dt) { }
    public virtual void Render() { }
}