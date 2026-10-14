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
        bool m_Pause, m_Tap;

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
            m_Pad.Read(m_Fingers, ref input, out m_Pause, out m_Tap);
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
        public void SpecialFiresOncePerTouch()
        {
            Assert.IsTrue(Frame(Down(2, On(ArcadePad.Button.Special))).SpecialPressed);
            Assert.IsFalse(Frame(Held(2, On(ArcadePad.Button.Special))).SpecialPressed);
        }

        [Test]
        public void ThePauseButtonPauses()
        {
            SoldierInput input = Frame(Down(4, On(ArcadePad.Button.Pause)));
            Assert.IsTrue(m_Pause);
            Assert.IsFalse(input.Fire);
        }

        [Test]
        public void WhileHiddenATouchIsATap()
        {
            m_Pad.Visible = false;
            m_Pad.PauseVisible = false;
            SoldierInput input = Frame(Down(2, On(ArcadePad.Button.Fire)));
            Assert.IsTrue(m_Tap);
            Assert.IsFalse(input.Fire);
        }

        [Test]
        public void TheTapThatStartsTheGameDoesntFire()
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
    }
}
