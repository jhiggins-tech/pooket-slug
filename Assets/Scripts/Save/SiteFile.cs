using System;
using System.Collections;
using UnityEngine.Networking;

namespace PooketSlug
{
    /// <summary>
    /// Fetches a small text file published beside the game (a sandbox's setup, <see cref="SetupText.FileUrl"/>; a scene file; the narration's list), fresh
    /// each time: Pages caches files for a few minutes, so a file just published would otherwise read as the old one.
    /// </summary>
    public static class SiteFile
    {
        /// <summary>Run as a coroutine; <paramref name="done"/> gets the text, or null and why not.</summary>
        public static IEnumerator Fetch(string url, Action<string, string> done)
        {
            string fresh = url + (url.Contains("?") ? "&" : "?") + "v=" + DateTime.UtcNow.Ticks;
            using (UnityWebRequest request = UnityWebRequest.Get(fresh))
            {
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success) done(request.downloadHandler.text, null);
                else done(null, request.responseCode == 404 ? "not found" : request.error);
            }
        }

        /// <summary>A file's bytes (a narrator's clip), as the URL has it: it carries its own version, so no cache-busting.</summary>
        public static IEnumerator FetchBytes(string url, Action<byte[], string> done)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success) done(request.downloadHandler.data, null);
                else done(null, request.responseCode == 404 ? "not found" : request.error);
            }
        }
    }
}
