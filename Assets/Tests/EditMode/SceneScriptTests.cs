using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace PooketSlug.Tests
{
    /// <summary>Scene scripts (Core/SceneScript.cs, docs/scenes.md), their staging (Core/Staging.cs) and the library that picks a copy.</summary>
    public class SceneScriptTests
    {
        const string Tiny = "net 9\nshot: Hello\nlength 4\nset room\ntones2 at 56\n1 tones2: HI\n";

        static SceneScript Read(string text) => SceneScript.Read("test", text);

        [Test]
        public void EveryGameSceneReadsCleanly()
        {
            foreach (string name in SceneLibrary.Names)
            {
                SceneScript s = TestScenes.Script(name);
                Assert.IsEmpty(s.Problems, name + ": " + string.Join("; ", s.Problems));
                Assert.AreEqual("scene-" + name, s.Def.Id, "the id the profile remembers it by");
            }
            Assert.IsEmpty(SceneCheck.Numbers(TestScenes.All.Values), "every scene has its own number for a partner's phone");
            foreach (SceneScript s in TestScenes.All.Values)
                Assert.IsTrue(SetupText.IsName(s.Name) && s.Name == s.Name.ToLowerInvariant(), s.Name + ": a ?scene= name, in lower case");
        }

        [Test]
        public void TheScenesKeepTheirNumbersAndShapes()
        {
            // a partner's phone knows a scene by its number, and the tests elsewhere count shots
            Assert.AreEqual(1, TestScenes.Opening.NetId);
            Assert.AreEqual(2, TestScenes.MotorPool.NetId);
            Assert.AreEqual(3, TestScenes.FreeingKie.NetId);
            Assert.AreEqual(63, TestScenes.CoopTest.NetId);
            Assert.AreEqual(new[] { "Outside the hangout", "The door", "Inside, raided", "A straggler", "The keycard", "Heading out" },
                System.Array.ConvertAll(TestScenes.Opening.Shots, s => s.Name));
            Assert.AreEqual("MISSION 1 · THE MILITARY BASE\nFIND THE CREW", TestScenes.Opening.Shots[5].Lines[2].Text, "'|' is a new line");
            Assert.AreEqual("opening-4a", TestScenes.Opening.Shots[3].Lines[1].Voice);
        }

        [Test]
        public void AScriptReadsLinesCuesAndAnIntro()
        {
            SceneScript s = Read("net 7\n// a note\nshot: Out front\nlength 8.5\nset street\nintro 4 tones2: TONES2\n" +
                                 "4.3 narrator opening-1: Friday night: snacks.\n6.4 tones2: SNACKS ARE HERE LADS\n0.5 cue door\n4.1 say mission-start\n");
            Assert.IsEmpty(s.Problems, string.Join("; ", s.Problems));
            CutsceneShot shot = s.Def.Shots[0];
            Assert.AreEqual("Out front", shot.Name);
            Assert.AreEqual(8.5f, shot.Seconds);
            Assert.AreEqual("street", shot.Set);
            Assert.AreEqual("tones2", shot.Intro.Figure);
            Assert.AreEqual("TONES2", shot.Intro.Name);
            Assert.AreEqual(2, shot.Lines.Length);
            Assert.AreEqual(Speaker.Narrator, shot.Lines[0].Who);
            Assert.AreEqual("opening-1", shot.Lines[0].Voice);
            Assert.AreEqual("Friday night: snacks.", shot.Lines[0].Text, "a colon in the words is just a colon");
            Assert.AreEqual(Speaker.Tones2, shot.Lines[1].Who);
            Assert.AreEqual(new[] { "door", Cues.SayMissionStart }, System.Array.ConvertAll(shot.Cues, c => c.Id));
        }

        [Test]
        public void MistakesAreProblemsWithTheirLineNumbers()
        {
            void Says(string text, string expected)
            {
                List<string> problems = Read(text).Problems;
                Assert.IsTrue(problems.Exists(p => p.Contains(expected)), "expected \"" + expected + "\" in: " + string.Join("; ", problems));
            }

            Says(Tiny + "2 tones3 at 4\n", "line 7: no thing called 'tones3'");
            Says(Tiny + "2 cue dor\n", "line 7: no cue called 'dor'");
            Says(Tiny + "2 tones2 fly 4\n", "tones2 can't 'fly'");
            Says(Tiny + "2 tones2 walk 4 8\n", "'walk' runs over a window");
            Says(Tiny + "2-1 tones2 walk 4 8\n", "ends before it starts");
            Says(Tiny + "9 tones2: LATE\n", "after the shot's ended");
            Says(Tiny + "1 kie opening-1: HI\n", "only the narrator's lines are recorded");
            Says(Tiny + "1 window colour mauve\n", "no colour called 'mauve'");
            Says(Tiny + "intro 1 tones2: TONES2\n", "build up");
            Says(Tiny + "set lawn\n", "no set called 'lawn'");
            Says(Tiny + "tones2 holds sign 1\n", "across and up");
            Says("shot: No net\nlength 2\nset room\n", "needs its number");
            Says("net 4\nshot: Nothing\n", "needs its length");
            Says("net 4\nlength 3\n", "start a shot");
        }

        [Test]
        public void AThingIsOnStageFromItsFirstLineUnlessShownOrHidden()
        {
            SceneScript s = Read("net 9\nshot: A\nlength 8\nset room\n" +
                                 "tones2 at 56\n2.1 kie show\nkie at 140 12\n0.45 goon hide\ngoon at 178\n4.2-6.4 card show\n4.2-4.55 card move 176 14 176 0 in\n");
            Assert.IsEmpty(s.Problems, string.Join("; ", s.Problems));
            var stage = new ShotStage(s.Def.Shots[0]);
            Assert.IsTrue(stage.Shown("tones2", 0f));
            Assert.IsFalse(stage.Shown("bag", 0f), "never mentioned: not there");
            Assert.IsFalse(stage.Shown("kie", 2f), "shown at 2.1");
            Assert.IsTrue(stage.Shown("kie", 2.2f));
            Assert.IsTrue(stage.Shown("goon", 0.2f), "there until it's hidden");
            Assert.IsFalse(stage.Shown("goon", 0.5f));
            Assert.IsFalse(stage.Shown("card", 4f));
            Assert.IsTrue(stage.Shown("card", 5f));
            Assert.IsFalse(stage.Shown("card", 6.5f), "a window's show ends with it, even with a move after it");
        }

        [Test]
        public void MovesWalksAndHoldsPlaceThings()
        {
            SceneScript s = Read("net 9\nshot: A\nlength 8\nset room\n" +
                                 "tones2 at 56\ntones2 holds bag cup\n5.4-6.2 tones2 walk 56 164\n2.2-2.4 bag move 66 12 66 0\n" +
                                 "4.2-4.55 card move 176 14 176 0 in\n2.2 kie holds cup -7 9\nkie at 100\n");
            Assert.IsEmpty(s.Problems, string.Join("; ", s.Problems));
            var stage = new ShotStage(s.Def.Shots[0]);
            Assert.AreEqual(new Vector2(56f, 0f), stage.Where("tones2", 1f, 0f));
            Assert.AreEqual(new Vector2(50f, 9f), stage.Where("bag", 1f, 0f), "in his hand");
            Assert.AreEqual(new Vector2(66f, 6f), stage.Where("bag", 2.3f, 0f), "dropping");
            Assert.AreEqual(new Vector2(66f, 0f), stage.Where("bag", 7f, 0f), "and it stays where it fell");
            Assert.AreEqual(110f, stage.Where("tones2", 5.8f, 0f).x, 0.01f, "halfway over");
            Assert.IsTrue(stage.Walking("tones2", 5.8f));
            Assert.AreEqual("run", stage.Pose("tones2", 5.8f));
            Assert.AreEqual("idle", stage.Pose("tones2", 6.3f));
            Assert.AreEqual(164f, stage.Where("tones2", 7f, 0f).x);
            Assert.AreEqual(14f - 14f * 0.25f, stage.Where("card", 4.2f + 0.35f / 2f, 0f).y, 0.01f, "easing in: a quarter of the way at halfway");
            Assert.AreEqual(new Vector2(65f, 13f), stage.Where("cup", 1f, 0f), "tones2's");
            Assert.AreEqual(new Vector2(93f, 9f), stage.Where("cup", 3f, 0f), "then kie's");
        }

        [Test]
        public void ColoursTurnsSizesAndTheCamera()
        {
            SceneScript s = Read("net 9\nshot: A\nlength 8\nset street\nshow window door\n" +
                                 "1.2-1.8 window flicker warm cyan 14\n1.8 window colour cyan\n0.5-0.8 door size 1 1 0.35 1\n0.5-0.8 door turn 0 -9\n" +
                                 "0-2 camera 1.2 20 -10 smooth\nspark blink 9 3\n");
            Assert.IsEmpty(s.Problems, string.Join("; ", s.Problems));
            var stage = new ShotStage(s.Def.Shots[0]);
            Assert.AreEqual(Staging.Warm, stage.Colour("window", 1f, 0f), "its own colour first");
            Assert.AreEqual(Staging.Warm, stage.Colour("window", 1.3f, 0f), "a blink of the old colour");
            Assert.AreEqual(Staging.Cyan, stage.Colour("window", 1.3f, 1f / 14f), "then the new");
            Assert.AreEqual(Staging.Cyan, stage.Colour("window", 2f, 0f));
            Assert.AreEqual(new Vector2(0.35f, 1f), stage.Size("door", 1f));
            Assert.AreEqual(-9f, stage.Turn("door", 1f));
            stage.Camera(1f, out float zoom, out Vector2 look);
            Assert.AreEqual(1.1f, zoom, 0.001f);
            Assert.AreEqual(new Vector2(10f, -5f), look);
            stage.Camera(5f, out zoom, out look);
            Assert.AreEqual(1.2f, zoom, 0.001f);
            Assert.IsTrue(stage.Lit("spark", 1f, 0f));
            Assert.IsFalse(stage.Lit("spark", 1f, 1f / 9f));
        }

        [Test]
        public void TheLibraryTakesTheSitesCopyOnlyWhenItsClean()
        {
            var lib = new SceneLibrary();
            Assert.IsEmpty(lib.AddBuiltIn("tiny", Tiny));
            Assert.AreEqual("HI", lib.Get("tiny").Shots[0].Lines[0].Text);
            Assert.IsFalse(lib.UseSite("tiny", Tiny.Replace("HI", "HI") + "2 cue dor\n", out string why));
            StringAssert.Contains("problem", why);
            Assert.IsFalse(lib.UseSite("tiny", Tiny.Replace("net 9", "net 8"), out why), "a partner's phone knows it by its number");
            Assert.IsFalse(lib.UseSite("tiny", Tiny, out why), "the same as the built-in one: nothing to take");
            Assert.AreEqual(0, lib.Versions, "nothing from the site");
            Assert.IsTrue(lib.UseSite("tiny", Tiny.Replace("HI", "HELLO"), out why), why);
            Assert.AreEqual("HELLO", lib.Get("tiny").Shots[0].Lines[0].Text);
            Assert.IsTrue(lib.FromSite("tiny"));
            Assert.AreNotEqual(0, lib.Versions);
        }

        [Test]
        public void TwoPhonesPlayTheSitesCopyTogetherOnlyIfBothHaveIt()
        {
            var a = new SceneLibrary();
            var b = new SceneLibrary();
            a.AddBuiltIn("tiny", Tiny);
            b.AddBuiltIn("tiny", Tiny);
            a.UseSite("tiny", Tiny.Replace("HI", "HELLO"), out _);
            int va = a.StartCoop(), vb = b.StartCoop();
            Assert.AreNotEqual(va, vb);
            Assert.AreEqual("HI", a.ForCoop("tiny", vb).Shots[0].Lines[0].Text, "b hasn't the new copy, so both play the built-in one");
            Assert.AreEqual("HI", b.ForCoop("tiny", va).Shots[0].Lines[0].Text);

            b.UseSite("tiny", Tiny.Replace("HI", "HELLO"), out _);
            Assert.AreEqual(vb, b.Versions, "fetched mid-session: it doesn't count until the next");
            Assert.AreEqual("HI", b.ForCoop("tiny", va).Shots[0].Lines[0].Text);
            Assert.AreEqual("HELLO", b.Get("tiny").Shots[0].Lines[0].Text, "but plays alone");
            vb = b.StartCoop();
            Assert.AreEqual(va, vb);
            Assert.AreEqual("HELLO", a.ForCoop("tiny", vb).Shots[0].Lines[0].Text, "the same copies: the new one, on both");
            Assert.AreEqual("HELLO", b.ForCoop("tiny", va).Shots[0].Lines[0].Text);
        }

        [Test]
        public void EveryThingAndVerbInTheVocabularyReads()
        {
            foreach (KeyValuePair<string, Staging.Thing> kv in Staging.Things)
                Assert.IsTrue(Staging.Has(kv.Key, "show"), kv.Key);
            Assert.IsTrue(Staging.Has("kie", "walk"));
            Assert.IsFalse(Staging.Has("bag", "walk"), "props don't walk");
            Assert.IsTrue(Staging.Has("shutter", "open"));
            Assert.IsFalse(Staging.Has("door", "open"), "a verb belongs to the things it's for");
            Assert.IsTrue(Staging.TryColour("#ff8000", out Color c));
            Assert.AreEqual(new Color(1f, 128f / 255f, 0f), c);
            Assert.IsTrue(Staging.TryColour("#ff800080", out c));
            Assert.AreEqual(128f / 255f, c.a, 0.001f);
        }

        [Test]
        public void TheHashIsPinned()
        {
            // the scene deploy's voice list (tools/scenecheck) and the game hash the same way; FNV-1a 32
            Assert.AreEqual(unchecked((int)2166136261), SceneScript.Hash(""));
            Assert.AreEqual(unchecked((int)0xe40c292c), SceneScript.Hash("a"));
        }

        [Test]
        public void TheVoiceListReadsBack()
        {
            var clip = new VoiceClip { Id = "opening-1", Hash = unchecked((int)0xe40c292c), Seconds = 2.5f };
            List<VoiceClip> back = VoiceClip.Read(clip + "\n\nnot a line\nbad/id 00000001 1\n");
            Assert.AreEqual(1, back.Count);
            Assert.AreEqual("opening-1", back[0].Id);
            Assert.AreEqual(clip.Hash, back[0].Hash);
            Assert.AreEqual(2.5f, back[0].Seconds, 0.001f);
        }

        [Test]
        public void CoopPlaysTheSiteCopyOnlyWhenBothPhonesHaveIt()
        {
            const string Built = "net 9\nshot: A\nlength 3\nset street\n0.2 tones2: HI.\n";
            var library = new SceneLibrary();
            CollectionAssert.IsEmpty(library.AddBuiltIn("x", Built));
            Assert.IsFalse(library.UseSite("x", Built, out _), "the same as the built-in copy");
            Assert.IsFalse(library.UseSite("x", Built.Replace("net 9", "net 8"), out string why), why);
            Assert.IsTrue(library.UseSite("x", Built.Replace("HI.", "HELLO."), out _));
            Assert.AreEqual("HELLO.", library.Get("x").Shots[0].Lines[0].Text);
            int mine = library.StartCoop();
            Assert.AreNotEqual(0, mine);
            Assert.AreEqual("HELLO.", library.ForCoop("x", mine).Shots[0].Lines[0].Text);
            Assert.AreEqual("HI.", library.ForCoop("x", 0).Shots[0].Lines[0].Text, "a partner on the built-in copies");
            Assert.AreEqual("HI.", library.ForCoop("x", -1).Shots[0].Lines[0].Text, "no partner yet");
        }
    }
}
