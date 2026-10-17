using System;
using System.Collections.Generic;

namespace PooketSlug
{
    /// <summary>One line of a setup (<see cref="SetupText"/>): its keyword and what follows it.</summary>
    public readonly struct SetupLine
    {
        /// <summary>Where it is, from 1, for the problems a reader reports.</summary>
        public readonly int Number;
        /// <summary>The first word, in lower case.</summary>
        public readonly string Key;
        /// <summary>Everything after the keyword, trimmed (a map row, kept as it is).</summary>
        public readonly string Rest;

        public SetupLine(int number, string key, string rest)
        {
            Number = number;
            Key = key;
            Rest = rest;
        }

        /// <summary>The words after the keyword, split at spaces and '=' ("tune=kit.superkills=6" is two).</summary>
        public string[] Args => Rest.Split(new[] { ' ', '\t', '=' }, StringSplitOptions.RemoveEmptyEntries);

        public override string ToString() => Key + " " + Rest;
    }

    /// <summary>
    /// The link reader: setups written as short lines of <c>keyword what-follows</c>, from a page link or from a small
    /// text file published beside the game (<c>sandbox/&lt;name&gt;.txt</c>). The sandbox reads them (<see cref="SandboxSetup"/>),
    /// and cutscenes as data will too. A line ends at a new line or a ';' (so one fits in a link), the keyword at a
    /// space, ':' or '='; lines starting <c>//</c> are notes. '#' starts a link's fragment, so ground in a link is
    /// written another way (the sandbox's <c>ground</c> line). Scene scripts (<see cref="SceneScript"/>) are only ever
    /// files, so their lines end only at a new line, and a ';' is just a ';'.
    /// </summary>
    public static class SetupText
    {
        /// <summary>The longest setup read, so a bad link or file can't stall the game.</summary>
        public const int MaxLength = 16384;

        public static List<SetupLine> Read(string text, bool links = true)
        {
            var lines = new List<SetupLine>();
            if (string.IsNullOrEmpty(text)) return lines;
            if (text.Length > MaxLength) text = text.Substring(0, MaxLength);
            int number = 0;
            foreach (string raw in links ? text.Replace("\r", "").Split('\n', ';') : text.Replace("\r", "").Split('\n'))
            {
                number++;
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal)) continue;
                int cut = line.IndexOfAny(new[] { ' ', '\t', ':', '=' });
                string key = (cut < 0 ? line : line.Substring(0, cut)).ToLowerInvariant();
                string rest = cut < 0 ? "" : line.Substring(cut + 1).Trim();
                lines.Add(new SetupLine(number, key, rest));
            }
            return lines;
        }

        /// <summary>
        /// The value names a file rather than being a setup itself: letters, digits, '-' and '_' only, so it can only
        /// ever be a file in the game's own folder.
        /// </summary>
        public static bool IsName(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > 48) return false;
            foreach (char c in value)
                if (!(c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == '-' || c == '_')) return false;
            return true;
        }

        /// <summary>
        /// Where the file <paramref name="name"/> is: <c>&lt;folder&gt;/&lt;name&gt;.txt</c> beside the page (a preview's own
        /// folder in a preview), or null off the site or for a name that isn't one (<see cref="IsName"/>).
        /// </summary>
        public static string FileUrl(string pageUrl, string folder, string name)
        {
            string page = PageQuery.Folder(pageUrl);
            if (page.Length == 0 || !IsName(name)) return null;
            return page + folder + "/" + name.ToLowerInvariant() + ".txt";
        }
    }
}
