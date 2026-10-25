using NUnit.Framework;

namespace PooketSlug.Tests
{
    public class BossTankTests
    {
        [TestCase(-8f, 0f, 45f)]
        [TestCase(-5f, -1f, 60f)]
        [TestCase(6f, 2f, 55f)]
        public void TheBossTanksShotLandsWhereItAimed(float dx, float dy, float lob)
        {
            const float g = 18f;
            float v = BossTankSpec.PowerFor(dx, dy, lob, g);
            Assert.IsFalse(float.IsNaN(v));
            // fly it: at the time it crosses dx, it's at dy
            float a = lob * UnityEngine.Mathf.Deg2Rad;
            float t = UnityEngine.Mathf.Abs(dx) / (v * UnityEngine.Mathf.Cos(a));
            float y = v * UnityEngine.Mathf.Sin(a) * t - 0.5f * g * t * t;
            Assert.AreEqual(dy, y, 1e-3f);
        }

        [Test]
        public void ATooFlatLobCantReachAHighTarget()
        {
            Assert.IsTrue(float.IsNaN(BossTankSpec.PowerFor(-4f, 6f, 30f, 18f)), "tan 30° × 4 is under 6: no speed gets there");
        }

        [Test]
        public void TheBossTankGetsQuickerAsItsHurt()
        {
            BossTankSpec spec = Content.BossTank;
            Assert.AreEqual(1f, spec.Rage(1f), 1e-6f);
            Assert.AreEqual(spec.MaxRage, spec.Rage(0f), 1e-6f);
            Assert.Greater(spec.Rage(0.5f), 1f);
            Assert.Less(spec.Rage(0.5f), spec.MaxRage);
            Assert.Greater(spec.MaxAngle, 90f, "it can lob over its own back");
        }

        [Test]
        public void TheTabkWakesWhenItsInViewOrTheViewCantGoFurther()
        {
            Assert.IsFalse(BossTankSpec.Wakes(viewRight: 100f, tankX: 110f, halfWidth: 2f, scrollLimit: 300f));
            Assert.IsTrue(BossTankSpec.Wakes(112.5f, 110f, 2f, 300f), "all of it in view");
            // kcaj's parks 3.5 tiles from the map's end: a 20-tile co-op screen there can't show it with room to spare
            Assert.IsFalse(BossTankSpec.Wakes(205f, 206.5f, 3f, 210f));
            Assert.IsTrue(BossTankSpec.Wakes(210f, 206.5f, 3f, 210f), "the view's gone as far as it can");
        }

        [Test]
        public void TheBossTanksShellsGoOnTheWire()
        {
            // Its shells are enemy shots, so a co-op partner is sent each one by its weapon id.
            Assert.AreSame(Content.BossCannon, Content.Weapon(Content.BossCannon.Id));
            Assert.AreSame(Content.BigShell, Content.BossCannon.Projectile);
            Assert.IsTrue(Content.BigShell.Explodes);
        }

        [Test]
        public void TheLandingMarkerBlinksOnTheBeatItAlwaysHad()
        {
            // the old inline blink, before it went through Fx: nothing a player sees changes
            for (int c = 0; c <= 20; c++)
                for (int i = 0; i < 3000; i++)
                {
                    float charge = c / 20f, anim = i * 0.0013f;
                    bool old = (int)(anim * UnityEngine.Mathf.Lerp(6f, 20f, charge)) % 2 == 0;
                    Assert.AreEqual(old, BossTankSpec.MarkerLit(anim, charge), "at " + anim + ", charged " + charge);
                }
        }
    }
}
