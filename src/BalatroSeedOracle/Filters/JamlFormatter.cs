namespace Motely.Filters;

internal static class JamlFormatter
{
    // Motely owns JAML emission — delegate to the engine's AOT-safe StaticSerializer
    // instead of a reflection-based SerializerBuilder (IL3050).
    public static string Format(JamlRootDocument config) =>
        Motely.Filters.Jaml.JamlConfigLoader.SerializeRoot(config);

    public static string Format(string jaml)
    {
        try
        {
            var stream = new YamlDotNet.RepresentationModel.YamlStream();
            using var reader = new System.IO.StringReader(jaml);
            stream.Load(reader);
            using var writer = new System.IO.StringWriter();
            stream.Save(writer, assignAnchors: false);
            return writer.ToString();
        }
        catch
        {
            return jaml;
        }
    }
}
