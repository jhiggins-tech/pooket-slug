namespace PooketSlug
{
    /// <summary>
    /// The co-op room: one always-there room everyone's phone joins (Jack, 18 Oct: we play with mates, so there's
    /// nothing to host, browse or type). It's Photon session <see cref="Mates"/>, in one region
    /// (<see cref="NetConfig.Region"/>), and only identical builds meet, so each build (each branch preview too) has
    /// its own. <c>?room=NAME</c> names another room instead: the co-op tests give each round its own, and a link to
    /// that room carries it.
    /// </summary>
    public static class CoopRoom
    {
        /// <summary>The room everyone joins.</summary>
        public const string Mates = "MATES";

        /// <summary>The room CO-OP joins: the one <c>?room=</c> names, or <see cref="Mates"/>.</summary>
        public static string NameFor(string roomFlag) => TryParse(roomFlag, out string name) ? name : Mates;

        /// <summary>
        /// The invite link: the game's address (no query), with <c>?room=NAME</c> only for a room other than
        /// <see cref="Mates"/>. Null with no address (the editor).
        /// </summary>
        public static string Link(string pageUrl, string room)
        {
            if (string.IsNullOrEmpty(pageUrl)) return null;
            return string.IsNullOrEmpty(room) || room == Mates ? pageUrl : pageUrl + "?room=" + room;
        }

        /// <summary>
        /// The room name in a <c>?room=</c> flag, in upper case. False when there's none. Older invite links carried a
        /// region ("ABCD-EU"); it's ignored, since every game is in one region.
        /// </summary>
        public static bool TryParse(string flag, out string name)
        {
            name = (flag ?? "").Trim().ToUpperInvariant().Split('-')[0].Trim();
            return name.Length > 0;
        }
    }
}
