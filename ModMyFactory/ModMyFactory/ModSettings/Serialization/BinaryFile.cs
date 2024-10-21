﻿using ModMyFactory.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace ModMyFactory.ModSettings.Serialization
{
    /// <summary>
    /// Represents a binary settings file.
    /// </summary>
    class BinaryFile
    {
        enum PropertyTreeType : byte
        {
            None = 0,
            Bool = 1,
            Double = 2,
            String = 3,
            List = 4,
            Dictionary = 5,
            Int = 6,
        }

        static readonly BinaryVersion OldestSupportedVersion = new BinaryVersion(0, 16, 0, 0);
        static readonly BinaryVersion DefaultWriteVersion = new BinaryVersion(0, 17, 9, 1);
        static readonly Dictionary<Version, BinaryVersion> DefaultWriteVersions = new Dictionary<Version, BinaryVersion>()
        {
            { new Version(0, 16), new BinaryVersion(0, 16, 51, 0) },
            { new Version(0, 17), new BinaryVersion(0, 17, 42, 4) },
        };
        static readonly BinaryVersion BehaviourSwitch = new BinaryVersion(0, 17, 0, 0); // Starting with 0.17 there is an additional byte in the file.

        public BinaryVersion Version { get; }

        public string JsonString { get; set; }

        private static bool FileVersionSupported(BinaryVersion fileVersion, out BinaryVersion writeVersion)
        {
            if (fileVersion < OldestSupportedVersion)
            {
                writeVersion = BinaryVersion.Empty;
                return false;
            }

            writeVersion = fileVersion;
            return true;
        }

        private static string ReadString(BinaryReader reader)
        {
            string result = string.Empty;

            if (!reader.ReadBoolean()) // If true string is empty
            {
                uint length = reader.ReadByte(); // Length is usually stored as 1 byte
                if (length == byte.MaxValue) length = reader.ReadUInt32(); // If length exceeds 255 chars it is stored as 4 bytes

                byte[] buffer = reader.ReadBytes((int)length);
                result = Encoding.UTF8.GetString(buffer);
            }

            return result;
        }

        private static void ReadPropertyTree(BinaryReader reader, JsonWriter jsonWriter)
        {
            var type = (PropertyTreeType)reader.ReadByte();
            reader.ReadByte(); // Reserved

            switch (type)
            {
                case PropertyTreeType.None:
                    break;

                case PropertyTreeType.Bool:
                    jsonWriter.WriteValue(reader.ReadBoolean());
                    break;

                case PropertyTreeType.Double:
                    jsonWriter.WriteValue(reader.ReadDouble());
                    break;

                case PropertyTreeType.String:
                    string value = ReadString(reader);
                    jsonWriter.WriteValue(value);
                    break;

                case PropertyTreeType.List:
                    {
                        jsonWriter.WriteStartObject();

                        uint count = reader.ReadUInt32();
                        for (int i = 0; i < count; i++)
                        {
                            ReadString(reader); // List is actually dictionary but with empty key
                            ReadPropertyTree(reader, jsonWriter);
                        }

                        jsonWriter.WriteEndObject();
                        break;
                    }
                    

                case PropertyTreeType.Dictionary:
                    {
                        jsonWriter.WriteStartObject();

                        uint count = reader.ReadUInt32();
                        for (int i = 0; i < count; i++)
                        {
                            string key = ReadString(reader);
                            jsonWriter.WritePropertyName(key);
                            ReadPropertyTree(reader, jsonWriter);
                        }

                        jsonWriter.WriteEndObject();
                        break;
                    }

                case PropertyTreeType.Int:
                    jsonWriter.WriteValue(reader.ReadInt32());
                    break;

                default:
                    throw new InvalidOperationException($"Found unknown type {type} in property tree.");
            }
        }

        public BinaryFile(FileInfo file)
        {
            try
            {
                using (var stream = file.OpenRead())
                {
                    using (var reader = new BinaryReader(stream))
                    {
                        var fileVersion = reader.ReadBinaryVersion();
                        if (!FileVersionSupported(fileVersion, out var writeVersion)) throw new ArgumentException("The file version is not supported.", nameof(file));
                        Version = writeVersion;
                        if (Version >= BehaviourSwitch) reader.ReadBoolean();

                        var sb = new StringBuilder();
                        var sw = new StringWriter(sb);
                        var writer = new JsonTextWriter(sw);
                        writer.Formatting = Formatting.Indented;

                        ReadPropertyTree(reader, writer);
                        JsonString = sw.ToString();
                    }
                }
            }
            catch (Exception ex) when (ex is EndOfStreamException || ex is JsonException)
            {
                throw new ArgumentException("The specified file is not a valid settings file.", nameof(file), ex);
            }
        }

        private BinaryVersion GetDefaultWriteVersion(Version factorioVersion)
        {
            if (DefaultWriteVersions.TryGetValue(factorioVersion, out var result)) return result;
            return DefaultWriteVersion;
        }

        public BinaryFile(Version factorioVersion, string jsonString = null)
        {
            Version = GetDefaultWriteVersion(factorioVersion);
            JsonString = jsonString;
        }

        private static PropertyTreeType ParseTokenType(JTokenType type)
        {
            switch (type)
            {
                case JTokenType.Object: return PropertyTreeType.Dictionary;
                case JTokenType.Array: return PropertyTreeType.List;
                case JTokenType.Integer: return PropertyTreeType.Int;
                case JTokenType.Float: return PropertyTreeType.Double;
                case JTokenType.String: return PropertyTreeType.String;
                case JTokenType.Boolean: return PropertyTreeType.Bool;
                default:
                    throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(JTokenType));
            }
        }

        private static void WriteString(BinaryWriter writer, string value)
        {
            bool isEmpty = string.IsNullOrEmpty(value);
            writer.Write(isEmpty);
            if (isEmpty) return;

            byte[] buffer = Encoding.UTF8.GetBytes(value);
            
            if (buffer.Length < byte.MaxValue)
            {
                writer.Write((byte)buffer.Length);
            }
            else
            {
                writer.Write(byte.MaxValue);
                writer.Write((uint)buffer.Length);
            }

            writer.Write(buffer);
        }

        private static void WritePropertyTree(BinaryWriter writer, JToken token)
        {
            var type = ParseTokenType(token.Type);
            writer.Write((byte)type);
            writer.Write(type == PropertyTreeType.String); // Write reserved byte; value taken from Rsedings code, meaning unknown

            switch (type)
            {
                case PropertyTreeType.None:
                    break;

                case PropertyTreeType.Bool:
                    writer.Write(token.Value<bool>());
                    break;

                case PropertyTreeType.Double:
                    writer.Write(token.Value<double>());
                    break;

                case PropertyTreeType.String:
                    WriteString(writer, token.Value<string>());
                    break;

                case PropertyTreeType.List:
                    {
                        writer.Write((uint)token.Count());

                        foreach (var child in token.Children())
                        {
                            WriteString(writer, null); // Write empty key
                            WritePropertyTree(writer, child);
                        }

                        break;
                    }


                case PropertyTreeType.Dictionary:
                    {
                        writer.Write((uint)token.Count());

                        foreach (var pair in token.Value<IDictionary<string, JToken>>())
                        {
                            WriteString(writer, pair.Key);
                            WritePropertyTree(writer, pair.Value);
                        }

                        break;
                    }

                case PropertyTreeType.Int:
                    writer.Write(token.Value<int>());
                    break;
            }
        }

        public void Save(FileInfo file)
        {
            using (var stream = file.Open(FileMode.Create, FileAccess.Write))
            {
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write(Version);
                    if (Version >= BehaviourSwitch) writer.Write(false);

                    if (string.IsNullOrWhiteSpace(JsonString))
                    {
                        writer.Write((byte)PropertyTreeType.None);
                        return;
                    }

                    try
                    {
                        var token = JObject.Parse(JsonString);
                        WritePropertyTree(writer, token);
                    }
                    catch (Exception ex) when (ex is InvalidEnumArgumentException || ex is JsonException)
                    {
                        throw new ArgumentException("The JSON string does not represent a valid settings file.", nameof(file), ex);
                    }
                }
            }
        }
    }
}
