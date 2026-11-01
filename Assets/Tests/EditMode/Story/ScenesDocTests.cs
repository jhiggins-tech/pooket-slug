using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>
    /// docs/scenes.md names the stage's vocabulary (sets, speakers, cues, things, verbs, colours). Those lists are
    /// copies of what Staging, SceneScript and Cues hold, so each is checked against the code both ways: a name added
    /// to the code and not the page fails, and so does one on the page the code doesn't have.
    /// </summary>
    public class ScenesDocTests
    {
        static string s_Doc;

        static string Doc => s_Doc ?? (s_Doc = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "docs", "scenes.md")).Replace("\r\n", "\n"));

        /// <summary>The text from <paramref name="marker"/> to the end of its paragraph (the next blank line).</summary>
        static string Paragraph(string marker)
        {
            int at = Doc.IndexOf(marker, System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(at, 0, "docs/scenes.md has '" + marker + "'");
            int end = Doc.IndexOf("\n\n", at, System.StringComparison.Ordinal);
            return Doc.Substring(at, (end < 0 ? Doc.Length : end) - at);
        }

        /// <summary>The table rows (lines starting with |) after <paramref name="header"/>, header and divider left out.</summary>
        static List<string> Rows(string header)
        {
            int at = Doc.IndexOf(header, System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(at, 0, "docs/scenes.md has the table '" + header + "'");
            var rows = new List<string>();
            foreach (string line in Doc.Substring(at).Split('\n').Skip(2))
            {
                if (!line.StartsWith("|")) break;
                rows.Add(line);
            }
            return rows;
        }

        static List<string> Spans(string text) =>
            Regex.Matches(text, "`([^`]+)`").Cast<Match>().Select(m => Regex.Replace(m.Groups[1].Value, @"\s+", " ")).ToList();

        static string First(string span) => span.Split(' ')[0];

        static void SameSet(IEnumerable<string> doc, IEnumerable<string> code, string what)
        {
            var d = new HashSet<string>(doc);
            var c = new HashSet<string>(code);
            Assert.IsEmpty(c.Except(d).OrderBy(x => x), what + ": in the code but not in docs/scenes.md");
            Assert.IsEmpty(d.Except(c).OrderBy(x => x), what + ": in docs/scenes.md but not in the code");
        }

        [Test]
        public void TheSetsMatch()
        {
            string line = Doc.Split('\n').First(l => l.StartsWith("set street"));
            int from = line.IndexOf("the backdrop: ", System.StringComparison.Ordinal) + "the backdrop: ".Length;
            int to = line.IndexOf(" (", from, System.StringComparison.Ordinal);
            string list = line.Substring(from, to < 0 ? line.Length - from : to - from);
            SameSet(list.Split(new[] { ", ", " or " }, System.StringSplitOptions.RemoveEmptyEntries), Staging.Sets.Keys, "sets");
        }

        [Test]
        public void TheSpeakersMatch()
        {
            SameSet(Spans(Paragraph("**Who speaks**")).Select(First), SceneScript.Speakers.Keys, "speakers");
        }

        [Test]
        public void TheCuesMatch()
        {
            string para = Paragraph("**Cues**");
            List<string> spans = Spans(para);
            SameSet(spans.Where(s => !s.Contains(" ")), Cues.Effects.Keys, "cues");
            // the announcer lines it gives as examples are ones the game calls
            foreach (string s in spans.Where(s => Regex.IsMatch(s, @"(^| )say [a-z-]+$")))
                Assert.IsTrue(Cues.CallsStart(Cues.Say(s.Substring(s.LastIndexOf(' ') + 1))) || s.EndsWith("<line>"), s);
        }

        [Test]
        public void TheThingsMatch()
        {
            var kinds = new Dictionary<string, Staging.Kind>
            {
                { "Soldiers", Staging.Kind.Figure }, { "Props", Staging.Kind.Prop }, { "Effects", Staging.Kind.Effect },
            };
            var all = new List<string>();
            foreach (string row in Rows("| Kind | Things |"))
            {
                string[] cells = row.Split('|');
                string kindName = cells[1].Trim();
                Assert.IsTrue(kinds.ContainsKey(kindName), "a row for '" + kindName + "' is a kind the test knows");
                var names = Spans(cells[2]).Select(First).ToList();
                SameSet(names, Staging.Things.Values.Where(t => t.Kind == kinds[kindName]).Select(t => t.Name), kindName);
                all.AddRange(names);
            }
            SameSet(all, Staging.Things.Keys, "things");
        }

        [Test]
        public void TheVerbsEveryThingHasMatch()
        {
            var verbs = new List<string>();
            foreach (string row in Rows("| Verb | Example |"))
                verbs.AddRange(Spans(row.Split('|')[1]));
            SameSet(verbs, Staging.Common, "verbs every thing has");
        }

        [Test]
        public void TheSoldiersAndPropsOwnVerbsMatch()
        {
            string soldiers = Paragraph("**Soldiers also**");
            string own = Paragraph("**Props' and effects' own verbs**");
            var mentioned = new HashSet<string>();
            foreach (string span in Spans(soldiers).Concat(Spans(own)))
            {
                string[] words = span.Split(' ');
                // an example may start with its time: 0-1 kie walk 100 166
                int i = char.IsDigit(words[0][0]) ? 1 : 0;
                foreach (string w in words) mentioned.Add(w);
                if (i < words.Length && Staging.Things.ContainsKey(words[i]) && i + 1 < words.Length && !float.TryParse(words[i + 1], out _))
                    Assert.IsTrue(Staging.Has(words[i], words[i + 1]), "the page says " + words[i] + " can '" + words[i + 1] + "'");
            }
            foreach (string v in Staging.FigureVerbs)
                Assert.IsTrue(mentioned.Contains(v), "the page says soldiers can '" + v + "'");
            foreach (Staging.Thing t in Staging.Things.Values)
                foreach (string v in t.Verbs)
                    Assert.IsTrue(mentioned.Contains(v), "the page says " + t.Name + " can '" + v + "'");
            // the words after pose and look are the whole lists
            SameSet(Spans(soldiers).First(s => s.StartsWith("pose ")).Split(' ').Skip(1), Staging.Poses, "poses");
            SameSet(Spans(soldiers).First(s => s.StartsWith("look ")).Split(' ').Skip(1), Staging.Looks, "looks");
            SameSet(Spans(soldiers).First(s => s.StartsWith("arms ")).Split(' ').Skip(1), Staging.Arms, "arms");
        }

        [Test]
        public void TheColoursMatch()
        {
            string row = Rows("| Verb | Example |").First(r => r.Contains("`colour`"));
            string names = Spans(row).First(s => s.StartsWith("white "));
            SameSet(names.Split(' '), Staging.Colours.Keys, "colours");
        }
    }
}
