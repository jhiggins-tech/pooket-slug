using System.Collections.Generic;
using NUnit.Framework;

namespace PooketSlug.Tests
{
    public class KitGuideTests
    {
        [Test]
        public void EverySoldierTheCrewCanPickHasEveryMoveExplained()
        {
            foreach (SoldierDef s in Crew.Freed(null, everyone: true))
            {
                IReadOnlyList<MoveGuide> moves = KitGuide.For(s);
                var slots = new List<KitSlot>();
                foreach (MoveGuide m in moves)
                {
                    Assert.AreSame(s, m.Soldier, m.DemoId);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(m.Name), m.DemoId);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(m.Text), m.DemoId);
                    Assert.IsNotNull(m.Demo, m.DemoId);
                    slots.Add(m.Slot);
                }
                CollectionAssert.AreEqual(Expected(s), slots, s.Name);
            }
        }

        static List<KitSlot> Expected(SoldierDef s)
        {
            var slots = new List<KitSlot> { KitSlot.Fire, KitSlot.Special };
            if (s.Super != null) slots.Add(KitSlot.Super);
            if (s.Ability != null) slots.Add(KitSlot.Ability);
            return slots;
        }

        [Test]
        public void DemoIdsAreUniqueAndFoundAgain()
        {
            var seen = new HashSet<string>();
            foreach (MoveGuide m in KitGuide.All)
            {
                Assert.IsTrue(seen.Add(m.DemoId), "two moves share " + m.DemoId);
                StringAssert.IsMatch("^[a-z0-9]+-(fire|special|super|ability)$", m.DemoId); // a file name in demos/
                Assert.AreSame(m, KitGuide.Find(m.DemoId.ToUpperInvariant()));
            }
            Assert.AreEqual("tones2-fire", KitGuide.Tones2[0].DemoId);
            Assert.IsNull(KitGuide.Find("nobody-fire"));
            Assert.IsNull(KitGuide.Find(null));
        }

        [Test]
        public void EachDemoMapLoadsWithItsSoldierAndSomeGoons()
        {
            foreach (MoveGuide m in KitGuide.All)
            {
                var level = new Level(m.Demo.Map);
                Assert.Greater(level.PlayerStart.x, 0f, m.DemoId);
                int goons = 0;
                foreach (Spawn s in level.Spawns)
                    if (Content.EnemyFor(s.Kind) != null) goons++;
                Assert.Greater(goons, 0, m.DemoId);
                Assert.Greater(m.Demo.Seconds, 2f, m.DemoId);
            }
        }

        [Test]
        public void ADemoPressesAKeyOnTheFrameItGoesDown()
        {
            var demo = new DemoScript(DemoScript.Ground("P"), 3f, false, (1f, 2f, DemoKeys.Fire | DemoKeys.Right), (1.5f, 1.6f, DemoKeys.Super));
            int at1 = 30, at15 = 45, at2 = 60; // frames at 30 fps
            SoldierInput before = demo.InputAt(at1 - 1), first = demo.InputAt(at1), next = demo.InputAt(at1 + 1);
            Assert.IsFalse(before.Fire);
            Assert.IsTrue(first.Fire && first.FirePressed && first.Right);
            Assert.IsTrue(next.Fire && !next.FirePressed);
            Assert.IsTrue(demo.InputAt(at15).SuperPressed);
            Assert.IsFalse(demo.InputAt(at15 + 1).SuperPressed);
            Assert.IsFalse(demo.InputAt(at2).Fire);
            // every press lands once, however the frames fall on the steps
            int presses = 0;
            for (int f = 1; f <= 90; f++) if (demo.InputAt(f).FirePressed) presses++;
            Assert.AreEqual(1, presses);
            Assert.AreEqual(DemoKeys.None, demo.Held(-1f));
        }

        [Test]
        public void TheMoveNamesMatchTheKit()
        {
            Assert.AreEqual("TEN-1", KitGuide.Tones2[0].Name);
            Assert.AreEqual(Content.Tones2.Primary.Hud, KitGuide.Tones2[0].Name);
            Assert.AreEqual(Content.Kie.Primary.Name.ToUpperInvariant(), KitGuide.Kie[0].Name);
            Assert.AreEqual(Content.Kie.Special.Name.ToUpperInvariant(), KitGuide.Kie[1].Name);
            Assert.AreEqual(Content.Kie.Super.Name.ToUpperInvariant(), KitGuide.Kie[2].Name);
            Assert.AreEqual("FIRE", KitGuide.Kie[0].Button);
            Assert.AreEqual("SU", KitGuide.Kie[2].Button);
        }
    
        [Test]
        public void NumbersInTheKitTextComeFromTheTuning()
        {
            string Text(SoldierDef who, KitSlot slot)
            {
                foreach (MoveGuide m in KitGuide.For(who)) if (m.Slot == slot) return m.Text;
                Assert.Fail(who.Name + " " + slot);
                return null;
            }
            Assert.AreEqual(1.5f, Tones2Tuning.Spew.Duration, "the text says a second and a half");
            Assert.AreEqual(15f, Tones2Tuning.Spew.SludgeLife, "the text says about 15 seconds");
            Assert.AreEqual(3, KieTuning.Weasels.Count, "the text says three weasels");
            Assert.AreEqual(3, KieTuning.Trollogram.MaxUp, "the text says up to three");
            Assert.AreEqual(120, KcajTuning.Pills.Count, "the text says 120 pills");
            StringAssert.StartsWith("A second and a half of spew", Text(Content.Tones2, KitSlot.Super));
            StringAssert.Contains("for about 15 seconds.", Text(Content.Tones2, KitSlot.Super));
            StringAssert.StartsWith("Each press tumbles out three weasels that", Text(Content.Kie, KitSlot.Fire));
            StringAssert.Contains("Trollogram of yourself, up to three. Goons", Text(Content.Kie, KitSlot.Special));
            StringAssert.StartsWith("120 pills rain across", Text(Content.Kcaj, KitSlot.Super));
        }
}
}
