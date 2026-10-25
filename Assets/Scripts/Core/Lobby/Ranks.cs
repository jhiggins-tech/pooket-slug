namespace PooketSlug
{
    /// <summary>One rung of the rank ladder (Pooket Tabks' own, <c>src/stats/ranks.ts</c> there).</summary>
    public sealed class Rank
    {
        /// <summary>Never reused or renamed: saves go by the ladder's order, and this names it.</summary>
        public readonly string Id;
        public readonly string Name;
        /// <summary>The insignia's colours, light and dark (0xRRGGBB).</summary>
        public readonly uint Light, Dark;
        /// <summary>A line for the rank-up, or null.</summary>
        public readonly string Line;

        public Rank(string id, string name, uint light, uint dark, string line)
        {
            Id = id;
            Name = name;
            Light = light;
            Dark = dark;
            Line = line;
        }

        public override string ToString() => Name;
    }

    /// <summary>
    /// The rank ladder, lowest first: Pooket Tabks' 33 ranks, ported from its <c>src/stats/ranks.ts</c> (the owner,
    /// 2026-10-16). Everyone starts on the lowest (<c>docs/design/player.md</c>, What the player keeps). Most are
    /// meant to be discovered: a rank's name shows in the game only once you've reached it, and only Bronze, Silver,
    /// Gold, Platinum, Diamond, Master, Grandmaster and Champion may be named in text players read (the game's screens,
    /// the site). Code, tests, docs and commit messages may name any of them (the owner, 17 Oct: it's about spoilers).
    /// </summary>
    public static class Ranks
    {
        public static readonly Rank[] All =
        {
            new Rank("pototo", "Pototo", 0xecd49c, 0x9c7a3c, "Technically a rank."),
            new Rank("duck", "Rubber Duck", 0xffee70, 0xe0a800, "Squeak."),
            new Rank("cardboard", "Cardboard", 0xecd0a6, 0xa97c50, "Fine until it rains."),
            new Rank("wood", "Wood", 0xd6aa72, 0x78502c, "Knock knock."),
            new Rank("stone", "Stone", 0xcfd1d8, 0x6b6f7a, "Next up, the crafting table."),
            new Rank("plastic", "Plastic", 0xff7b7b, 0xb91c1c, "Looks fine until you step on it."),
            new Rank("glass", "Glass", 0xe4fcff, 0x6fb7c9, "Please don’t throw it."),
            new Rank("tin", "Tin", 0xe2e8ec, 0x7d8b96, "Rattles when shaken."),
            new Rank("iron", "Iron", 0xb4bccb, 0x3f4756, "Heavy, honest, a bit rusty."),
            new Rank("bronze", "Bronze", 0xe0a36a, 0x8a5426, null),
            new Rank("silver", "Silver", 0xf2f4f8, 0x8b93a6, null),
            new Rank("gold", "Gold", 0xffe27a, 0xb8860b, null),
            new Rank("cheese", "Cheese", 0xffd36a, 0xd98a14, "Gouda enough."),
            new Rank("feudal", "Feudal", 0xd3d7c6, 0x5f6b52, "Kneel."),
            new Rank("marble", "Marble", 0xf8f5f1, 0x9aa0b4, "Cold, smooth, a bit smug."),
            new Rank("platinum", "Platinum", 0xd9fffb, 0x3fa7a0, null),
            new Rank("amethyst", "Amethyst", 0xe6c9ff, 0x7e22ce, "Gem of the month."),
            new Rank("emerald", "Emerald", 0x8af5b8, 0x0f8f5a, "Greener than it looks."),
            new Rank("ruby", "Ruby", 0xff9ab0, 0xb3123c, "Red means ranked."),
            new Rank("sapphire", "Sapphire", 0x86b4ff, 0x1e3a8a, "Deep and a little sad."),
            new Rank("diamond", "Diamond", 0xc9ecff, 0x3b82f6, null),
            new Rank("obsidian", "Obsidian", 0xa594ff, 0x2d1f5e, "Forged in a bad mood."),
            new Rank("titanium", "Titanium", 0xd3dcea, 0x4b5d78, "Light, strong, insufferable."),
            new Rank("mithril", "Mithril", 0xf0fbff, 0x3d7fd6, "One ring to rule the Game browser."),
            new Rank("netherite", "Netherite", 0xb09a90, 0x2b1f1c, "Diamond, but angrier."),
            new Rank("adamantium", "Adamantium", 0xc9d0ff, 0x4338ca, "Snikt."),
            new Rank("plasma", "Plasma", 0xd8fbff, 0x0ea5e9, "Do not lick."),
            new Rank("master", "Master", 0xffd9a0, 0xe2741d, null),
            new Rank("grandmaster", "Grandmaster", 0xe4d4ff, 0x7c3aed, null),
            new Rank("champion", "Champion", 0xffd1ec, 0xe11d74, null),
            new Rank("supernova", "Supernova", 0xfff3b0, 0xff6a00, "Brief, bright, and loud."),
            new Rank("blackhole", "Black Hole", 0xffd29a, 0xff5a1f, "Nothing gets out."),
            new Rank("unobtainium", "Unobtainium", 0xb8ffe3, 0x7c3aed, "Mathematically possible."),
        };
    }
}
