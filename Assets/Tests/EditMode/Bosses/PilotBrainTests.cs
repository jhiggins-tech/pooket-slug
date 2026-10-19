using NUnit.Framework;
using UnityEngine;

namespace PooketSlug.Tests
{
    /// <summary>The pilot brain against scripted players (<see cref="PilotBrain"/>, kie's playbook).</summary>
    public class PilotBrainTests
    {
        const float Dt = 1f / 60f;

        static PilotView View(float targetX, float targetY = 0f) => new PilotView
        {
            Pos = new Vector2(20f, 0f), Grounded = true, Facing = -1, Health = 1f, Target = new Vector2(targetX, targetY), HasTarget = true,
            Specials = 0, Left = 5f, Right = 35f,
        };

        static PilotBrain Brain(uint seed = 7) => new PilotBrain(PilotPlaybook.Kie, new PilotSpec(), seed);

        /// <summary>Runs the brain for <paramref name="seconds"/> on a fixed view, counting what it pressed.</summary>
        static (int left, int right, int fire, int jump, int up, int special, int super) Run(PilotBrain b, PilotView v, float seconds)
        {
            int left = 0, right = 0, fire = 0, jump = 0, up = 0, special = 0, super = 0;
            for (float t = 0f; t < seconds; t += Dt)
            {
                SoldierInput i = b.Tick(Dt, v);
                if (i.Left) left++;
                if (i.Right) right++;
                if (i.FirePressed || i.Fire) fire++;
                if (i.JumpPressed) jump++;
                if (i.Up) up++;
                if (i.SpecialPressed) special++;
                if (i.SuperPressed) super++;
            }
            return (left, right, fire, jump, up, special, super);
        }

        [Test]
        public void WithNobodyToFightItWaits()
        {
            PilotView v = View(10f);
            v.HasTarget = false;
            var r = Run(Brain(), v, 5f);
            Assert.AreEqual(0, r.left + r.right + r.fire + r.jump + r.special + r.super);
        }

        [Test]
        public void ItBacksOffWhenYouAreTooCloseAndClosesInWhenTooFar()
        {
            var close = Run(Brain(), View(18f), 3f); // you're 2 to its left
            Assert.Greater(close.right, close.left * 2, "backs away, to the right");
            var far = Run(Brain(), View(4f + 2f), 3f); // you're 14 to its left
            Assert.Greater(far.left, far.right * 2, "closes in, to the left");
        }

        [Test]
        public void ItNeverBacksOffTheScreen()
        {
            PilotView v = View(30f);
            v.Pos = new Vector2(v.Left + 0.5f, 0f);
            v.Target = new Vector2(v.Left + 2f, 0f); // you're close, pinning it to the edge
            var r = Run(Brain(), v, 3f);
            Assert.AreEqual(0, r.left);
        }

        [Test]
        public void ItFiresInBurstsWithPauses()
        {
            PilotBrain b = Brain();
            PilotView v = View(13f); // in its range
            int presses = 0, longestQuiet = 0, quiet = 0;
            for (float t = 0f; t < 12f; t += Dt)
            {
                SoldierInput i = b.Tick(Dt, v);
                if (i.FirePressed)
                {
                    presses++;
                    quiet = 0;
                }
                else longestQuiet = Mathf.Max(longestQuiet, ++quiet);
            }
            Assert.Greater(presses, 5, "it fires");
            Assert.Less(presses, 12f / PilotPlaybook.Kie.TapGap, "but not all the time");
            Assert.Greater(longestQuiet * Dt, PilotPlaybook.Kie.TapGap * 1.2f, "with pauses longer than its tapping");
        }

        [Test]
        public void ItFacesYou()
        {
            PilotView v = View(30f); // you're to its right, but it faces left
            v.Pos = new Vector2(23f, 0f);
            var r = Run(Brain(), v, 2f);
            Assert.Greater(r.right, 0);
        }

        [Test]
        public void ItAimsUpAtATargetWellAbove()
        {
            var r = Run(Brain(), View(13f, 4f), 3f);
            Assert.Greater(r.up, 0);
            var level = Run(Brain(), View(13f, 0f), 3f);
            Assert.AreEqual(0, level.up);
        }

        [Test]
        public void ItJumpsShotsItSeesComingMostOfTheTime()
        {
            int dodged = 0;
            for (uint seed = 1; seed <= 40; seed++)
            {
                PilotView v = View(13f);
                v.Threat = true;
                v.ThreatSide = -1;
                if (Run(Brain(seed), v, 0.6f).jump > 0) dodged++;
            }
            Assert.Greater(dodged, 12, "often");
            Assert.Less(dodged, 40, "but not always: it's no aimbot");
        }

        [Test]
        public void AHurtPilotIsQuickerAndBolder()
        {
            var spec = new PilotSpec();
            Assert.Less(spec.ReactionAt(0.1f), spec.ReactionAt(1f));
            Assert.Greater(spec.DodgeAt(0.1f), spec.DodgeAt(1f));
            Assert.Less(spec.PauseAt(1f, 0.1f), spec.PauseAt(1f, 1f));
        }

        [Test]
        public void ItWarnsBeforeItDropsAHologramWhenYouCloseIn()
        {
            PilotBrain b = Brain();
            PilotView v = View(18f);
            v.Specials = 3;
            float toldAt = -1f, pressedAt = -1f;
            for (float t = 0f; t < 3f && pressedAt < 0f; t += Dt)
            {
                SoldierInput i = b.Tick(Dt, v);
                if (b.Telling && toldAt < 0f) toldAt = t;
                if (i.SpecialPressed) pressedAt = t;
            }
            Assert.GreaterOrEqual(toldAt, 0f, "a tell");
            Assert.Greater(pressedAt, 0f, "then the hologram");
            Assert.GreaterOrEqual(pressedAt - toldAt, new PilotSpec().Tell - Dt * 1.5f, "half a second after the tell");
        }

        [Test]
        public void WithoutChargesItDropsNoHologram()
        {
            PilotView v = View(18f);
            v.Specials = 0;
            Assert.AreEqual(0, Run(Brain(), v, 3f).special);
        }

        [Test]
        public void ItStealsWhenChargedAfterATell()
        {
            PilotBrain b = Brain();
            PilotView v = View(13f);
            v.SuperReady = true;
            float pressedAt = -1f;
            for (float t = 0f; t < 2f && pressedAt < 0f; t += Dt)
                if (b.Tick(Dt, v).SuperPressed) pressedAt = t;
            Assert.GreaterOrEqual(pressedAt, new PilotSpec().Tell - Dt, "Steal, after its tell");
        }

        [Test]
        public void KieSwapsWithHisHologramToGetAwayFromYou()
        {
            PilotView v = View(18.5f); // you're on top of the real one
            v.DecoyUp = true;
            v.Decoy = new Vector2(28f, 0f); // the hologram is well off
            Assert.Greater(Run(Brain(), v, 2f).special, 0);
        }

        [Test]
        public void KieDoesntSwapWhenItWouldLandNextToYou()
        {
            PilotView v = View(18.5f);
            v.DecoyUp = true;
            v.Decoy = new Vector2(17f, 0f); // the hologram is beside you
            Assert.AreEqual(0, Run(Brain(), v, 2f).special);
        }

        [Test]
        public void ItFollowsYouUpAndDown()
        {
            Assert.Greater(Run(Brain(), View(14f, 3f), 2f).jump, 0, "jumps up after you");
            PilotView below = View(14f, -3f);
            below.OnBridge = true;
            PilotBrain b = Brain();
            bool dropped = false;
            for (float t = 0f; t < 2f; t += Dt)
            {
                SoldierInput i = b.Tick(Dt, below);
                dropped |= i.Down && i.JumpPressed;
            }
            Assert.IsTrue(dropped, "drops through the bridge after you");
        }

        [Test]
        public void TheSameSeedPlaysTheSameWay()
        {
            PilotBrain a = Brain(3), b = Brain(3);
            PilotView v = View(12f);
            v.Threat = true;
            for (float t = 0f; t < 5f; t += Dt)
            {
                SoldierInput ia = a.Tick(Dt, v), ib = b.Tick(Dt, v);
                Assert.AreEqual(ia.Left, ib.Left);
                Assert.AreEqual(ia.FirePressed, ib.FirePressed);
                Assert.AreEqual(ia.JumpPressed, ib.JumpPressed);
            }
        }

        [Test]
        public void KiesPilotsSuperChargesFromHitsOnYou()
        {
            var spec = new KiePilotSpec();
            int meter = spec.ChargeSuper(0, 1);
            Assert.IsFalse(spec.SuperReady(meter));
            meter = spec.ChargeSuper(meter, spec.SuperHits);
            Assert.IsTrue(spec.SuperReady(meter));
            Assert.AreEqual(spec.SuperHits, meter, "it fills, and no further");
            Assert.AreEqual(0, spec.ChargeSuper(0, -2), "healing doesn't drain it");
        }

        [Test]
        public void KiesPilotIsABossDuelAndHisKitIsSofterThanAPlayers()
        {
            var spec = new KiePilotSpec();
            Assert.AreEqual(spec.Hp, Content.Pilot.Hp);
            Assert.Greater(spec.Hp, 3 * Content.Kie.MaxHp, "a boss bar, counted in shots");
            Assert.Less(spec.WeaselsPerThrow, KieTuning.Weasels.Count);
            Assert.Greater(spec.WeaselCooldown, KieTuning.Weasels.Cooldown);
            Assert.AreEqual(EnemyBrain.Pilot, Content.Pilot.Brain);
            Assert.AreEqual(EnemyBrain.PilotDecoy, Content.PilotDecoy.Brain);
        }

        [Test]
        public void KcajHoldsHisBeamOnYouFromRange_WhenYoureLevelWithHim()
        {
            var b = new PilotBrain(PilotPlaybook.Kcaj, new PilotSpec(), 7);
            PilotView v = View(12f); // 8 to its left, level
            v.Specials = 2;
            Assert.Greater(Run(b, v, 3f).special, 0, "from range, level: Hyperfixate");

            b = new PilotBrain(PilotPlaybook.Kcaj, new PilotSpec(), 7);
            v = View(12f, 3f); // up on a ledge: the beam would go under you
            v.Specials = 2;
            Assert.AreEqual(0, Run(b, v, 3f).special);

            b = new PilotBrain(PilotPlaybook.Kcaj, new PilotSpec(), 7);
            v = View(12f);
            v.Specials = 2;
            v.SpecialBusy = true; // his beam's already stuck
            Assert.AreEqual(0, Run(b, v, 3f).special);

            b = new PilotBrain(PilotPlaybook.Kcaj, new PilotSpec(), 7);
            v = View(18f); // right up close: no beam, he backs off
            v.Specials = 2;
            Assert.AreEqual(0, Run(b, v, 3f).special);
        }
    }
}