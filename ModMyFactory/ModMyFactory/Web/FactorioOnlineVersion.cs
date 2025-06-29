using System;
using WPFCore;

namespace ModMyFactory.Web
{
    sealed class FactorioOnlineVersion : NotifyPropertyChangedBase
    {
        private static string GetBranch(bool isExpansion)
        {
            if (isExpansion) return "expansion";

            return "alpha";
        }


        public Version Version { get; }

        public bool IsExperimental { get; }

        public bool IsExpansion { get; }

        public string DownloadUrl { get; }

        public string DisplayVersion { get; }

        public FactorioOnlineVersion(Version version, bool isExpansion, bool isExperimental)
        {
            Version = version;
            IsExpansion = isExpansion;
            IsExperimental = isExperimental;

            string versionString = version.ToString(3);
            string branch = GetBranch(isExpansion);
            string platformString = Environment.Is64BitOperatingSystem ? "win64-manual" : "win32-manual";
            DownloadUrl = $"https://www.factorio.com/get-download/{versionString}/{branch}/{platformString}";
            DisplayVersion = versionString + (isExpansion ? " (Expansion)" : string.Empty);
        }
    }
}
