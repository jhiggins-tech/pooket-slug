using NUnit.Framework;
using UnityEngine;

namespace PooketSlug.Tests
{
    /// <summary>Spike coop-camera: the partner arrow, the low-health and away warnings, and the camera's hold.</summary>
    public class PartnerBeaconTests
    {
        static readonly Rect View = new Rect(0f, 0f, 20f, 11.25f);

        [Test]
        public void ASoldierPartlyInViewIsOnScreen()
        {
            Assert.IsTrue(PartnerBeacon.OnScreen(View, new Vector2(5f, 1f), 1.6f));
            Assert.IsTrue(PartnerBeacon.OnScreen(View, new Vector2(5f, -1f), 1.6f), "feet below the bottom, head showing");
            Assert.IsFalse(PartnerBeacon.OnScreen(View, new Vector2(21f, 1f), 1.6f), "off to the right");
            Assert.IsFalse(PartnerBeacon.OnScreen(View, new Vector2(-1f, 1f), 1.6f), "left behind");
            Assert.IsFalse(PartnerBeacon.OnScreen(View, new Vector2(5f, 12f), 1.6f), "up off the top");
        }

        [Test]
        public void TheArrowSitsOnTheEdgeFacingThem()
        {
            Vector2 right = PartnerBeacon.EdgePoint(View, new Vector2(40f, View.center.y), out float a);
            Assert.AreEqual(View.xMax - PartnerBeacon.Inset, right.x, 1e-4f);
            Assert.AreEqual(View.center.y, right.y, 1e-4f);
            Assert.AreEqual(0f, a, 1e-3f);

            Vector2 left = PartnerBeacon.EdgePoint(View, new Vector2(-30f, View.center.y), out a);
            Assert.AreEqual(View.xMin + PartnerBeacon.Inset, left.x, 1e-4f);
            Assert.AreEqual(180f, Mathf.Abs(a), 1e-3f);

            Vector2 up = PartnerBeacon.EdgePoint(View, new Vector2(View.center.x, 50f), out a);
            Assert.AreEqual(View.yMax - PartnerBeacon.Inset, up.y, 1e-4f);
            Assert.AreEqual(90f, a, 1e-3f);

            // up and to the right: on the top edge, inside the view, pointing between the two
            Vector2 corner = PartnerBeacon.EdgePoint(View, new Vector2(18f, 40f), out a);
            Assert.AreEqual(View.yMax - PartnerBeacon.Inset, corner.y, 1e-4f);
            Assert.That(corner.x, Is.InRange(View.center.x, View.xMax - PartnerBeacon.Inset));
            Assert.That(a, Is.InRange(0f, 90f));
        }

        [Test]
        public void TheArrowSaysHowFarOffTheyAre()
        {
            Assert.AreEqual(10, PartnerBeacon.Metres(View, new Vector2(30f, 3f)));
            Assert.AreEqual(5, PartnerBeacon.Metres(View, new Vector2(-5f, 3f)));
            Assert.AreEqual(1, PartnerBeacon.Metres(View, new Vector2(20.1f, 3f)), "never zero");
        }

        [TestCase(5, 5, false)]
        [TestCase(3, 5, false)]
        [TestCase(2, 5, true)]
        [TestCase(1, 5, true)]
        [TestCase(0, 5, false)] // down: the HUD says so already
        [TestCase(1, 2, true)]  // always at least one heart
        public void LowHealth(int hp, int max, bool low) => Assert.AreEqual(low, PartnerBeacon.IsLow(hp, max));

        [Test]
        public void TheBeatTicksFourTimesASecond()
        {
            Assert.AreEqual(0, PartnerBeacon.BeatAt(0f));
            Assert.AreEqual(1, PartnerBeacon.BeatAt(0.26f));
            Assert.AreEqual(3, PartnerBeacon.BeatAt(0.76f));
            Assert.AreEqual(0, PartnerBeacon.BeatAt(1.01f));
        }

        [Test]
        public void PresenceRidesInThePose()
        {
            for (int beat = 0; beat < 4; beat++)
                foreach (bool away in new[] { false, true })
                {
                    var pose = new AvatarPose { Facing = 1, Away = away, Beat = beat, SuperReady = true, Jetting = true, Pressure = 1f };
                    AvatarPose back = AvatarPose.Unpack(pose.Pack(), 0f, 0f);
                    Assert.AreEqual(away, back.Away);
                    Assert.AreEqual(beat, back.Beat);
                    Assert.IsTrue(back.SuperReady && back.Jetting, "the other bits are untouched");
                }
        }

        [Test]
        public void APartnerWhoseGameStopsIsAway()
        {
            var presence = new Presence();
            float t = 0f;
            for (int i = 0; i < 120; i++, t += 1f / 60f) presence.Tick(1f / 60f, new AvatarPose { Beat = PartnerBeacon.BeatAt(t) });
            Assert.IsFalse(presence.Away, "beating: here");
            // their phone goes to the background: the last pose they sent stays
            var frozen = new AvatarPose { Beat = PartnerBeacon.BeatAt(t) };
            for (int i = 0; i < 60; i++) presence.Tick(1f / 60f, frozen);
            Assert.IsFalse(presence.Away, "a second is only a hiccup");
            for (int i = 0; i < 60; i++) presence.Tick(1f / 60f, frozen);
            Assert.IsTrue(presence.Away, "two seconds still: away");
            presence.Tick(1f / 60f, new AvatarPose { Beat = (frozen.Beat + 1) & 3 });
            Assert.IsFalse(presence.Away, "back as soon as it beats again");
        }

        [Test]
        public void APartnerWhoSaysTheyreAwayIsAwayAtOnce()
        {
            var presence = new Presence();
            presence.Tick(0.016f, new AvatarPose { Away = true });
            Assert.IsTrue(presence.Away);
        }

        [Test]
        public void TheCameraFollowsTheLeaderWithoutLosingTheTrailer()
        {
            var hold = new ScrollHold();
            Assert.IsFalse(hold.Any, "nobody playing: nobody holds it");
            hold.Add(10f);
            Assert.AreEqual(10f + Tuning.Camera.LookAhead, hold.Desired(Tuning.Coop.HalfWidth), 1e-4f);
            hold.Add(2f); // a partner way behind
            Assert.AreEqual(2f - Tuning.Coop.TrailMargin + Tuning.Coop.HalfWidth, hold.Desired(Tuning.Coop.HalfWidth), 1e-4f);
        }

        [Test]
        public void TheGhostGoesEverywhereTheBeaconCaresAbout()
        {
            Vector2 you = new Vector2(30f, 2f);
            float half = 10f;
            bool beside = false, ahead = false, behind = false, above = false, low = false, away = false;
            for (float t = 0f; t < PartnerGhost.Loop; t += 0.25f)
            {
                PartnerGhost.State g = PartnerGhost.At(t, you, half);
                var view = new Rect(you.x - half, 0f, 2f * half, 11.25f);
                bool on = PartnerBeacon.OnScreen(view, g.Pos, 1.6f);
                beside |= on;
                ahead |= !on && g.Pos.x > view.xMax;
                behind |= !on && g.Pos.x < view.xMin;
                above |= !on && g.Pos.y > view.yMax;
                low |= PartnerBeacon.IsLow(g.Hp, g.MaxHp);
                away |= g.Away;
            }
            Assert.IsTrue(beside && ahead && behind && above && low && away);
        }
    }
}
