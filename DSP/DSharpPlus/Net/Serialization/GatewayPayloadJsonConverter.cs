using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using DSharpPlus.Net.Abstractions;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Frozen;
using System.Diagnostics;

namespace DSharpPlus.Net.Serialization
{
    internal class GatewayPayloadConverter : JsonConverter<GatewayPayload>
    {
        private static readonly FrozenDictionary<string, Type> PayloadTypes = new Dictionary<string, Type>()
        {
            { "READY", typeof(ReadyPayload) },
        }.ToFrozenDictionary();

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, GatewayPayload? value, JsonSerializer serializer)
            => throw new NotSupportedException();

        public override GatewayPayload? ReadJson(JsonReader reader, Type objectType, GatewayPayload? existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            if (reader.TokenType != JsonToken.StartObject)
            {
                throw new JsonSerializationException($"JsonTokenType was of type {reader.TokenType}, expected {nameof(JsonToken.StartObject)}");
            }

            GatewayOpCode? opcode = null; // op
            object? data = null; // d
            int? sequence = null; // s
            string? eventName = null; // t

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.PropertyName:
                        var propertyName = reader.Value!.ToString()!;

                        if (!reader.Read())
                        {
                            throw new JsonSerializationException("Unexpected end");
                        }

                        // skip to content
                        while (reader.TokenType == JsonToken.Comment)
                        {
                            if (!reader.Read())
                            {
                                throw new JsonSerializationException("Unexpected end");
                            }
                        }

                        switch (propertyName)
                        {
                            case "t":
                                if (reader.TokenType == JsonToken.String)
                                    eventName = (string)reader.Value;
                                else if (reader.TokenType == JsonToken.Null)
                                    eventName = null;
                                else throw new JsonSerializationException("Type was not string or null");

                                break;
                            case "s":
                                if (reader.TokenType == JsonToken.Integer)
                                    sequence = (int)(long)reader.Value; // if this cast fails for some reason, use Convert.ToInt32
                                else if (reader.TokenType == JsonToken.Null)
                                    sequence = null;
                                else throw new JsonSerializationException("Sequence was not int or null.");

                                break;
                            case "op":
                                if (reader.TokenType != JsonToken.Integer)
                                {
                                    throw new JsonSerializationException("OpCode was not int.");
                                }

                                opcode = (GatewayOpCode)(long)reader.Value; // if this cast fails for some reason, use Convert.ToInt32

                                break;
                            case "d":
                                if (reader.TokenType != JsonToken.Null)
                                {
                                    if (eventName == null)
                                    {
                                        Trace.WriteLine("GatewayPayloadConverter fastpath missed!");
                                        data = JToken.Load(reader);
                                    }
                                    else
                                    {
                                        if (reader.TokenType == JsonToken.Integer)
                                        {
                                            data = reader.Value; // will probably be a long, check it
                                        }
                                        else if (eventName != null && PayloadTypes.TryGetValue(eventName, out var payloadType))
                                        {
                                            // i'm like 70% sure this is what you're supposed to do
                                            data = serializer.Deserialize(reader, payloadType);
                                        }
                                        else
                                        {
                                            data = JToken.Load(reader);
                                        }
                                    }
                                }

                                break;
                        }
                        break;
                    case JsonToken.Comment:
                        break;
                    case JsonToken.EndObject:
                        return new GatewayPayload
                        {
                            Sequence = sequence,
                            Data = data,
                            EventName = eventName,
                            OpCode = opcode!.Value,
                        };
                }
            }

            throw new JsonSerializationException("Unexpected end");
        }
    }
}
