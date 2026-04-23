using System.IO;
using System.Reflection;

namespace GK2PUMA
{
    public static class Resources
    {
        public static string ReadResource(string resourcePath)
        {
            var assembly = Assembly.GetExecutingAssembly();

            using Stream stream = assembly.GetManifestResourceStream(resourcePath);
            using StreamReader reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        public static Stream GetResourceStream(string resourcePath)
        {
            var assembly = Assembly.GetExecutingAssembly();
            return assembly.GetManifestResourceStream(resourcePath);
        }
    }
}