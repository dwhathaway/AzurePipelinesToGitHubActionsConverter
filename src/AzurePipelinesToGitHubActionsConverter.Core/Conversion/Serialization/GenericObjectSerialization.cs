using System.Collections.Generic;
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

    class MyEventEmitter : ChainedEventEmitter
    {
        private class EmitterState
        {
            private int valuePeriod;
            private int currentIndex;

            public EmitterState(int valuePeriod)
            {
                this.valuePeriod = valuePeriod;
            }

            public bool VisitNext()
            {
                ++currentIndex;
                return (currentIndex % valuePeriod) == 0;
            }
        }

        private readonly Stack<EmitterState> state = new Stack<EmitterState>();

        public MyEventEmitter(IEventEmitter nextEmitter)
            : base(nextEmitter)
        {
            this.state.Push(new EmitterState(1));
        }

        public override void Emit(ScalarEventInfo eventInfo, IEmitter emitter)
        {
            if (this.state.Peek().VisitNext())
            {
                // if (eventInfo.Source.Type == typeof(string))
                // {
                //     eventInfo.Style = ScalarStyle.DoubleQuoted;
                // }

                // if (eventInfo.Source.Value?.ToString() == "Report LFS Data Usage")
                // {
                //     int i = 0;
                // }

                if (eventInfo.Source.Value?.ToString().StartsWith("function DisplayInBytes($num)") ?? false)
                {
                    eventInfo.Style = ScalarStyle.Literal;
                }

                if (eventInfo.Source.Value?.ToString().StartsWith("git submodule deinit -f handheld/src-external/imgui") ?? false)
                {
                    eventInfo.Style = ScalarStyle.Literal;
                }
            }

            base.Emit(eventInfo, emitter);
        }

        public override void Emit(MappingStartEventInfo eventInfo, IEmitter emitter)
        {
            this.state.Peek().VisitNext();
            this.state.Push(new EmitterState(2));
            base.Emit(eventInfo, emitter);
        }

        public override void Emit(MappingEndEventInfo eventInfo, IEmitter emitter)
        {
            this.state.Pop();
            base.Emit(eventInfo, emitter);
        }

        public override void Emit(SequenceStartEventInfo eventInfo, IEmitter emitter)
        {
            this.state.Peek().VisitNext();
            this.state.Push(new EmitterState(1));
            base.Emit(eventInfo, emitter);
        }

        public override void Emit(SequenceEndEventInfo eventInfo, IEmitter emitter)
        {
            this.state.Pop();
            base.Emit(eventInfo, emitter);
        }
    }
}
