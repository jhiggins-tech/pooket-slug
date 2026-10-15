using NUnit.Framework;
using UnityEngine;

namespace PooketSlug.Tests
{
    public class KitMetersTests
    {
        [Test]
        public void TheSpecialSpendsAndRefillsOneChargeAtATime()
        {
            var kit = new KitMeters(3, 3, 6f, 12, 0f);
            Assert.IsTrue(kit.UseSpecial());
            Assert.AreEqual(2, kit.Specials);
            kit.Tick(3f);
            Assert.AreEqual(0.5f, kit.SpecialRefillProgress, 1e-4f);
            kit.Tick(3f);
            Assert.AreEqual(3, kit.Specials);
            kit.Tick(60f);
            Assert.AreEqual(3, kit.Specials, "refilling stops when full");
            Assert.AreEqual(0f, kit.SpecialRefillProgress);
        }

        [Test]
        public void NoChargesNoSpecial()
        {
            var kit = new KitMeters(0, 3, 6f, 12, 0f);
            Assert.IsFalse(kit.UseSpecial());
            Assert.AreEqual(0, kit.Specials);
        }

        [Test]
        public void CratesCanGoPastTheRefillCap()
        {
            var kit = new KitMeters(3, 3, 6f, 12, 0f) { Specials = 13 };
            kit.Tick(60f);
            Assert.AreEqual(13, kit.Specials);
        }

        [Test]
        public void WithoutARefillChargesDontComeBack()
        {
            var kit = new KitMeters(3, 0, 0f, 12, 0f);
            kit.UseSpecial();
            kit.Tick(60f);
            Assert.AreEqual(2, kit.Specials);
        }

        [Test]
        public void KillsChargeTheSuperAndFiringEmptiesIt()
        {
            var kit = new KitMeters(3, 3, 6f, 4, 0f);
            Assert.IsFalse(kit.UseSuper());
            for (int i = 0; i < 3; i++) kit.AddKill();
            Assert.AreEqual(0.75f, kit.SuperCharge, 1e-4f);
            Assert.IsFalse(kit.SuperReady);
            kit.AddKill();
            kit.AddKill();
            Assert.IsTrue(kit.SuperReady);
            Assert.AreEqual(1f, kit.SuperCharge);
            Assert.IsTrue(kit.UseSuper());
            Assert.AreEqual(0f, kit.SuperCharge, "kills past full don't carry over");
            Assert.IsFalse(kit.UseSuper());
        }

        [Test]
        public void ASpikeCanStartWithTheSuperReady()
        {
            var kit = new KitMeters(3, 3, 6f, 12, 0f);
            kit.FillSuper();
            Assert.IsTrue(kit.SuperReady);
            Assert.IsTrue(kit.UseSuper());
            Assert.IsFalse(kit.SuperReady, "after that it charges from kills as usual");
        }

        [Test]
        public void TheAbilityWaitsOutItsCooldown()
        {
            var kit = new KitMeters(3, 3, 6f, 12, 5f);
            Assert.IsTrue(kit.HasAbility && kit.AbilityReady);
            Assert.IsTrue(kit.UseAbility());
            Assert.IsFalse(kit.UseAbility());
            kit.Tick(2.5f);
            Assert.AreEqual(0.5f, kit.AbilityProgress, 1e-4f);
            kit.Tick(2.5f);
            Assert.IsTrue(kit.UseAbility());
        }

        [Test]
        public void NoAbilityMeansNoAbility()
        {
            var kit = new KitMeters(3, 3, 6f, 12, 0f);
            Assert.IsFalse(kit.HasAbility);
            Assert.IsFalse(kit.UseAbility());
        }
    }

    public class CallInGestureTests
    {
        const float Dt = 0.05f;
        CallInGesture m_Gesture;

        [SetUp]
        public void SetUp() => m_Gesture = new CallInGesture(0.2f, 0.8f);

        /// <summary>Holds SP (pressed on the first frame) for this many seconds, and returns the last result.</summary>
        CallInGesture.Result Hold(float seconds, bool down = true, bool canCall = true)
        {
            CallInGesture.Result r = m_Gesture.Tick(Dt, down, true, true, canCall);
            for (float t = Dt; t < seconds - 1e-4f; t += Dt) r = m_Gesture.Tick(Dt, down, true, false, canCall);
            return r;
        }

        CallInGesture.Result Release(bool down = true, bool canCall = true) => m_Gesture.Tick(Dt, down, false, false, canCall);

        [Test]
        public void WithNoTankSpFiresAtOnce()
        {
            Assert.AreEqual(CallInGesture.Result.FireSpecial, m_Gesture.Tick(Dt, true, true, true, false));
        }

        [Test]
        public void WithoutTheStickDownSpFiresAtOnce()
        {
            Assert.AreEqual(CallInGesture.Result.FireSpecial, m_Gesture.Tick(Dt, false, true, true, true));
        }

        [Test]
        public void ATapWithTheStickDownStillFiresTheSpecial()
        {
            Assert.AreEqual(CallInGesture.Result.None, Hold(0.1f));
            Assert.AreEqual(CallInGesture.Result.FireSpecial, Release());
            Assert.IsFalse(m_Gesture.Charging);
        }

        [Test]
        public void HoldingChargesThenCallsTheTank()
        {
            Assert.AreEqual(CallInGesture.Result.Charging, Hold(0.6f));
            Assert.IsTrue(m_Gesture.Charging);
            Assert.AreEqual(0.5f, m_Gesture.Charge, 0.07f);
            CallInGesture.Result r = CallInGesture.Result.None;
            for (int i = 0; i < 20 && r != CallInGesture.Result.Called; i++) r = m_Gesture.Tick(Dt, true, true, false, true);
            Assert.AreEqual(CallInGesture.Result.Called, r);
            Assert.AreEqual(0f, m_Gesture.Charge);
        }

        [Test]
        public void LettingGoOfSpCancels()
        {
            Hold(0.5f);
            Assert.AreEqual(CallInGesture.Result.Cancelled, Release());
            Assert.IsFalse(m_Gesture.Charging);
        }

        [Test]
        public void LettingGoOfDownCancels()
        {
            Hold(0.5f);
            Assert.AreEqual(CallInGesture.Result.Cancelled, m_Gesture.Tick(Dt, false, true, false, true));
        }

        [Test]
        public void ACancelledHoldDoesntFireTheSpecialOnRelease()
        {
            Hold(0.5f);
            Release();
            Assert.AreEqual(CallInGesture.Result.None, Release());
        }

        [Test]
        public void ResetDropsACharge()
        {
            Hold(0.5f);
            m_Gesture.Reset();
            Assert.IsFalse(m_Gesture.Charging);
            Assert.AreEqual(CallInGesture.Result.None, Release());
        }
    }

    public class AimingTests
    {
        static readonly float Diag = Mathf.Sqrt(0.5f);

        [Test]
        public void StandingStillAimsStraight()
        {
            Assert.AreEqual(new Vector2(1f, 0f), Aiming.Direction(false, false, 0f, 1, true));
            Assert.AreEqual(new Vector2(-1f, 0f), Aiming.Direction(false, false, 0f, -1, true));
            Assert.AreEqual(Vector2.up, Aiming.Direction(true, false, 0f, 1, true));
            Assert.AreEqual(Vector2.down, Aiming.Direction(false, true, 0f, 1, false));
        }

        [Test]
        public void DownOnTheGroundCrouchesAimingForward()
        {
            Assert.AreEqual(new Vector2(1f, 0f), Aiming.Direction(false, true, 1f, 1, true));
        }

        [Test]
        public void MovingTurnsUpAndAirDownIntoDiagonals()
        {
            Vector2 upFwd = Aiming.Direction(true, false, -1f, -1, true);
            Assert.AreEqual(-Diag, upFwd.x, 1e-5f);
            Assert.AreEqual(Diag, upFwd.y, 1e-5f);
            Vector2 downFwd = Aiming.Direction(false, true, 1f, 1, false);
            Assert.AreEqual(Diag, downFwd.x, 1e-5f);
            Assert.AreEqual(-Diag, downFwd.y, 1e-5f);
            Assert.AreEqual(1f, upFwd.magnitude, 1e-5f, "shots keep their speed");
        }

        [Test]
        public void ForDrawingAimsAreSortedOut()
        {
            Vector2 diag = new Vector2(Diag, Diag);
            Assert.AreEqual(1, Aiming.AimY(diag));
            Assert.IsTrue(Aiming.IsDiagonal(diag));
            Assert.AreEqual(-1, Aiming.AimY(Vector2.down));
            Assert.IsFalse(Aiming.IsDiagonal(Vector2.down));
            Assert.AreEqual(0, Aiming.AimY(Vector2.left));
            Assert.IsFalse(Aiming.IsDiagonal(Vector2.left));
        }
    }
}
