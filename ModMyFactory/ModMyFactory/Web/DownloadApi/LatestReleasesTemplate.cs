using System.Collections.Generic;
using Newtonsoft.Json;

namespace ModMyFactory.Web.DownloadApi
{
    /// <summary>
    /// Maps the JSON returned by https://factorio.com/api/latest-releases.
    /// Both channels contain build name -> version string entries (alpha, demo, expansion, headless).
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    sealed class LatestReleasesTemplate
    {
        [JsonProperty("stable")]
        public Dictionary<string, string> Stable { get; set; }

        [JsonProperty("experimental")]
        public Dictionary<string, string> Experimental { get; set; }
    }
}
