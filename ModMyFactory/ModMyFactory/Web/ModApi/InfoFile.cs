﻿using System;
using Newtonsoft.Json;

namespace ModMyFactory.Web.ModApi
{
    sealed class InfoFile
    {
        [JsonProperty("factorio_version")]
        [JsonConverter(typeof(GameVersionConverter))]
        public GameCompatibleVersion FactorioVersion { get; set; }

        [JsonProperty("dependencies")]
        [JsonConverter(typeof(SingleOrArrayJsonConverter<string>))]
        public string[] Dependencies { get; set; }
    }
}
