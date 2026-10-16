using System.Collections.Generic;
using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>The sandbox (Core/Sandbox.cs, docs/sandbox.md): its link reader, its setup lines and its knobs.</summary>
    public class SandboxTests
    {
        [Test]
        public void SetupLines_SplitAtSemicolonsAndNewLines_AndSkipNotes()
        {
            List<SetupLine> lines = SetupText.Read("soldier kie;ground=P..s\n// a note\n\ntune:kit.superkills=6");
            Assert.AreEqual(3, lines.Count);
            Assert.AreEqual("soldier", lines[0].Key);
            Assert.AreEqual("kie", lines[0].Rest);
            Assert.AreEqual("P..s", lines[1].Rest, "a map row keeps its markers");
            Assert.AreEqual("tune", lines[2].Key);
            CollectionAssert.AreEqual(new[] { "kit.superkills", "6" }, lines[2].Args);
            Assert.AreEqual(5, lines[2].Number, "lines count from 1, notes and blanks included");
        }

        [Test]
        public void AName_IsAFileBesideThePage_AndNothingElseIs()
        {
            Assert.IsTrue(SetupText.IsName("boss-fight_2"));
            Assert.IsFalse(SetupText.IsName("soldier kie"));
            Assert.IsFalse(SetupText.IsName("../secret"));
            Assert.IsFalse(SetupText.IsName("https:x"));
            Assert.AreEqual("https://x.github.io/pooket-slug/previews/sandbox-mode/sandbox/boss.txt",
                SetupText.FileUrl("https://x.github.io/pooket-slug/previews/sandbox-mode/index.html?sandbox=Boss", "sandbox", "Boss"));
            Assert.IsNull(SetupText.FileUrl("", "sandbox", "boss"), "off the site");
            Assert.IsNull(SetupText.FileUrl("https://x.github.io/pooket-slug/", "sandbox", "a/b"));
        }

        [Test]
        public void AnEmptySandbox_IsTones2OnFlatGroundWithGoons()
        {
            SandboxSetup s = SandboxSetup.Read("");
            Assert.AreSame(Content.Tones2, s.Soldier);
            Assert.IsEmpty(s.Problems);
            Assert.IsNull(s.Script);
            Level level = s.Mission.Load();
            Assert.Greater(level.PlayerStart.x, 0f);
            Assert.That(level.Spawns.Exists(sp => sp.Kind == SpawnKind.Soldier));
            Assert.AreEqual("SANDBOX", s.Label);
        }

        [Test]
        public void ASetup_ReadsEveryLine()
        {
            SandboxSetup s = SandboxSetup.Read("soldier kie;ground P....s....T;air ...d;at boss;boss weak-pilot;cheat invincible super specials tank");
            Assert.IsEmpty(s.Problems, string.Join(", ", s.Problems));
            Assert.AreSame(Content.Kie, s.Soldier);
            Level level = s.Mission.Load();
            Assert.That(level.Spawns.Exists(sp => sp.Kind == SpawnKind.Tank), "the boss tabk");
            Assert.That(level.Spawns.Exists(sp => sp.Kind == SpawnKind.Drone), "the air row");
            Assert.AreEqual("boss", s.At);
            Assert.AreEqual(SandboxBoss.WeakPilot, s.Boss);
            Assert.IsTrue(s.Invincible && s.Super && s.Specials && s.Tank);
        }

        [Test]
        public void AMissionOrAWholeMap_CanBeTheSandbox()
        {
            Assert.AreSame(Missions.One, SandboxSetup.Read("mission 1").Mission);
            Assert.AreSame(Missions.One, SandboxSetup.Read("mission one").Mission);
            SandboxSetup s = SandboxSetup.Read("map ..........\nmap .P...s..T.\nmap ##########");
            Assert.IsEmpty(s.Problems);
            Assert.AreEqual(3, s.Mission.Map.Length);
            Assert.IsTrue(s.Mission.Load().IsSolid(0, 0));
        }

        [Test]
        public void WhatItCantFollow_IsAProblem_ByLine()
        {
            SandboxSetup s = SandboxSetup.Read("soldier nobody;fly away;cheat godmode;ground ....s....");
            Assert.AreEqual(4, s.Problems.Count, string.Join(", ", s.Problems));
            Assert.AreEqual("LINE 1: NO SOLDIER CALLED NOBODY", s.Problems[0]);
            Assert.AreEqual("LINE 2: NO SETTING CALLED FLY", s.Problems[1]);
            Assert.AreEqual("THE MAP HAS NO P, WHERE THE SOLDIER STARTS", s.Problems[3]);
            Assert.IsNotNull(s.Mission, "the default map stands in");
            StringAssert.StartsWith("SANDBOX · 4 PROBLEMS: LINE 1", s.Label);
        }

        [Test]
        public void Keys_MakeAScriptedTake()
        {
            SandboxSetup s = SandboxSetup.Read("keys 0 1 right;keys 0.5 2 fire+up");
            Assert.IsEmpty(s.Problems, string.Join(", ", s.Problems));
            Assert.IsNotNull(s.Script);
            Assert.AreEqual(3f, s.Script.Seconds, 1e-4f, "a second after the last key's let go");
            Assert.AreEqual(DemoKeys.Right | DemoKeys.Fire | DemoKeys.Up, s.Script.Held(0.75f));
            Assert.AreEqual(6f, SandboxSetup.Read("keys 0 1 right;seconds 6").Script.Seconds, 1e-4f);
            Assert.IsFalse(SandboxSetup.KeysOf("left+dance", out _));
            Assert.AreEqual(DemoKeys.Fire | DemoKeys.Up, SandboxSetup.Read("keys 0 1 fire up").Script.Held(0.5f), "a link's '+' arrives as a space");
            Assert.AreEqual(1, SandboxSetup.Read("keys 2 1 left").Problems.Count, "a key let go before it's held");
        }

        [Test]
        public void Knobs_TurnTuning_KitNumbers_AndDefs()
        {
            float gravity = Tuning.Physics.Gravity, ramp = Tones2Tuning.Jet.RampTime;
            int hp = Content.Rifleman.Hp;
            float run = Content.Tones2.RunSpeed;
            try
            {
                SandboxSetup s = SandboxSetup.Read("tune physics.gravity 30;tune Tones2.Jet.RampTime=0.5;tune rifleman.hp 9;tune tones2.runspeed 7");
                s.ApplyTunes();
                Assert.IsEmpty(s.Problems, string.Join(", ", s.Problems));
                Assert.AreEqual(30f, Tuning.Physics.Gravity);
                Assert.AreEqual(0.5f, Tones2Tuning.Jet.RampTime, "a crewmate's kit numbers");
                Assert.AreEqual(9, Content.Rifleman.Hp, "a def");
                Assert.AreEqual(7f, Content.Tones2.RunSpeed, "tones2 is both his kit and his soldier");
                Assert.AreEqual("30", Knobs.Get("physics.gravity"));
            }
            finally
            {
                Tuning.Physics.Gravity = gravity;
                Tones2Tuning.Jet.RampTime = ramp;
                Assert.IsTrue(Knobs.TrySet("rifleman.hp", hp.ToString(), out _));
                Assert.IsTrue(Knobs.TrySet("tones2.runspeed", run.ToString(System.Globalization.CultureInfo.InvariantCulture), out _));
            }
            Assert.AreEqual(hp, Content.Rifleman.Hp);
        }

        [Test]
        public void Knobs_SayWhyNot()
        {
            Assert.IsFalse(Knobs.TrySet("physics.wobble", "1", out string problem));
            Assert.AreEqual("no knob called physics.wobble", problem);
            Assert.IsFalse(Knobs.TrySet("camera.viewheight", "20", out problem));
            Assert.AreEqual("camera.viewheight is built into the game", problem);
            Assert.IsFalse(Knobs.TrySet("physics.gravity", "lots", out problem));
            Assert.AreEqual("physics.gravity can't be lots", problem);
            Assert.IsFalse(Knobs.TrySet("tones2.jet", "1", out problem));
            Assert.AreEqual("tones2.jet isn't a number or an on/off", problem);
        }

        [Test]
        public void TheHowTosKnobs_AllExist()
        {
            foreach (string knob in new[] { "physics.gravity", "lives.spare", "kit.superkills", "tones2.jet.ramptime", "kie.trollogram.swapcooldown",
                         "tones2.runspeed", "kie.specialrefill", "rifleman.hp", "pistol.cooldown", "grenade.damage" })
                Assert.IsNotNull(Knobs.Get(knob), knob + " (docs/sandbox.md)");
        }

        [Test]
        public void EverySandboxFile_ReadsCleanly()
        {
            string folder = System.IO.Path.Combine(UnityEngine.Application.dataPath, "..", "sandbox");
            string[] files = System.IO.Directory.GetFiles(folder, "*.txt");
            Assert.IsNotEmpty(files, "the examples in sandbox/");
            foreach (string file in files)
            {
                string name = System.IO.Path.GetFileNameWithoutExtension(file);
                Assert.IsTrue(SetupText.IsName(name) && name == name.ToLowerInvariant(), name + ": a ?sandbox= name, in lower case");
                SandboxSetup s = SandboxSetup.Read(System.IO.File.ReadAllText(file));
                Assert.IsEmpty(s.Problems, name + ": " + string.Join(", ", s.Problems));
                foreach (string tune in s.Tunes)
                    Assert.IsNotNull(Knobs.Get(tune.Substring(0, tune.IndexOf(' '))), name + ": " + tune);
            }
        }

        [Test]
        public void EveryListedKnob_CanBeReadBack()
        {
            List<string> all = Knobs.All();
            Assert.Greater(all.Count, 100, "tuning, three kits and the defs");
            CollectionAssert.Contains(all, "kit.superkills " + Tuning.Kit.SuperKills);
            foreach (string knob in all)
            {
                string path = knob.Substring(0, knob.IndexOf(' '));
                Assert.AreEqual(knob.Substring(path.Length + 1), Knobs.Get(path), path);
            }
        }
    }
}
