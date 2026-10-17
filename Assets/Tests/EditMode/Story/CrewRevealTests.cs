using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>Two of the crew (docs/design/story.md): the menu grows on the title screen.</summary>
    public class CrewRevealTests
    {
        static void Run(CrewReveal r, float seconds, ref int clunks)
        {
            for (float t = 0f; t < seconds; t += 0.05f)
                if (r.Tick(0.05f)) clunks++;
        }

        [Test]
        public void ItRunsAboutEightSecondsInFourShots()
        {
            float total = 0f;
            foreach (float s in CrewReveal.Seconds) total += s;
            Assert.AreEqual(4, CrewReveal.Seconds.Length);
            Assert.That(total, Is.InRange(7f, 9f));
            var r = new CrewReveal();
            int clunks = 0;
            Run(r, total + 0.2f, ref clunks);
            Assert.IsTrue(r.Done);
        }

        [Test]
        public void TheThreeButtonsDropOneAtATimeEachWithAClunk()
        {
            var r = new CrewReveal();
            int clunks = 0;
            r.Tick(1f);
            r.Tick(2f);
            Assert.AreEqual(CrewReveal.Shot.MenuGrows, r.Now);
            Assert.IsTrue(r.OldStart, "START cracks before it splits");
            Run(r, CrewReveal.DropAt(1) - 0.05f, ref clunks);
            Assert.IsFalse(r.OldStart);
            Assert.AreEqual(1, r.Dropped);
            Assert.AreEqual(0f, r.Drop(2));
            Run(r, 3f, ref clunks);
            Assert.AreEqual(3, clunks);
            Assert.AreEqual(3, r.Dropped);
            Assert.AreEqual(new[] { "MISSIONS", "CO-OP", "CREW" }, CrewReveal.NewButtons);
        }

        [Test]
        public void TheTitleShowsStartUntilTheCrewsOpenAndStartHasCracked()
        {
            Assert.IsFalse(CrewReveal.MenuGrown(false, null), "before Mission one's beaten: START");
            Assert.IsTrue(CrewReveal.MenuGrown(true, null), "after: MISSIONS");
            var r = new CrewReveal();
            Assert.IsFalse(CrewReveal.MenuGrown(true, r), "while the reveal plays, START until it cracks");
            r.Tick(1f);
            r.Tick(2f);
            r.Tick(CrewReveal.CrackSeconds + 0.01f);
            Assert.IsTrue(CrewReveal.MenuGrown(true, r));
        }

        [Test]
        public void ATapMovesOnButCantSkipAShotUnseen()
        {
            var r = new CrewReveal();
            r.Tap();
            Assert.AreEqual(CrewReveal.Shot.OldTitle, r.Now, "too soon");
            r.Tick(CrewReveal.TapGuard);
            r.Tap();
            Assert.AreEqual(CrewReveal.Shot.KieWalksIn, r.Now);
            r.Tap();
            Assert.AreEqual(CrewReveal.Shot.KieWalksIn, r.Now, "each shot gets its moment");
            for (int i = 0; i < 3; i++)
            {
                r.Tick(CrewReveal.TapGuard);
                r.Tap();
            }
            Assert.IsTrue(r.Done);
            Assert.AreEqual(3, r.Dropped, "a tapped-through menu is fully grown");
        }

        [Test]
        public void KieSaysTheTwoLines()
        {
            var r = new CrewReveal();
            r.Tick(1.1f);
            Assert.IsNull(r.Line);
            r.Tick(1.5f);
            Assert.AreEqual("MISSED ME?", r.Line);
            r.Tick(1f);
            r.Tick(3f);
            Assert.AreEqual(CrewReveal.Shot.KiesLine, r.Now);
            Assert.AreEqual("TWO OF US NOW. BRING A MATE.", r.Line);
        }
    }
}
