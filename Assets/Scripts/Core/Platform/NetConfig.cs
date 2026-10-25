using System;

namespace PooketSlug
{
    /// <summary>
    /// The Photon settings a build bakes in as <c>Resources/pooket-net.json</c>, and the rules for reading them. The CI
    /// build writes the file from the PHOTON_APP_ID secret (see WebGLBuild); it is git-ignored, so local builds need their
    /// own copy. The netcode loads it (<c>Net/NetSession.cs</c>), so Core never touches Unity's resources.
    /// </summary>
    public static class NetConfig
    {
        public const string ResourceName = "pooket-net";

        /// <summary>The file's fields; a field left out reads as null.</summary>
        [Serializable]
        public sealed class Data
        {
            public string appId;
            public string region; // the Photon region every game is in; empty = DefaultRegion

            /// <summary>Online play needs an AppId; without one the build is solo only.</summary>
            public bool Configured => !string.IsNullOrEmpty(appId);
            /// <summary>The region every game is in, whichever room it joins (MATES or a <c>?room=</c> one).</summary>
            public string Region => NetConfig.Region(region);
        }

        /// <summary>The region every game is in unless the build names another: Australia (NET-05, the owner's call).</summary>
        public const string DefaultRegion = "au";

        /// <summary>The region a build is in, from the one it names: trimmed, lower case, and <see cref="DefaultRegion"/> when blank.</summary>
        public static string Region(string named)
        {
            string region = named?.Trim().ToLowerInvariant();
            return string.IsNullOrEmpty(region) ? DefaultRegion : region;
        }
    }
}
