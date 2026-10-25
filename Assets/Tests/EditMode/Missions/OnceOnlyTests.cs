using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>
    /// The once-only keys (Core/Missions/OnceOnly.cs): every key the game marks seen is in the list and every key in the
    /// list is marked by something, the keys are unique, and they are pinned as they are in players' saves.
    /// </summary>
    public class OnceOnlyTests
    {
        /// <summary>The keys as they are in saves today. A key here never changes or goes; a new beat adds its key.</summary>
        static readonly string[] Pinned =
        {
            "coop",
            "coda", "debrief", "super",
            "scene-opening", "scene-motor-pool", "scene-freeing-kie", "scene-coop-test", "scene-m2-briefing",
            "scene-freeing-kcaj", "scene-kcaj-tabk",
            "den-first-visit", "moved-in-kcaj",
            "kie-tabk-home",
        };

        static List<string> Keys => OnceOnly.All.Select(b => b.Key).ToList();

        [Test]
        public void TheKeysArePinnedAsTheyAreInSaves()
        {
            foreach (string key in Pinned)
                CollectionAssert.Contains(Keys, key, "a saved key must never change or go: players' saves hold it");
            foreach (string key in Keys)
                CollectionAssert.Contains(Pinned, key, "a new once-only key: pin it here, as it will be in saves");
            // Byte for byte: the UTF-8 the save writes, not only an equal string under some culture.
            foreach (string key in Pinned)
            {
                string mine = Keys.First(k => string.Equals(k, key, StringComparison.Ordinal));
                CollectionAssert.AreEqual(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(mine), key);
            }
        }

        [Test]
        public void TheKeysAreUniqueAndEachSaysWhatItIs()
        {
            CollectionAssert.AllItemsAreUnique(Keys);
            foreach (OnceOnlyBeat beat in OnceOnly.All)
            {
                Assert.IsFalse(string.IsNullOrEmpty(beat.Key));
                Assert.IsFalse(string.IsNullOrEmpty(beat.What), beat.Key + " says what it is");
            }
        }

        [Test]
        public void TheListTakesEachKeyFromItsSource()
        {
            Assert.IsTrue(OnceOnly.Has(Reveals.Coop));
            Assert.IsTrue(OnceOnly.Has(StoryRecord.Coda) && OnceOnly.Has(StoryRecord.Debrief) && OnceOnly.Has(StoryRecord.Super));
            foreach (string name in SceneLibrary.Names)
                Assert.IsTrue(OnceOnly.Has(SceneScript.IdOf(name)), name);
            Assert.IsTrue(OnceOnly.Has(DenBeats.FirstVisit));
            foreach (CrewMember m in Crew.All)
                Assert.AreEqual(!DenBeats.Starter(m.Soldier), OnceOnly.Has(DenBeats.MovedIn(m.Soldier)), m.Name + " moves in unless home for the first visit");
            Assert.IsTrue(OnceOnly.Has(KieHomecoming.Reveal));
            Assert.IsFalse(OnceOnly.Has(Reveals.Tank), "tank is only ?unlock=tank's key: nothing saves it");
        }

        [Test]
        public void AScenesSavedKeyIsItsIdInTheList()
        {
            // SceneFlow marks the scene's own id: for every scene the game plays, that is the list's key.
            foreach (string name in SceneLibrary.Names)
                Assert.AreEqual(SceneScript.IdOf(name), TestScenes.Script(name).Def.Id, name);
        }

        [Test]
        public void TheDensBeatsMarkTheirKeys()
        {
            var profile = new Profile();
            foreach (DenBeat beat in DenBeats.Due(profile, Crew.All.Select(m => m.Soldier).ToList(), DenMode.Solo))
                DenBeats.Played(profile, beat);
            CollectionAssert.IsNotEmpty(profile.seen);
            foreach (string key in profile.seen)
                Assert.IsTrue(OnceOnly.Has(key), key + " is in the list");
            foreach (string key in Keys.Where(k => k == DenBeats.FirstVisit || k.StartsWith("moved-in-", StringComparison.Ordinal)))
                CollectionAssert.Contains(profile.seen, key, "the den marks " + key);
        }

        /// <summary>
        /// Every place the code marks a key seen (<c>MarkSeen(…)</c>, or a <see cref="StoryRecord"/>'s <c>Mark(…)</c>), by its
        /// file and what it passes, and the keys it can save. A new place fails the test until it's here, and its keys
        /// are in the list. Pass-throughs (the record copying another copy's keys, <c>StoryRecord.Mark</c> itself) save
        /// nothing of their own.
        /// </summary>
        static Dictionary<string, Func<IEnumerable<string>>> CallSites => new Dictionary<string, Func<IEnumerable<string>>>
        {
            ["Game.cs: Reveals.Coop"] = () => new[] { Reveals.Coop },
            ["Game.cs: StoryRecord.Coda"] = () => new[] { StoryRecord.Coda },
            ["Game.cs: StoryRecord.Super"] = () => new[] { StoryRecord.Super },
            ["DebriefFlow.cs: StoryRecord.Debrief"] = () => new[] { StoryRecord.Debrief },
            ["SceneFlow.cs: m_Scene.Scene.Id"] = () => SceneLibrary.Names.Select(SceneScript.IdOf),
            ["BayFlow.cs: KieHomecoming.Reveal"] = () => new[] { KieHomecoming.Reveal },
            ["DenBeats.cs: beat.Kind == DenBeatKind.FirstVisit ? FirstVisit : MovedIn(beat.Newcomer)"] = () =>
                new[] { DenBeats.FirstVisit }.Concat(Crew.All.Where(m => !DenBeats.Starter(m.Soldier)).Select(m => DenBeats.MovedIn(m.Soldier))),
            ["Profile.cs: s"] = () => new string[0],
            ["MissionStory.cs: key"] = () => new string[0],
        };

        static List<string> FoundCallSites()
        {
            string scripts = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Scripts");
            var found = new List<string>();
            var call = new Regex(@"\b(MarkSeen|Mark)\(");
            foreach (string file in Directory.GetFiles(scripts, "*.cs", SearchOption.AllDirectories))
            {
                string text = File.ReadAllText(file);
                foreach (Match m in call.Matches(text))
                {
                    int open = m.Index + m.Length, depth = 1, i = open;
                    for (; i < text.Length && depth > 0; i++)
                        if (text[i] == '(') depth++;
                        else if (text[i] == ')') depth--;
                    string arg = text.Substring(open, i - 1 - open).Trim();
                    if (arg.StartsWith("string ", StringComparison.Ordinal)) continue; // the method itself
                    found.Add(Path.GetFileName(file) + ": " + arg);
                }
            }
            return found;
        }

        [Test]
        public void EveryKeyTheGameSavesIsInTheListAndEveryKeyInItIsSaved()
        {
            List<string> found = FoundCallSites();
            foreach (string site in found)
                Assert.IsTrue(CallSites.ContainsKey(site), "a new place marks a key seen (" + site + "): add it to CallSites here and its keys to OnceOnly");
            var saved = new HashSet<string>();
            foreach (KeyValuePair<string, Func<IEnumerable<string>>> site in CallSites)
            {
                CollectionAssert.Contains(found, site.Key, "this place no longer marks a key; take it out of CallSites");
                foreach (string key in site.Value())
                {
                    Assert.IsTrue(OnceOnly.Has(key), site.Key + " saves " + key + ", which isn't in OnceOnly");
                    saved.Add(key);
                }
            }
            foreach (string key in Keys)
                Assert.IsTrue(saved.Contains(key), key + " is in OnceOnly but nothing saves it");
        }
    }
}
