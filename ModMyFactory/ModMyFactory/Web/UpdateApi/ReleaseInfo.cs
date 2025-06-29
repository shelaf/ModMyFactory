using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ModMyFactory.Web.UpdateApi
{
    sealed class ReleaseInfo
    {
        [JsonProperty("experimental")]
        public VersionGroup Experimental { get; private set; }

        [JsonProperty("stable")]
        public VersionGroup Stable { get; private set; }

        internal class VersionGroup
        {
            [JsonProperty("alpha")]
            [JsonConverter(typeof(VersionConverter))]
            public Version Alpha { get; private set; }

            [JsonProperty("demo")]
            [JsonConverter(typeof(VersionConverter))]
            public Version Demo { get; private set; }

            [JsonProperty("expansion")]
            [JsonConverter(typeof(VersionConverter))]
            public Version Expansion { get; private set; }

            [JsonProperty("headless")]
            [JsonConverter(typeof(VersionConverter))]
            public Version Headless { get; private set; }
        }
    }
}
