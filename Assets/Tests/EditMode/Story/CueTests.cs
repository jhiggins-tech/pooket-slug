using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>The cutscene cues (Core/Story/Cues.cs): every cue a scene names does something.</summary>
    public class CueTests
    {
        static IEnumerable<string> SceneCues()
        {
            foreach (CutsceneDef scene in new[] { TestScenes.Opening, TestScenes.MotorPool, TestScenes.FreeingKie, TestScenes.CoopTest })
            foreach (CutsceneShot shot in scene.Shots)
            foreach (CutsceneCue cue in shot.Cues)
                yield return cue.Id;
        }

        static List<string> Constants()
        {
            var ids = new List<string>();
            foreach (FieldInfo f in typeof(Cues).GetFields(BindingFlags.Public | BindingFlags.Static))
                if (f.IsLiteral && f.FieldType == typeof(string) && f.Name != nameof(Cues.SayPrefix))
                    ids.Add((string)f.GetRawConstantValue());
            return ids;
        }

        [Test]
        public void EveryCueInEveryScene_IsHandled()
        {
            int count = 0;
            foreach (string id in SceneCues())
            {
                Assert.IsTrue(Cues.Handled(id), "nothing handles cue \"" + id + "\"");
                count++;
            }
            Assert.Greater(count, 20, "the scenes' cues were all read");
        }

        [Test]
        public void EveryCueConstant_IsHandled()
        {
            List<string> ids = Constants();
            Assert.AreEqual(ids.Count, new HashSet<string>(ids).Count, "no two cues share an id");
            foreach (string id in ids) Assert.IsTrue(Cues.Handled(id), "nothing handles Cues constant \"" + id + "\"");
            Assert.AreEqual(Cues.Effects.Count + 1, ids.Count, "every effect has a constant (and so does the one say: cue)");
        }

        [Test]
        public void TheIntroCues_AreHandled()
        {
            Assert.IsTrue(Cues.Handled(CutscenePlayer.BuildUpCue));
            Assert.IsTrue(Cues.Handled(CutscenePlayer.IntroCue));
        }

        [Test]
        public void EveryEffect_PlaysSomething()
        {
            foreach (KeyValuePair<string, CueEffect> kv in Cues.Effects)
            {
                CueEffect e = kv.Value;
                Assert.AreNotEqual(CueSound.None, e.Sound, kv.Key + " has a sound");
                Assert.That(e.Volume, Is.InRange(0.05f, 1f), kv.Key + "'s volume");
                if (e.Sound2 != CueSound.None) Assert.That(e.Volume2, Is.InRange(0.05f, 1f), kv.Key + "'s second volume");
            }
        }

        [Test]
        public void SayCues_NameTheirLine()
        {
            Assert.AreEqual("mission-start", Cues.SaidLine(Cues.SayMissionStart));
            Assert.AreEqual("warning", Cues.SaidLine(Cues.Say("warning")));
            Assert.IsNull(Cues.SaidLine(Cues.SayPrefix), "a say: cue with no line");
            Assert.IsNull(Cues.SaidLine(Cues.Door));
            Assert.IsFalse(Cues.Handled("dor"), "a typo does nothing, and the test above catches it in a scene");
            Assert.IsFalse(Cues.Handled(null));
        }
    }
}
