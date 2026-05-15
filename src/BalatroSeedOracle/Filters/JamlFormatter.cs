using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Motely.Filters;

internal static class JamlFormatter
{
    public static string Format(JamlRootDocument config) =>
        new SerializerBuilder()
            .WithNamingConvention(NullNamingConvention.Instance)
            .DisableAliases()
            .ConfigureDefaultValuesHandling(
                DefaultValuesHandling.OmitNull
                    | DefaultValuesHandling.OmitEmptyCollections
                    | DefaultValuesHandling.OmitDefaults)
            .Build()
            .Serialize(config);

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
