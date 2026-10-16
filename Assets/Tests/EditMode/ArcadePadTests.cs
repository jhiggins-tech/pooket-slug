using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace PooketSlug.Tests
{
    public class ArcadePadTests
    {
        const float Width = 640f;
        static readonly Vector2 Thumb = new Vector2(100f, 260f);

        ArcadePad m_Pad;
        readonly List<ArcadePad.Finger> m_Fingers = new List<ArcadePad.Finger>();
        bool m_Pause;

        [SetUp]
        public void SetUp()
        {
            m_Pad = new ArcadePad { Visible = true, PauseVisible = true };
            m_Pad.Layout(Width);
        }

        /// <summary>One frame with these fingers down.</summary>
        SoldierInput Frame(params ArcadePad.Finger[] fingers)
        {
            m_Fingers.Clear();
            m_Fingers.AddRange(fingers);
            var input = new SoldierInput();
            m_Pad.Read(m_Fingers, ref input, out m_Pause);
            return input;
        }

        static ArcadePad.Finger Down(int id, Vector2 pos) => new ArcadePad.Finger(id, pos, true);
        static ArcadePad.Finger Held(int id, Vector2 pos) => new ArcadePad.Finger(id, pos, false);
        Vector2 On(ArcadePad.Button b) => m_Pad.Center(b);

        [Test]
        public void NoFingersNoInput()
        {
            SoldierInput input = Frame();
            Assert.AreEqual(0f, input.MoveX);
            Assert.IsFalse(input.Fire || input.Jump || input.Up || input.Down || input.SpecialPressed);
        }

        [Test]
        public void TheStickStartsWhereTheThumbLands()
        {
            SoldierInput input = Frame(Down(1, Thumb));
            Assert.AreEqual(0f, input.MoveX, "landing doesn't push");
            Assert.AreEqual(Thumb, m_Pad.StickOrigin);
        }

        [Test]
        public void PushingRunsAimsAndCrouches()
        {
            Frame(Down(1, Thumb));
            Assert.AreEqual(1f, Frame(Held(1, Thumb + new Vector2(30f, 0f))).MoveX);
            Assert.AreEqual(-1f, Frame(Held(1, Thumb + new Vector2(-30f, 0f))).MoveX);
            Assert.IsTrue(Frame(Held(1, Thumb + new Vector2(0f, -30f))).Up, "y goes down the screen");
            Assert.IsTrue(Frame(Held(1, Thumb + new Vector2(0f, 30f))).Down);
        }

        [Test]
        public void AWobbleIsIgnored()
        {
            Frame(Down(1, Thumb));
            SoldierInput input = Frame(Held(1, Thumb + new Vector2(8f, 6f)));
            Assert.AreEqual(0f, input.MoveX);
            Assert.IsFalse(input.Up || input.Down);
        }

        [Test]
        public void TheStickFollowsAThumbThatGoesTooFar()
        {
            Frame(Down(1, Thumb));
            Frame(Held(1, Thumb + new Vector2(200f, 0f)));
            // a short way back is enough to turn round; without following, this would still push right
            SoldierInput input = Frame(Held(1, Thumb + new Vector2(140f, 0f)));
            Assert.AreEqual(-1f, input.MoveX);
        }

        [Test]
        public void LiftingTheThumbLetsGo()
        {
            Frame(Down(1, Thumb));
            Frame(Held(1, Thumb + new Vector2(30f, 0f)));
            SoldierInput input = Frame();
            Assert.AreEqual(0f, input.MoveX);
            Assert.IsFalse(m_Pad.StickActive);
        }

        [Test]
        public void FireIsPressedOnceThenHeld()
        {
            SoldierInput first = Frame(Down(2, On(ArcadePad.Button.Fire)));
            Assert.IsTrue(first.Fire && first.FirePressed);
            SoldierInput second = Frame(Held(2, On(ArcadePad.Button.Fire)));
            Assert.IsTrue(second.Fire);
            Assert.IsFalse(second.FirePressed);
        }

        [Test]
        public void ANearMissStillCounts()
        {
            Vector2 nearFire = On(ArcadePad.Button.Fire) + new Vector2(m_Pad.Radius(ArcadePad.Button.Fire) * 1.3f, 0f);
            Assert.IsTrue(Frame(Down(2, nearFire)).Fire);
        }

        [Test]
        public void AThumbCanSlideFromFireToJump()
        {
            Frame(Down(2, On(ArcadePad.Button.Fire)));
            SoldierInput input = Frame(Held(2, On(ArcadePad.Button.Jump)));
            Assert.IsTrue(input.Jump && input.JumpPressed);
            Assert.IsFalse(input.Fire);
        }

        [Test]
        public void BothThumbsWorkAtOnce()
        {
            Frame(Down(1, Thumb), Down(2, On(ArcadePad.Button.Fire)));
            SoldierInput input = Frame(Held(1, Thumb + new Vector2(30f, -30f)), Held(2, On(ArcadePad.Button.Fire)));
            Assert.AreEqual(1f, input.MoveX);
            Assert.IsTrue(input.Up);
            Assert.IsTrue(input.Fire);
        }

        [Test]
        public void ASecondThumbOnTheLeftDoesntTakeTheStick()
        {
            Frame(Down(1, Thumb));
            SoldierInput input = Frame(Held(1, Thumb + new Vector2(30f, 0f)), Down(3, Thumb + new Vector2(-60f, 0f)));
            Assert.AreEqual(1f, input.MoveX);
        }

        [Test]
        public void AMissedTouchDownStillCounts()
        {
            // the first frame a finger shows up in may not be the one it touched down in
            SoldierInput input = Frame(Held(2, On(ArcadePad.Button.Fire)));
            Assert.IsTrue(input.Fire && input.FirePressed);
        }

        [Test]
        public void AFingerThatLandsLeftCanSlideOntoAButton()
        {
            Frame(Down(1, Thumb));
            Frame(Held(1, Thumb), Down(2, new Vector2(Width * 0.45f, 300f)));
            SoldierInput input = Frame(Held(1, Thumb), Held(2, On(ArcadePad.Button.Jump)));
            Assert.IsTrue(input.Jump && input.JumpPressed);
        }

        [Test]
        public void SpecialFiresOncePerTouch()
        {
            Assert.IsTrue(Frame(Down(2, On(ArcadePad.Button.Special))).SpecialPressed);
            Assert.IsFalse(Frame(Held(2, On(ArcadePad.Button.Special))).SpecialPressed);
        }

        [Test]
        public void SpIsHeldAsWellAsPressed()
        {
            SoldierInput input = Frame(Down(2, On(ArcadePad.Button.Special)));
            Assert.IsTrue(input.Special && input.SpecialPressed);
            input = Frame(Held(2, On(ArcadePad.Button.Special)));
            Assert.IsTrue(input.Special);
            Assert.IsFalse(input.SpecialPressed);
        }

        [Test]
        public void SuFiresTheSuper()
        {
            Assert.IsTrue(Frame(Down(2, On(ArcadePad.Button.Super))).SuperPressed);
            Assert.IsFalse(Frame(Held(2, On(ArcadePad.Button.Super))).SuperPressed);
        }

        [Test]
        public void AbShowsAndAnswersOnlyForASoldierWithAnAbility()
        {
            Assert.IsFalse(m_Pad.Shown(ArcadePad.Button.Ability));
            Assert.IsFalse(Frame(Down(2, On(ArcadePad.Button.Ability))).Ability);
            Frame();
            m_Pad.AbilityVisible = true;
            Assert.IsTrue(m_Pad.Shown(ArcadePad.Button.Ability));
            Assert.IsTrue(Frame(Down(3, On(ArcadePad.Button.Ability))).AbilityPressed);
        }

        [Test]
        public void TheButtonsDontOverlap()
        {
            var buttons = (ArcadePad.Button[])System.Enum.GetValues(typeof(ArcadePad.Button));
            foreach (ArcadePad.Button a in buttons)
                foreach (ArcadePad.Button b in buttons)
                {
                    if (a >= b) continue;
                    float gap = Vector2.Distance(On(a), On(b)) - m_Pad.Radius(a) - m_Pad.Radius(b);
                    Assert.Greater(gap, 0f, a + " and " + b);
                    Vector2 c = On(a);
                    Assert.IsTrue(c.x > 0f && c.x < Width && c.y > 0f && c.y < Tuning.Hud.Height, a + " is on screen");
                }
        }

        [Test]
        public void ThePauseButtonPauses()
        {
            SoldierInput input = Frame(Down(4, On(ArcadePad.Button.Pause)));
            Assert.IsTrue(m_Pause);
            Assert.IsFalse(input.Fire);
        }

        [Test]
        public void WhileHiddenTouchesDoNothing()
        {
            m_Pad.Visible = false;
            m_Pad.PauseVisible = false;
            SoldierInput input = Frame(Down(1, Thumb), Down(2, On(ArcadePad.Button.Fire)), Down(4, On(ArcadePad.Button.Pause)));
            Assert.IsFalse(input.Fire);
            Assert.AreEqual(0f, input.MoveX);
            Assert.IsFalse(m_Pause);
        }

        [Test]
        public void TheTouchThatStartsTheGameDoesntFire()
        {
            m_Pad.Visible = false;
            Frame(Down(2, On(ArcadePad.Button.Fire)));
            m_Pad.Visible = true;
            Assert.IsFalse(Frame(Held(2, On(ArcadePad.Button.Fire))).Fire, "still the same finger");
            Frame();
            Assert.IsTrue(Frame(Down(5, On(ArcadePad.Button.Fire))).Fire, "a new touch");
        }

        [Test]
        public void TheButtonsFollowTheScreensWidth()
        {
            m_Pad.Layout(800f);
            Assert.Greater(On(ArcadePad.Button.Fire).x, 640f);
            Assert.Less(On(ArcadePad.Button.Fire).x, 800f);
        }

        [Test]
        public void LeftHanded_TheStickIsOnTheRightAndTheButtonsOnTheLeft()
        {
            m_Pad.Layout(Width, leftHanded: true);
            Assert.IsTrue(m_Pad.LeftHanded);
            Assert.Greater(m_Pad.StickHome.x, Width * 0.5f);
            foreach (ArcadePad.Button b in new[] { ArcadePad.Button.Fire, ArcadePad.Button.Jump, ArcadePad.Button.Special, ArcadePad.Button.Super })
                Assert.Less(On(b).x, Width * 0.5f, b + " is under the left thumb");
            Assert.Greater(On(ArcadePad.Button.Pause).x, Width * 0.5f, "pause stays top right");

            Vector2 thumb = new Vector2(Width - Thumb.x, Thumb.y);
            Frame(Down(1, thumb));
            Assert.IsTrue(m_Pad.StickActive, "a thumb on the right takes the stick");
            Assert.AreEqual(1f, Frame(Held(1, thumb + new Vector2(30f, 0f))).MoveX);
            SoldierInput input = Frame(Held(1, thumb), Down(2, On(ArcadePad.Button.Fire)));
            Assert.IsTrue(input.FirePressed, "the left thumb fires");
        }

        [Test]
        public void LeftHanded_TheButtonsStillDontOverlap()
        {
            m_Pad.Layout(Width, leftHanded: true);
            TheButtonsDontOverlap();
            m_Pad.Layout(Width, leftHanded: false);
            Assert.Less(m_Pad.StickHome.x, Width * 0.5f, "and back again");
        }
    }
}
