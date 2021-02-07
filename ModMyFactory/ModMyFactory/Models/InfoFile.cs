﻿using System;
using System.IO;
using ModMyFactory.Helpers;
using Newtonsoft.Json;

namespace ModMyFactory.Models
{
    [JsonObject(MemberSerialization.OptIn)]
    sealed class InfoFile
    {
        /// <summary>
        /// The unique name of the mod.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; }

        /// <summary>
        /// The mods version.
        /// </summary>
        [JsonProperty("version")]
        [JsonConverter(typeof(GameVersionConverter))]
        public GameCompatibleVersion Version { get; }

        /// <summary>
        /// The actual version of Factorio this mod is compatible with.
        /// </summary>
        [JsonProperty("factorio_version")]
        [JsonConverter(typeof(GameVersionConverter))]
        public GameCompatibleVersion ActualFactorioVersion { get; }

        /// <summary>
        /// The version of Factorio this mod is compatible with.<br/>
        /// This is always a minor version, where version 1.0 is considered to be version 0.18 for compatibility reasons
        /// </summary>
        [JsonIgnore]
        public GameCompatibleVersion FactorioVersion { get; }

        /// <summary>
        /// The mods friendly name.
        /// </summary>
        [JsonProperty("title")]
        public string FriendlyName { get; }

        /// <summary>
        /// The author of the mod.
        /// </summary>
        [JsonProperty("author")]
        public string Author { get; }

        /// <summary>
        /// A description of the mod.
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; }

        /// <summary>
        /// The mods dependencies.
        /// </summary>
        [JsonProperty("dependencies")]
        [JsonConverter(typeof(SingleOrArrayJsonConverter<ModDependency>))]
        public ModDependency[] Dependencies { get; }

        /// <summary>
        /// Indicates whether this info file is valid.
        /// To be valid, the <see cref="Name"/>, <see cref="Version"/> and <see cref="ActualFactorioVersion"/> properties must be non-null.
        /// </summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(Name) && (Version != null) && ((Name == "base") || (ActualFactorioVersion != null));

        [JsonConstructor]
        private InfoFile(string name, GameCompatibleVersion version, GameCompatibleVersion actualFactorioVersion, string friendlyName, string author, string description, ModDependency[] dependencies)
        {
            Name = name;
            Version = version;
            ActualFactorioVersion = actualFactorioVersion;
            FactorioVersion = null;
            if (actualFactorioVersion != null)
            {
                FactorioVersion = actualFactorioVersion.ToFactorioMinor();
            }
            FriendlyName = friendlyName;
            Author = author;
            Description = description;
            Dependencies = dependencies ?? new ModDependency[0];
        }

        /// <summary>
        /// Deserializes an info file from a JSON string.
        /// </summary>
        /// <param name="jsonString">The string to deserialize.</param>
        /// <returns>Returns the deserialized info file object.</returns>
        public static InfoFile FromJsonString(string jsonString)
        {
            return JsonHelper.Deserialize<InfoFile>(jsonString);
        }

        /// <summary>
        /// Deserializes an info file from a JSON stream.
        /// </summary>
        /// <param name="jsonStream">The stream to deserialize.</param>
        /// <returns>Returns the deserialized info file object.</returns>
        public static InfoFile FromJsonStream(Stream jsonStream)
        {
            using (var reader = new StreamReader(jsonStream))
            {
                string jsonString = reader.ReadToEnd();
                return FromJsonString(jsonString);
            }
        }
    }
}
