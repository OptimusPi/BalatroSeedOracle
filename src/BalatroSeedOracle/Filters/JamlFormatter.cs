using System;

namespace Motely.Filters;

internal static class JamlFormatter
{
    public static string Format(MotelyJsonConfig config) => BsoDraftToJaml.ToJamlYaml(config);

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
