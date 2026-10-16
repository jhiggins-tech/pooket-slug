using NUnit.Framework;
using UnityEngine;

namespace PooketSlug.Tests
{
    public class CraterGroundTests
    {
        static readonly float HalfWidth = Content.Trooper.HalfWidth, Height = Content.Trooper.StandHeight;
        const float Dt = 1f / 60f;

        // Two rows of ground (top at y = 2); columns 4 on are diggable.
        static readonly string[] Map =
        {
            "................",
            "................",
            "................",
            "................",
            "################",
            "################",
        };

        static Level Arena()
        {
            var level = new Level(Map);
            level.Craters = new CraterGround(level, 4, level.Width);
            return level;
        }

        static MoveResult Step(Level level, ref Vector2 feet, ref Vector2 vel, int frames = 1)
        {
            MoveResult r = default;
            for (int i = 0; i < frames; i++)
            {
                vel.y -= 30f * Dt;
                r = TilePhysics.Move(level, ref feet, HalfWidth, Height, ref vel, Dt, false);
            }
            return r;
        }

        [Test]
        public void ItStartsAsTheTiles()
        {
            Level level = Arena();
            Assert.IsTrue(level.IsSolidAt(new Vector2(8.5f, 1.9f)));
            Assert.IsFalse(level.IsSolidAt(new Vector2(8.5f, 2.1f)));
            Assert.AreEqual(2f, level.Craters.SurfaceBelow(8.5f, 5f), 1e-5f);
        }

        [Test]
        public void ABlastDigsARoundHoleButNotThroughTheBottom()
        {
            Level level = Arena();
            Assert.IsTrue(level.Craters.Dig(new Vector2(8f, 2f), 3f));
            Assert.IsFalse(level.IsSolidAt(new Vector2(8f, 1f)));
            Assert.IsFalse(level.IsSolidAt(new Vector2(6f, 1.9f)));
            Assert.IsTrue(level.IsSolidAt(new Vector2(8f, 0.1f)), "bedrock stays");
            Assert.IsTrue(level.IsSolidAt(new Vector2(12f, 1.9f)), "outside the blast stays");
            Assert.IsTrue(level.Craters.TakeDirty(out RectInt dirty));
            Assert.Greater(dirty.width, 0);
            Assert.IsFalse(level.Craters.TakeDirty(out _), "taken once");
            Assert.IsFalse(level.Craters.Dig(new Vector2(8f, 6f), 1f), "a blast in the air digs nothing");
        }

        [Test]
        public void WalkingOnUndugGroundIsLikeTiles()
        {
            Level level = Arena();
            var feet = new Vector2(5f, 2f);
            var vel = new Vector2(4f, 0f);
            MoveResult r = Step(level, ref feet, ref vel, 30);
            Assert.IsTrue(r.Grounded);
            Assert.AreEqual(2f, feet.y, 1e-4f);
            Assert.Greater(feet.x, 6.5f);
        }

        [Test]
        public void YouFallIntoACraterAndHaveToJumpOut()
        {
            Level level = Arena();
            level.Craters.Dig(new Vector2(9f, 2f), 1.6f);
            var feet = new Vector2(9f, 2f);
            var vel = Vector2.zero;
            MoveResult r = Step(level, ref feet, ref vel, 60);
            Assert.IsTrue(r.Grounded);
            Assert.Less(feet.y, 1f, "fell to the bottom of the crater");

            vel.x = 4f;
            bool walled = false;
            for (int i = 0; i < 60; i++) walled |= Step(level, ref feet, ref vel).HitWall;
            Assert.IsTrue(walled, "the crater wall stops a walk");
            Assert.Less(feet.x, 10.6f);
        }

        [Test]
        public void SmallDentsAreWalkedOver()
        {
            Level level = Arena();
            level.Craters.Dig(new Vector2(8f, 2.15f), 0.4f);
            var feet = new Vector2(6f, 2f);
            var vel = new Vector2(4f, 0f);
            for (int i = 0; i < 50; i++) Assert.IsFalse(Step(level, ref feet, ref vel).HitWall);
            Assert.Greater(feet.x, 9f);
            Assert.AreEqual(2f, feet.y, 1e-4f);
        }
    }
}
