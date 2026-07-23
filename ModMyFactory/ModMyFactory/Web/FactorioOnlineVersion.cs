using System;
using WPFCore;

namespace ModMyFactory.Web
{
    sealed class FactorioOnlineVersion : NotifyPropertyChangedBase
    {
        public Version Version { get; }

        public bool IsExperimental { get; }

        public FactorioBuild Build { get; }

        public bool IsExpansion => Build == FactorioBuild.Expansion;

        public string DownloadUrl { get; }

        public FactorioOnlineVersion(Version version, bool isExperimental, FactorioBuild build)
        {
            Version = version;
            IsExperimental = isExperimental;
            Build = build;

            string versionString = version.ToString(3);
            string buildString = build.ToUrlSegment();
            string platformString = Environment.Is64BitOperatingSystem ? "win64-manual" : "win32-manual";
            DownloadUrl = $"https://www.factorio.com/get-download/{versionString}/{buildString}/{platformString}";
        }
    }
}
