using NUnit.Framework;
using UnityEngine;

namespace PooketSlug.Tests
{
    /// <summary>Mission two's hazards (Core/Missions/Creamery.cs) and its map (Missions.Two).</summary>
    public class CreameryTests
    {
        [Test]
        public void BeltsAreGroundThatCarriesWhatStandsOnIt()
        {
            var level = new Level(new[]
            {
                "P.......",
                "##>><<##",
            });
            Assert.IsTrue(level.IsSolid(2, 0), "a belt is ground");
            Assert.AreEqual(Level.Ground, level.Get(3, 0));
            Assert.AreEqual(1, level.Belt(2, 0));
            Assert.AreEqual(-1, level.Belt(5, 0));
            Assert.AreEqual(0, level.Belt(1, 0));
            Assert.AreEqual(Creamery.BeltSpeed, level.BeltSpeedUnder(new Vector2(3.5f, 1f), grounded: true), 1e-5f);
            Assert.AreEqual(-Creamery.BeltSpeed, level.BeltSpeedUnder(new Vector2(4.2f, 1f), grounded: true), 1e-5f);
            Assert.AreEqual(0f, level.BeltSpeedUnder(new Vector2(3.5f, 1f), grounded: false), "in the air, nothing carries you");
            Assert.AreEqual(0f, level.BeltSpeedUnder(new Vector2(0.5f, 1f), grounded: true));
            Assert.Less(Creamery.BeltSpeed, Tuning.Soldier.RunSpeed, "you can always walk against a belt");
        }

        [Test]
        public void TheCreamerysMarkersPlaceTheirPieces()
        {
            var level = new Level(new[]
            {
                ".Q.....",
                "P.NcnC.",
                "#######",
            });
            Assert.AreEqual(SpawnKind.ScoopDropper, level.Spawns[0].Kind);
            var kinds = new System.Collections.Generic.List<SpawnKind>();
            foreach (Spawn s in level.Spawns) kinds.Add(s.Kind);
            CollectionAssert.IsSupersetOf(kinds, new[] { SpawnKind.NitrogenVent, SpawnKind.ConeBox, SpawnKind.NitrogenTank, SpawnKind.ColdStore });
        }

        [Test]
        public void AVentBlastsForPartOfItsCycle_WithATellBefore()
        {
            int on = 0, soon = 0, both = 0;
            const int steps = 3400;
            for (int i = 0; i < steps; i++)
            {
                float t = i * Creamery.VentCycle / steps;
                bool v = Creamery.VentOn(t, 10f), s = Creamery.VentSoon(t, 10f);
                if (v) on++;
                if (s) soon++;
                if (v && s) both++;
            }
            Assert.AreEqual(Creamery.VentFor / Creamery.VentCycle, on / (float)steps, 0.01f);
            Assert.AreEqual(Creamery.VentTell / Creamery.VentCycle, soon / (float)steps, 0.01f);
            Assert.AreEqual(0, both, "the hiss comes before the blast, not with it");
        }

        [Test]
        public void VentsSideBySideKeepTheirOwnBeat()
        {
            bool differ = false;
            for (float t = 0f; t < Creamery.VentCycle; t += 0.05f)
                if (Creamery.VentOn(t, 132f) != Creamery.VentOn(t, 138f)) differ = true;
            Assert.IsTrue(differ);
        }

        [Test]
        public void ADropperLetsOneScoopGoEachCycle_AfterItsTell()
        {
            const float dt = 1f / 60f;
            int drops = 0;
            bool toldFirst = false, telling = false;
            for (float t = dt; t <= Creamery.ScoopCycle * 3f + 1e-4f; t += dt)
            {
                if (Creamery.ScoopDrops(t, dt, 36f))
                {
                    drops++;
                    toldFirst |= telling;
                }
                telling = Creamery.ScoopTelling(t, 36f);
            }
            Assert.AreEqual(3, drops);
            Assert.IsTrue(toldFirst, "it shakes before it drops");
            Assert.IsFalse(Creamery.ScoopDrops(5f, 0f, 36f));
        }

        [Test]
        public void ABoxOfConesAlwaysDropsSomething_MostlyAMedkit_NeverAGun()
        {
            var rng = new Rng(12345);
            int medkits = 0, bombs = 0;
            for (int i = 0; i < 3000; i++)
            {
                PickupDef d = Creamery.ConeBoxDrop(rng);
                Assert.IsNotNull(d);
                Assert.AreNotEqual(PickupEffect.Gun, d.Effect);
                if (d == Content.Medkit) medkits++;
                if (d == Content.BombCrate) bombs++;
            }
            Assert.AreEqual(3000, medkits + bombs);
            Assert.AreEqual(2f, medkits / (float)bombs, 0.25f);
        }

        [Test]
        public void MissionTwoRunsThroughItsSixSections()
        {
            // the m2-map page: belts and droppers on the production line, nitrogen in the sign yard and the hall, then
            // the cold store's yard, one flat screen with the doors at its right
            Level level = Missions.Two.Load();
            Assert.AreEqual(210, level.Width);
            bool belts = false;
            for (int x = 30; x < 85; x++)
                for (int y = 0; y < level.Height; y++)
                    belts |= level.Belt(x, y) != 0;
            Assert.IsTrue(belts, "the production line has belts");
            float tank = -1f, door = -1f;
            int vents = 0, droppers = 0, nitrogen = 0, boxes = 0;
            foreach (Spawn s in level.Spawns)
            {
                if (s.Kind == SpawnKind.Tank) tank = s.Pos.x;
                if (s.Kind == SpawnKind.ColdStore) door = s.Pos.x;
                if (s.Kind == SpawnKind.NitrogenVent) { vents++; Assert.That(s.Pos.x, Is.InRange(125f, 165f), "vents are in the nitrogen hall"); }
                if (s.Kind == SpawnKind.ScoopDropper) { droppers++; Assert.That(s.Pos.x, Is.InRange(30f, 85f), "droppers are over the production line"); }
                if (s.Kind == SpawnKind.NitrogenTank) nitrogen++;
                if (s.Kind == SpawnKind.ConeBox) boxes++;
            }
            Assert.Greater(vents, 0);
            Assert.Greater(droppers, 0);
            Assert.Greater(nitrogen, 0);
            Assert.Greater(boxes, 0);
            Assert.Greater(door, tank, "the doors are behind the tabk: it rolls out of them");
            Assert.Less(door - tank, 16f);
            float arena = level.Width - Tuning.Coop.HalfWidth * 2f; // a 16:9 screen
            for (float x = arena; x < level.Width; x += 1f)
                Assert.AreEqual(2f, level.TopSurfaceY(x), "the yard's floor is flat at " + x);
        }
    }
}
