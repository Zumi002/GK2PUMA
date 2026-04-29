using System.Numerics;

namespace GK2PUMA.Graphics;

public static class MeshGenerator
{
    public static Mesh CreateSphereMesh(uint precision)
    {
        var vertices = new List<Vertex>();
        var indices = new List<uint>();

        for (int i = 0; i <= precision; i++)
        {
            double lat = Math.PI * (-0.5 + (double)i / precision);
            double sinLat = Math.Sin(lat);
            double cosLat = Math.Cos(lat);

            for (int j = 0; j <= precision; j++)
            {
                double lon = 2 * Math.PI * (j == precision ? 0 : j) / precision;
                float x = (float)(Math.Cos(lon) * cosLat);
                float y = (float)sinLat;
                float z = (float)(Math.Sin(lon) * cosLat);

                vertices.Add(new Vertex(new Vector3(x, y, z), new Vector3(x, y, z)));
            }
        }

        for (uint i = 0; i < precision; i++)
        {
            for (uint j = 0; j < precision; j++)
            {
                uint first = i * (precision + 1) + j;
                uint second = first + (precision + 1);

                indices.Add(first);
                indices.Add(second);
                indices.Add(first + 1);
                indices.Add(second);
                indices.Add(second + 1);
                indices.Add(first + 1);
            }
        }

        return new Mesh(vertices.ToArray(), indices.ToArray());
    }
}