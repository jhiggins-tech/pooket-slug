using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>The settings' CONTROLS lines: each kit button is named once the player can use it, with its real keys.</summary>
    public class ControlsLinesTests
    {
        static string All(bool touch, bool super, bool ability, bool coop) =>
            string.Join("\n", ControlsLines.For(touch, false, super, ability, coop));

        [TestCase(true)]
        [TestCase(false)]
        public void ThereAreAlwaysThreeLines(bool touch)
        {
            Assert.AreEqual(3, ControlsLines.For(touch, false, false, false, false).Length);
            Assert.AreEqual(3, ControlsLines.For(touch, true, true, true, true).Length);
        }

        [Test]
        public void TheAbilityIsNamedOnlyForASoldierWithOne()
        {
            StringAssert.DoesNotContain("AB  ", All(false, true, false, true));
            StringAssert.DoesNotContain("AB  ", All(true, true, false, true));
            StringAssert.Contains("AB  B / I", All(false, false, true, false));
            StringAssert.Contains("AB  ABILITY", All(true, false, true, false));
        }

        [Test]
        public void TheTeamSuperIsNamedOnlyOnceCoopIsOpen()
        {
            StringAssert.DoesNotContain("TEAM", All(false, true, true, false));
            StringAssert.DoesNotContain("TEAM", All(true, true, true, false));
            StringAssert.Contains("TEAM  T / Y", All(false, true, false, true));
            StringAssert.Contains("TEAM  BOTH SUPERS AT ONCE", All(true, true, false, true));
        }

        [Test]
        public void TheSuperIsNamedOnlyOnceItsBeenMet()
        {
            StringAssert.DoesNotContain("SUPER  V / U", All(false, false, false, false));
            StringAssert.Contains("SUPER  V / U", All(false, true, false, false));
            StringAssert.DoesNotContain("SU (", All(true, false, false, false));
            StringAssert.Contains("SU (SUPER, FROM KILLS)", All(true, true, false, false));
        }

        [Test]
        public void TheThumbsSwapForTheLeftHanded()
        {
            StringAssert.StartsWith("LEFT THUMB", ControlsLines.For(true, false, false, false, false)[0]);
            StringAssert.StartsWith("RIGHT THUMB", ControlsLines.For(true, true, false, false, false)[0]);
            StringAssert.StartsWith("LEFT THUMB", ControlsLines.For(true, true, false, false, false)[1]);
        }

        [Test]
        public void OnlySoldiersWithACooldownHaveAnAbility()
        {
            Assert.IsFalse(ControlsLines.HasAbility(null));
            foreach (SoldierDef s in new[] { Content.Trooper, Content.Tones2, Content.Kie, Content.Kcaj })
                Assert.AreEqual(s.Ability != null && s.AbilityCooldown > 0f, ControlsLines.HasAbility(s), s.Name);
            var with = new SoldierDef(250, "test", Content.Tones2.Primary, Content.Tones2.Special, 1, ability: Content.Tones2.Primary, abilityCooldown: 3f);
            Assert.IsTrue(ControlsLines.HasAbility(with));
            Assert.AreEqual(with.AbilityCooldown > 0f, new KitMeters(1, 1, 1f, 1, with.AbilityCooldown).HasAbility, "the HUD agrees");
        }

        [Test]
        public void EveryLineFitsTheScreen()
        {
            foreach (bool touch in new[] { true, false })
                foreach (string line in ControlsLines.For(touch, true, true, true, true))
                    Assert.LessOrEqual(line.Length, 100, line);
        }
    }
}
