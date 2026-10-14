using System;

namespace PooketSlug
{
    /// <summary>Reads the page address the WebGL build was opened with (Application.absoluteURL).</summary>
    public static class PageQuery
    {
        /// <summary>One query-string parameter, unescaped, or null when it's absent. Names ignore case.</summary>
        public static string Get(string url, string name)
        {
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(name)) return null;
            int q = url.IndexOf('?'), hash = url.IndexOf('#');
            if (q < 0 || (hash >= 0 && hash < q)) return null; // a '?' after the '#' is part of the fragment
            string query = hash < 0 ? url.Substring(q + 1) : url.Substring(q + 1, hash - q - 1);
            foreach (string part in query.Split('&'))
            {
                int eq = part.IndexOf('=');
                string key = eq < 0 ? part : part.Substring(0, eq);
                if (!string.Equals(key, name, StringComparison.OrdinalIgnoreCase)) continue;
                return eq < 0 ? "" : Uri.UnescapeDataString(part.Substring(eq + 1).Replace('+', ' '));
            }
            return null;
        }

        /// <summary>The address without its query string or fragment.</summary>
        public static string Base(string url)
        {
            if (string.IsNullOrEmpty(url)) return "";
            int cut = url.IndexOfAny(new[] { '?', '#' });
            return cut < 0 ? url : url.Substring(0, cut);
        }
    }
}
