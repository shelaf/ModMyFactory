using System;

namespace ModMyFactory.Helpers
{
    /// <summary>
    /// Provides helper methods for handling Factorio versions.
    /// </summary>
    static class FactorioVersionHelper
    {
        /// <summary>
        /// Normalizes a Factorio version to its mod-compatible two-part version.
        /// Factorio 1.0 is only a re-label of 0.18 and uses the same mods,
        /// so 1.0.x is treated as 0.18. All other versions are returned as their two-part version.
        /// </summary>
        /// <param name="version">The version to normalize.</param>
        /// <returns>The normalized two-part version, or null if <paramref name="version"/> is null.</returns>
        public static Version Normalize(Version version)
        {
            if (version == null) return null;

            // Factorio 1.0 is identical to 0.18 (same mod API version).
            if ((version.Major == 1) && (version.Minor == 0))
                return new Version(0, 18);

            return new Version(version.Major, version.Minor);
        }
    }
}
