using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ModMyFactory.Helpers;
using ModMyFactory.Models;
using ModMyFactory.Web.UpdateApi;

namespace ModMyFactory.Web
{
    /// <summary>
    /// Represents the factorio.com website.
    /// </summary>
    static class FactorioWebsite
    {
        private static bool VersionListContains(List<FactorioOnlineVersion> versionList, Version version, bool isExpansion)
        {
            return versionList.Any(item => item.Version == version && item.IsExpansion == isExpansion);
        }

        private static void AddVersionGroup(List<FactorioOnlineVersion> versionList, ReleaseInfo.VersionGroup versionGroup, bool isExperimental)
        {
            var versions = new[] { versionGroup?.Alpha, versionGroup?.Expansion };
            var isExpansionBuilds = new[] { false, true };

            foreach (var (version, isExpansion) in versions.Zip(isExpansionBuilds, (v, e) => (v, e)))
            {
                if (version != null)
                {
                    if (!VersionListContains(versionList, version, isExpansion))
                    {
                        var onlineVersion = new FactorioOnlineVersion(version, isExpansion, isExperimental);
                        versionList.Add(onlineVersion);
                    }
                }
            }
        }

        private static void GetVersionsFromUrl(string url, List<FactorioOnlineVersion> versionList)
        {
            string document = WebHelper.GetDocument(url);
            if (!string.IsNullOrEmpty(document))
            {
                var releaseInfo = JsonHelper.Deserialize<ReleaseInfo>(document);
                AddVersionGroup(versionList, releaseInfo?.Stable, false);
                AddVersionGroup(versionList, releaseInfo?.Experimental, true);
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
                var result = new List<FactorioOnlineVersion>();
                GetVersionsFromUrl("https://factorio.com/api/latest-releases", result);
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
