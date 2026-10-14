namespace PooketSlug
{
    /// <summary>
    /// Room invite codes. "ABCD-EU" is session "ABCD" in Photon region "eu". A host gets a random name, and
    /// players type the code or open an invite link with <c>?room=ABCD-EU</c>.
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

        /// <summary>
        /// Splits a typed code into its session name (upper case) and region (lower case, or null when the
        /// code has none, which means the best region by ping). False when there's no name.
        /// </summary>
        public static bool TryParse(string code, out string name, out string region)
        {
            string[] parts = (code ?? "").Trim().ToUpperInvariant().Split('-');
            name = parts[0].Trim();
            region = parts.Length > 1 ? parts[1].Trim().ToLowerInvariant() : null;
            if (region == "") region = null;
            return name.Length > 0;
        }

        /// <summary>The code players share: the name, then the region in capitals if there is one.</summary>
        public static string Format(string name, string region) =>
            string.IsNullOrEmpty(region) ? name : name + "-" + region.ToUpperInvariant();
    }
}
