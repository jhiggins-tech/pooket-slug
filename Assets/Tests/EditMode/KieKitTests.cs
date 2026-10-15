using NUnit.Framework;
using UnityEngine;

namespace PooketSlug.Tests
{
    /// <summary>kie's kit (<see cref="KieTuning"/>): Weasel Pop.</summary>
    public class KieKitTests
    {
        static readonly WeaselPopSpec Spec = new WeaselPopSpec();

        [Test]
        public void APressTumblesOutThreeWeaselsLobbedForward()
        {
            Vector2[] throws = Spec.Volley(Vector2.right, Vector2.zero);
            Assert.AreEqual(3, throws.Length);
            foreach (Vector2 v in throws)
            {
                Assert.Greater(v.x, 0f, "thrown the way kie aims");
                Assert.Greater(v.y, 0f, "lobbed, so it reaches over cover");
            }
        }

        [Test]
        public void AVolleySpreadsOut()
        {
            Vector2[] throws = Spec.Volley(Vector2.right, Vector2.zero);
            Assert.AreNotEqual(throws[0], throws[1]);
            Assert.AreNotEqual(throws[1], throws[2]);
            Assert.Greater(throws[0].magnitude, throws[2].magnitude, "each one softer, so they land apart");
        }

        [Test]
        public void TheyGoTheWayKieFacesAndAimsUp()
        {
            foreach (Vector2 v in Spec.Volley(Vector2.left, Vector2.zero)) Assert.Less(v.x, 0f);
            foreach (Vector2 v in Spec.Volley(Vector2.up, Vector2.zero)) Assert.Greater(v.y, Spec.ThrowSpeed * 0.5f);
        }

        [Test]
        public void RunningCarriesThemAlong()
        {
            Vector2 still = Spec.Volley(Vector2.right, Vector2.zero)[1], running = Spec.Volley(Vector2.right, new Vector2(5f, 0f))[1];
            Assert.Greater(running.x, still.x);
        }

        [Test]
        public void WeaselsHuntGoonsOnTheGroundInReach()
        {
            var weasel = new Vector2(10f, 2f);
            Assert.IsTrue(Spec.CanHunt(weasel, new Vector2(14f, 2f), false));
            Assert.IsTrue(Spec.CanHunt(weasel, new Vector2(6f, 3f), false), "behind it too, and up a step");
            Assert.IsFalse(Spec.CanHunt(weasel, new Vector2(10f + Spec.HuntRange + 1f, 2f), false), "too far");
            Assert.IsFalse(Spec.CanHunt(weasel, new Vector2(11f, 2f + Spec.HuntHeight + 1f), false), "up on a ledge");
        }

        [Test]
        public void WeaselsCantReachFlyers()
        {
            Assert.IsFalse(Spec.CanHunt(new Vector2(10f, 2f), new Vector2(10.5f, 2f), true));
        }

        [Test]
        public void AWeaselPopsRightUnderAGoon()
        {
            Assert.IsTrue(Spec.Under(10f, 10.1f, 0.3f));
            Assert.IsFalse(Spec.Under(10f, 11f, 0.3f));
            Assert.IsTrue(Spec.Under(10f, 10.8f, 1.5f), "under a wide one, such as a tank, from nearer its edge");
        }

        [Test]
        public void WeaselPopIsKiesUnlimitedPrimary()
        {
            Assert.AreSame(Content.WeaselPop, Content.Kie.Primary);
            Assert.AreEqual(WeaponBehaviour.Weasels, Content.WeaselPop.Behaviour);
            Assert.AreEqual(0, Content.WeaselPop.Ammo);
            Assert.Greater(Spec.MaxOut, Spec.Count);
        }
    
        static readonly TrollogramSpec Troll = new TrollogramSpec();

        [Test]
        public void SpDropsAHologramWithACharge()
        {
            Assert.AreEqual(TrollAction.Drop, Troll.OnSpecial(false, 99f, 1));
            Assert.AreEqual(TrollAction.Nothing, Troll.OnSpecial(false, 99f, 0), "no charges, no hologram");
        }

        [Test]
        public void SpAgainSwapsForFreeButNotStraightBack()
        {
            Assert.AreEqual(TrollAction.Swap, Troll.OnSpecial(true, 99f, 0), "swapping costs no charge");
            Assert.AreEqual(TrollAction.Nothing, Troll.OnSpecial(true, Troll.SwapCooldown * 0.5f, 3));
            Assert.AreEqual(TrollAction.Swap, Troll.OnSpecial(true, Troll.SwapCooldown, 3));
        }

        [Test]
        public void GoonsShootTheHologramOnlyWhileItsOnScreen()
        {
            Assert.IsTrue(TrollogramSpec.Draws(true, 10f, 0f, 20f));
            Assert.IsFalse(TrollogramSpec.Draws(true, 25f, 0f, 20f), "off screen: they go back to kie");
            Assert.IsFalse(TrollogramSpec.Draws(false, 10f, 0f, 20f));
        }

        [Test]
        public void TrollogramIsKiesSpecial()
        {
            Assert.AreSame(Content.Trollogram, Content.Kie.Special);
            Assert.AreEqual(WeaponBehaviour.Trollogram, Content.Trollogram.Behaviour);
            Assert.Greater(Troll.Damage, 0);
            Assert.Greater(Troll.Life, Troll.SwapCooldown);
        }
    }
}
