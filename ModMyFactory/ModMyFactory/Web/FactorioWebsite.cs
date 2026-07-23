using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ModMyFactory.Helpers;
using ModMyFactory.Models;
using ModMyFactory.Web.DownloadApi;

namespace ModMyFactory.Web
{
    /// <summary>
    /// Represents the factorio.com website.
    /// </summary>
    static class FactorioWebsite
    {
        const string LatestReleasesUrl = "https://factorio.com/api/latest-releases";

        // Only the playable full-package builds are offered for download.
        private static readonly (string Key, FactorioBuild Build)[] Builds =
        {
            ("alpha", FactorioBuild.Alpha),
            ("expansion", FactorioBuild.Expansion),
        };

        private static void AddVersions(Dictionary<string, string> releases, bool isExperimental, List<FactorioOnlineVersion> versionList)
        {
            if (releases == null) return;

            foreach (var (key, build) in Builds)
            {
                if (!releases.TryGetValue(key, out string versionString)) continue;
                if (!Version.TryParse(versionString, out var version)) continue;

                // Stable is added before experimental, so an identical experimental entry is dropped here.
                if (versionList.Any(item => item.Version == version && item.Build == build)) continue;

                versionList.Add(new FactorioOnlineVersion(version, isExperimental, build));
            }
        }

        /// <summary>
        /// Reads the Factorio version list.
        /// </summary>
        /// <returns>Returns the list of available Factorio versions or null if the operation was unsucessful.</returns>
        public static async Task<List<FactorioOnlineVersion>> GetVersionsAsync()
        {
            return await Task.Run(() =>
            {
                string document = WebHelper.GetDocument(LatestReleasesUrl);
                if (string.IsNullOrWhiteSpace(document)) return null;

                var template = JsonHelper.Deserialize<LatestReleasesTemplate>(document);
                if (template == null) return null;

                var result = new List<FactorioOnlineVersion>();
                AddVersions(template.Stable, false, result);
                AddVersions(template.Experimental, true, result);
                return result;
            });
        }

        /// <summary>
        /// Downloads Factorio.
        /// </summary>
        /// <param name="version">The version of Factorio to be downloaded.</param>
        /// <param name="progress">A progress object used to report the progress of the operation.</param>
        /// <param name="cancellationToken">A cancelation token that can be used to cancel the operation.</param>
        public static async Task<FactorioVersion> DownloadFactorioAsync(FactorioOnlineVersion version, string username, string token, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var factorioDirectory = App.Instance.Settings.GetFactorioDirectory();
            if (!factorioDirectory.Exists) factorioDirectory.Create();

            var file = new FileInfo(Path.Combine(factorioDirectory.FullName, "package.zip"));
            string url = version.DownloadUrl + $"?username={username}&token={token}";
            await WebHelper.DownloadFileAsync(new Uri(url), file, progress, cancellationToken);

            try
            {
                if (cancellationToken.IsCancellationRequested) return null;
                progress.Report(2);

                if (!FactorioFile.TryLoad(file, out var factorioFile)) return null;
                var factorioFolder = await FactorioFolder.FromFileAsync(factorioFile, factorioDirectory);
                return new FactorioVersion(factorioFolder);
            }
            finally
            {
                if (file.Exists)
                    file.Delete();
            }
        }
    }
}
