namespace PooketSlug
{
    /// <summary>
    /// Room invite codes: four letters, such as "ABCD", the session's name. Every game is in one Photon region
    /// (<see cref="NetConfig.Region"/>), so the code needs nothing else. A host gets a random name, and players type
    /// the code or open an invite link with <c>?room=ABCD</c>.
    /// </summary>
    public static class InviteCode
    {
        /// <summary>The letters a new name is made from: no I or O, which read as 1 and 0.</summary>
        public const string Letters = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        public const int NameLength = 4;

        /// <summary>A new session name for a host, such as "HCNW".</summary>
        public static string NewName(Rng rng)
        {
            var name = new char[NameLength];
            for (int i = 0; i < name.Length; i++) name[i] = Letters[rng.Range(0, Letters.Length)];
            return new string(name);
        }

        /// <summary>The invite link for a room: the game's address (no query) with <c>?room=CODE</c>.</summary>
        public static string Link(string pageUrl, string code) =>
            string.IsNullOrEmpty(pageUrl) || string.IsNullOrEmpty(code) ? null : pageUrl + "?room=" + code;

        /// <summary>
        /// The session name in a typed code, in upper case. False when there's none. Older builds' codes carried a
        /// region ("ABCD-EU"); it's ignored, since every game is now in one region.
        /// </summary>
        public static bool TryParse(string code, out string name)
        {
            name = (code ?? "").Trim().ToUpperInvariant().Split('-')[0].Trim();
            return name.Length > 0;
        }
    }
}
