using System;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Photon settings baked into the build as <c>Resources/pooket-net.json</c>. The CI build writes it from
    /// the PHOTON_APP_ID secret (see WebGLBuild); it is git-ignored, so local builds need their own copy.
    /// </summary>
    public static class NetConfig
    {
        public const string ResourceName = "pooket-net";

        [Serializable]
        public sealed class Data
        {
            public string appId;
            public string region; // the Photon region every game is in; empty = DefaultRegion
        }

        static Data s_Data;

        /// <summary>The region every game is in unless the build names another: Australia (NET-05, the owner's call).</summary>
        public const string DefaultRegion = "au";

        public static string AppId => Load().appId;
        /// <summary>The Photon region every game is in: hosting, joining and quick match alike.</summary>
        public static string Region
        {
            get
            {
                string region = Load().region?.Trim().ToLowerInvariant();
                return string.IsNullOrEmpty(region) ? DefaultRegion : region;
            }
        }
        public static bool Configured => !string.IsNullOrEmpty(AppId);

        static Data Load()
        {
            if (s_Data != null) return s_Data;
            var text = Resources.Load<TextAsset>(ResourceName);
            try { s_Data = text != null ? JsonUtility.FromJson<Data>(text.text) : null; }
            catch (ArgumentException) { s_Data = null; }
            return s_Data ??= new Data();
        }
    }
}
