using NUnit.Framework;
using UnityEngine;

namespace PooketSlug.Tests
{
    public class SoldierInputTests
    {
        static SoldierInput FromStick(float x, float y)
        {
            var input = new SoldierInput();
            input.AddStick(new Vector2(x, y));
            return input;
        }

        [Test]
        public void ASmallLeanDoesNothing()
        {
            SoldierInput input = FromStick(0.3f, -0.4f);
            Assert.AreEqual(0f, input.MoveX);
            Assert.IsFalse(input.Up);
            Assert.IsFalse(input.Down);
        }

        [Test]
        public void LeaningSidewaysRuns()
        {
            Assert.AreEqual(1f, FromStick(0.5f, 0f).MoveX);
            Assert.AreEqual(-1f, FromStick(-0.5f, 0f).MoveX);
        }

        [Test]
        public void LeaningUpAimsUpAndDownCrouches()
        {
            Assert.IsTrue(FromStick(0f, 0.6f).Up);
            Assert.IsTrue(FromStick(0f, -0.6f).Down);
        }

        [Test]
        public void ADiagonalIsBothDirections()
        {
            SoldierInput input = FromStick(0.7f, 0.7f);
            Assert.AreEqual(1f, input.MoveX);
            Assert.IsTrue(input.Up);
        }

        [Test]
        public void SourcesAddUp()
        {
            var keys = new SoldierInput { Left = true, Fire = true };
            var pad = new SoldierInput { Right = true, JumpPressed = true };
            keys.Add(pad);
            Assert.AreEqual(0f, keys.MoveX, "left and right together cancel out");
            Assert.IsTrue(keys.Fire);
            Assert.IsTrue(keys.JumpPressed);
        }
    }
}
