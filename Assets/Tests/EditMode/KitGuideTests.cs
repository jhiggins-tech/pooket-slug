using System.Collections.Generic;
using NUnit.Framework;

namespace PooketSlug.Tests
{
    public class KitGuideTests
    {
        [Test]
        public void EverySoldierTheCrewCanPickHasEveryMoveExplained()
        {
            foreach (SoldierDef s in Crew.Freed)
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
            const float dt = 1f / DemoScript.Fps;
            SoldierInput before = demo.InputAt(1f - dt, dt), first = demo.InputAt(1f, dt), next = demo.InputAt(1f + dt, dt);
            Assert.IsFalse(before.Fire);
            Assert.IsTrue(first.Fire && first.FirePressed && first.Right);
            Assert.IsTrue(next.Fire && !next.FirePressed);
            Assert.IsTrue(demo.InputAt(1.5f, dt).SuperPressed);
            Assert.IsFalse(demo.InputAt(2f, dt).Fire);
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
    }
}
