using System;

namespace ModMyFactory.Helpers
{
    static class UriExtensions
    {
        /// <summary>
        /// Indicates whether this URI is an absolute HTTP or HTTPS link.
        /// </summary>
        public static bool IsWebLink(this Uri uri)
        {
            return uri.IsAbsoluteUri && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }
    }
}
