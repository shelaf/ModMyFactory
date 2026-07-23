using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ModMyFactory.Helpers;
using ModMyFactory.Web.UpdateApi;

namespace ModMyFactory.Web
{
    /// <summary>
    /// Represents the updater.factorio.com website.
    /// </summary>
    static class UpdateWebsite
    {
        const string BaseUrl = "https://updater.factorio.com";
        const int ApiVersion = 2;

        /// <summary>
        /// Gets all available Factorio updates for a specific package.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="token">The login token.</param>
        /// <param name="package">The update package name (build and platform specific).</param>
        /// <returns>Returns information about all available updates for the package, or null if the package is not offered.</returns>
        public static async Task<UpdateInfo> GetUpdateInfoAsync(string username, string token, string package)
        {
            string url = $"{BaseUrl}/get-available-versions?username={username}&token={token}&apiVersion={ApiVersion}";
            string document = await Task.Run(() => WebHelper.GetDocument(url));

            if (!string.IsNullOrEmpty(document))
            {
                UpdateInfoTemplate template = JsonHelper.Deserialize<UpdateInfoTemplate>(document);
                if ((template != null) && template.TryGetValue(package, out var steps))
                    return new UpdateInfo(steps);
            }

            return null;
        }

        /// <summary>
        /// Gets the download link of a specific update step.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="token">The login token.</param>
        /// <param name="step">The update step to retrieve the download link of.</param>
        /// <param name="package">The update package name (build and platform specific).</param>
        /// <returns>Returns a link that can be used to download the update step.</returns>
        private static async Task<string> GetUpdateLinkAsync(string username, string token, UpdateStep step, string package)
        {
            string url = $"{BaseUrl}/get-download-link?username={username}&token={token}&apiVersion={ApiVersion}&package={package}&from={step.From}&to={step.To}";
            string document = await Task.Run(() => WebHelper.GetDocument(url));

            int firstIndex = document.IndexOf('"');
            int lastIndex = document.LastIndexOf('"');
            string link = document.Substring(firstIndex + 1, lastIndex - firstIndex - 1);

            return link;
        }

        /// <summary>
        /// Downloads an update package.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="token">The login token.</param>
        /// <param name="step">The update step to download.</param>
        /// <param name="package">The update package name (build and platform specific).</param>
        /// <param name="progress">A progress object used to report the progress of the operation.</param>
        /// <param name="cancellationToken">A cancelation token that can be used to cancel the operation.</param>
        /// <returns>Returns the downloaded file.</returns>
        public static async Task<FileInfo> DownloadUpdatePackageAsync(string username, string token, UpdateStep step, string package, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var tempDir = new DirectoryInfo(App.Instance.TempPath);
            if (!tempDir.Exists) tempDir.Create();

            string url = await GetUpdateLinkAsync(username, token, step, package);
            string fileName = Path.GetFileName(url);
            if (fileName.Contains("?")) fileName = fileName.Substring(0, fileName.LastIndexOf('?'));

            var file = new FileInfo(Path.Combine(tempDir.FullName, fileName));
            await WebHelper.DownloadFileAsync(new Uri(url), file, progress, cancellationToken);

            return cancellationToken.IsCancellationRequested ? null : file;
        }
    }
}
