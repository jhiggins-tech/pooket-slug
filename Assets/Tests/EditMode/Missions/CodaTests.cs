using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>Mission one's coda, the tabk run out of the base after the boss (docs/design/story.md, The coda).</summary>
    public class CodaTests
    {
        static float First(Level level, SpawnKind kind)
        {
            foreach (Spawn s in level.Spawns)
                if (s.Kind == kind) return s.Pos.x;
            return -1f;
        }

        [Test]
        public void MissionOnesCodaIsPastTheBossInOrder()
        {
            Level level = Missions.One.Load();
            float boss = First(level, SpawnKind.Tank), gate = First(level, SpawnKind.BackGate);
            Assert.Greater(gate, boss, "the back gate's past the boss");
            Assert.AreEqual(gate, level.CodaX + 0.5f);
            float checkpoint = -1f;
            foreach (Spawn s in level.Spawns)
                if (s.Kind == SpawnKind.Checkpoint && s.Pos.x > gate) checkpoint = s.Pos.x;
            Assert.Greater(checkpoint, gate, "a checkpoint at the coda's start");
            float tower = First(level, SpawnKind.ToppleTower), exit = First(level, SpawnKind.ExitGate), flag = First(level, SpawnKind.Flag);
            Assert.Greater(tower, checkpoint);
            Assert.Greater(exit, tower);
            Assert.Greater(flag, exit, "the flag's on the road out");
        }

        [Test]
        public void TheBackGateIsAWallUntilTheCodaOpens()
        {
            Level level = Missions.One.Load();
            int gate = (int)level.CodaX;
            Assert.AreEqual(level.CodaX, level.ScrollLimit, "nothing past the gate scrolls in");
            Assert.IsTrue(level.IsSolid(gate, 5), "a wall, all the way up");
            Assert.IsFalse(level.TileSolid(gate, 5), "the map's own tile is open air");
            level.OpenCoda();
            Assert.IsFalse(level.IsSolid(gate, 5));
            Assert.AreEqual(level.Width, level.ScrollLimit);
        }

        [Test]
        public void SomethingInTheWayBarsItsColumn()
        {
            Level level = Missions.One.Load();
            level.Bar(210, true);
            Assert.IsTrue(level.IsSolid(210, 3));
            Assert.IsTrue(level.IsSolidAt(new UnityEngine.Vector2(210.5f, 9f)));
            level.Bar(210, false);
            Assert.IsFalse(level.IsSolid(210, 3));
        }

        [Test]
        public void AMapWithoutACodaScrollsToItsEnd()
        {
            var level = new Level(new[] { "P....", "#####" });
            Assert.AreEqual(-1f, level.CodaX);
            Assert.AreEqual(level.Width, level.ScrollLimit);
        }

        [Test]
        public void TheTowerAndTheGatesAreRealNotHolograms()
        {
            Assert.AreSame(Content.ToppleTower, Content.EnemyFor(SpawnKind.ToppleTower));
            Assert.AreSame(Content.ExitGate, Content.EnemyFor(SpawnKind.ExitGate));
            Assert.IsNull(Content.EnemyFor(SpawnKind.BackGate), "the back gate's scenery");
            Assert.IsFalse(Content.PlayerTank.Crushes(Content.ToppleTower), "you shoot it down, not drive over it");
            Assert.IsFalse(Content.PlayerTank.Crushes(Content.ExitGate));
        }

        [Test]
        public void ThePromptsTeachOneThingAtATime()
        {
            var c = new CodaCoach(aimX: 20f, fireX: 30f, specialX: 50f);
            c.Tick(false, 5f, false, false, false, false);
            Assert.IsNull(c.Prompt, "only in the tabk");
            c.Tick(true, 5f, false, false, false, false);
            Assert.AreEqual(CodaCoach.DrivePrompt, c.Prompt);
            c.Tick(true, 6f, true, false, false, false);
            Assert.IsNull(c.Prompt, "driven: AIM waits for the gantry");
            c.Tick(true, 21f, false, false, false, false);
            Assert.AreEqual(CodaCoach.AimPrompt, c.Prompt);
            c.Tick(true, 22f, false, true, false, false);
            Assert.IsNull(c.Prompt);
            c.Tick(true, 31f, false, false, false, false);
            Assert.AreEqual(CodaCoach.FirePrompt, c.Prompt);
            c.Tick(true, 31f, false, false, true, false);
            c.Tick(true, 51f, false, false, false, false);
            Assert.AreEqual(CodaCoach.SpecialPrompt, c.Prompt);
            c.Tick(true, 51f, false, false, false, true);
            Assert.AreEqual(CodaCoach.Step.Done, c.At);
            Assert.IsNull(c.Prompt);
        }

        [Test]
        public void APromptDrivenWellPastIsDropped()
        {
            var c = new CodaCoach(aimX: 20f, fireX: 30f, specialX: 50f);
            c.Tick(true, 5f, true, false, false, false);
            c.Tick(true, 20f + CodaCoach.Overshoot + 1f, false, false, false, false);
            Assert.AreEqual(CodaCoach.FirePrompt, c.Prompt, "AIM's dropped, and FIRE's up, as the tower's in view");
        }

        [Test]
        public void AimingUpCountsOnceTheGantryIsInView()
        {
            var c = new CodaCoach(aimX: 20f, fireX: 30f, specialX: 50f);
            c.Tick(true, 5f, true, true, false, false); // aiming up early doesn't count: the gantry's not there yet
            c.Tick(true, 21f, false, true, false, false);
            Assert.AreEqual(CodaCoach.Step.Fire, c.At);
        }
    }
}
