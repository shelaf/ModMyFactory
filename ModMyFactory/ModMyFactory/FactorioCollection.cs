using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ModMyFactory.Helpers;
using ModMyFactory.Models;

namespace ModMyFactory
{
    sealed class FactorioCollection : ObservableCollection<FactorioVersion>
    {
        public static FactorioCollection Load()
        {
            FactorioVersion.ResetUniqueNames();

            var installedVersions = FactorioVersion.LoadInstalledVersions();
            if (App.Instance.Settings.LoadSteamVersion)
            {
                if (FactorioSteamVersion.TryLoad(out var steamVersion))
                {
                    installedVersions.Add(steamVersion);
                }
                else
                {
                    App.Instance.Settings.LoadSteamVersion = false;
                    App.Instance.Settings.Save();
                }
            }

            return new FactorioCollection(installedVersions);
        }

        public FactorioCollection()
            : base()
        {
            this.Add(new LatestFactorioVersion(this));
        }

        public FactorioCollection(IEnumerable<FactorioVersion> collection)
            : base(collection)
        {
            this.Add(new LatestFactorioVersion(this));
        }

        public FactorioCollection(List<FactorioVersion> list)
            : base(list)
        {
            this.Add(new LatestFactorioVersion(this));
        }

        public bool Contains(string name)
        {
            return this.Any(item => item.Name == name);
        }

        public FactorioVersion Find(string name)
        {
            return this.FirstOrDefault(item => item.Name == name);
        }

        public FactorioVersion Find(Version version, bool exact = true)
        {
            if (exact)
            {
                return this.FirstOrDefault(item => item.Version == version);
            }
            else
            {
                return this.Where(item => (item.Version.Major == version.Major) && (item.Version.Minor == version.Minor))
                           .MaxBy(item => item.Version, new VersionComparer());
            }
        }

        /// <summary>
        /// Finds the installed Factorio version with the oldest build number whose major and minor version match the specified version.
        /// Both versions are normalized to their mod-compatible two-part version before comparing,
        /// so 1.0.x matches a specified version of 0.18.
        /// Special versions without a concrete version are ignored.
        /// </summary>
        /// <returns>Returns the matching version with the oldest build, or null if none is found.</returns>
        public FactorioVersion FindOldestByMinor(Version version)
        {
            var normalized = FactorioVersionHelper.Normalize(version);

            return this.Where(item => (item.Version != null)
                                   && (FactorioVersionHelper.Normalize(item.Version) == normalized))
                       .MinBy(item => item.Version, new VersionComparer());
        }
    }
}
