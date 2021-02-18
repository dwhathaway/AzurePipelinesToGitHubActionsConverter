using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;

namespace AzurePipelinesToGitHubActionsConverter.Core.Conversion.Serialization
{
    public static class GenericObjectSerialization
    {
        // Read in a YAML file and convert it to a T object
        public static T DeserializeYaml<T>(string yaml)
        {
            var deserializer = new DeserializerBuilder().Build();
            T yamlObject = deserializer.Deserialize<T>(yaml);

            return yamlObject;
        }

        // Write a YAML file using the T object
        public static string SerializeYaml<T>(T obj)
        {
            // Convert the object into a YAML document
            var serializer = new SerializerBuilder()
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults) // New as of YamlDotNet 8.0.0: https://github.com/aaubry/YamlDotNet/wiki/Serialization.Serializer#configuredefaultvalueshandlingdefaultvalueshandling. This will not show null properties, e.g. "app-name: " will not display when the value is null, as the value is nullable
                .WithEventEmitter(nextEmitter => new MultilineScalarLiteralStyleEmitter(nextEmitter))
                .Build();
            string yaml = serializer.Serialize(obj);

            return yaml;
        }
    }

    public class MultilineScalarLiteralStyleEmitter : ChainedEventEmitter
    {
        public MultilineScalarLiteralStyleEmitter(IEventEmitter nextEmitter)
            : base(nextEmitter) { }

        public override void Emit(ScalarEventInfo eventInfo, IEmitter emitter)
        {
            if (typeof(string).IsAssignableFrom(eventInfo.Source.Type))
            {
                string value = eventInfo.Source.Value as string;

                if (!string.IsNullOrEmpty(value))
                {
                    bool isMultiLine = value.IndexOfAny(new char[] { '\r', '\n', '\x85', '\x2028', '\x2029' }) >= 0;

                    if (isMultiLine)
                    {
                        eventInfo = new ScalarEventInfo(eventInfo.Source)
                        {
                            Style = ScalarStyle.Literal
                        };
                    }
                }
            }

            nextEmitter.Emit(eventInfo, emitter);
        }
    }
}
