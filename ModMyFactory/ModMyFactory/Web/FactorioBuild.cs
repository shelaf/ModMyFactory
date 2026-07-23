namespace ModMyFactory.Web
{
    /// <summary>
    /// The distribution build of a Factorio release.
    /// 'alpha' is the base game, 'expansion' additionally contains the Space Age expansion.
    /// </summary>
    enum FactorioBuild
    {
        Alpha,
        Expansion,
    }

    static class FactorioBuildExtensions
    {
        /// <summary>
        /// The path segment used in factorio.com/get-download URLs.
        /// </summary>
        public static string ToUrlSegment(this FactorioBuild build)
        {
            switch (build)
            {
                case FactorioBuild.Expansion: return "expansion";
                default: return "alpha";
            }
        }

        /// <summary>
        /// The updater.factorio.com package name for this build and platform.
        /// The 'space-age' package name for the expansion build is not documented on the wiki
        /// and needs to be verified against a live get-available-versions response.
        /// </summary>
        public static string ToUpdatePackage(this FactorioBuild build, bool is64Bit)
        {
            string platform = is64Bit ? "win64" : "win32";
            switch (build)
            {
                case FactorioBuild.Expansion: return $"space-age-{platform}";
                default: return $"core-{platform}";
            }
        }
    }
}
