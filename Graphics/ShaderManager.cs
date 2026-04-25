namespace GK2PUMA.Graphics;

public class ShaderManager
{
    public const string UnlitShaderName = "Unlit";
    public const string PhongShaderName = "Phong";
    
    private readonly Dictionary<string, Shader> _shaders = new();

    public Shader GetShader(string shaderName)
    {
        if (_shaders.TryGetValue(shaderName, out var shader))
        {
            return shader;
        }

        throw new Exception($"Shader not found");
    }

    public void AddShader(string shaderName, Shader shader)
    {
        _shaders.Add(shaderName, shader);
    }

    public void DisposeAll()
    {
        foreach (var shader in _shaders.Values)
        {
            shader.Dispose();
        }

        _shaders.Clear();
    }
}