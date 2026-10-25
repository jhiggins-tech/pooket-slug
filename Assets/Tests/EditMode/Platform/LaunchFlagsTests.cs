using NUnit.Framework;

namespace PooketSlug.Tests
{
    public class LaunchFlagsTests
    {
        const string Page = "https://jhiggins-tech.github.io/pooket-slug/index.html";

        [Test]
        public void APlainVisitAsksForNothing()
        {
            LaunchFlags f = LaunchFlags.Read(Page);
            Assert.IsFalse(f.UnlockCoop || f.UnlockTank || f.UnlockCrew);
            Assert.IsFalse(f.Crate || f.Tank || f.OneUp || f.Debrief || f.Coda || f.KcajTank || f.Boss);
            Assert.IsFalse(f.Pilot || f.WeakPilot || f.Team || f.Scene || f.Reveal);
            Assert.IsFalse(f.Testing, "so a co-op room plays the mission's opening scene");
            Assert.IsNull(f.Demo);
            Assert.IsNull(f.Room);
            Assert.IsNull(f.LogLevels);
        }

        [Test]
        public void ASkitCanBeAskedFor_ByItsName()
        {
            Assert.IsNull(LaunchFlags.Read(Page).Skit);
            LaunchFlags f = LaunchFlags.Read(Page + "?skit=Two-Of-Us");
            Assert.AreEqual("two-of-us", f.Skit);
            Assert.IsInstanceOf<TwoOfUsSkit>(Skits.Named(f.Skit));
            Assert.IsInstanceOf<SpecialDeliverySkit>(Skits.Named("special-delivery"));
            Assert.IsInstanceOf<TrollogramSkit>(Skits.Named("trollogram-of-anything"));
            Assert.IsNull(Skits.Named("wanna-race"));
            Assert.IsFalse(f.Testing, "it's not a jump to a part: the den opens as it would");
        }

        [Test]
        public void SeveralTestsAtOnceIgnoringCase()
        {
            LaunchFlags f = LaunchFlags.Read(Page + "?test=Boss,REVEAL");
            Assert.IsTrue(f.Boss);
            Assert.IsTrue(f.Reveal);
            Assert.IsFalse(f.Pilot);
        }

        [TestCase("crate")] [TestCase("tank")] [TestCase("1up")] [TestCase("debrief")] [TestCase("coda")]
        [TestCase("team")] [TestCase("scene")] [TestCase("reveal")]
        public void EachTestHasItsFlag(string test)
        {
            LaunchFlags f = LaunchFlags.Read(Page + "?test=" + test);
            bool[] on = { f.Crate, f.Tank, f.OneUp, f.Debrief, f.Coda, f.Team, f.Scene, f.Reveal };
            string[] names = { "crate", "tank", "1up", "debrief", "coda", "team", "scene", "reveal" };
            for (int i = 0; i < names.Length; i++) Assert.AreEqual(names[i] == test, on[i], names[i]);
            Assert.IsFalse(f.Boss, "none of these starts at the boss");
            Assert.IsTrue(f.Testing, "any test skips a co-op mission's opening scene");
        }

        [Test]
        public void ThePilotAndKcajsTabkStartAtTheBoss()
        {
            Assert.IsTrue(LaunchFlags.Read(Page + "?test=pilot").Boss);
            Assert.IsTrue(LaunchFlags.Read(Page + "?test=kcajtank").Boss);
            Assert.IsFalse(LaunchFlags.Read(Page + "?test=kcajtank").Pilot);
        }

        [Test]
        public void KiesTabkIsATabkTest()
        {
            LaunchFlags f = LaunchFlags.Read(Page + "?test=kietabk");
            Assert.IsTrue(f.KieTabk);
            Assert.IsTrue(f.Tank, "it parks one, as ?test=tank does");
            Assert.IsTrue(f.Testing);
            Assert.IsFalse(LaunchFlags.Read(Page + "?test=tank").KieTabk);
        }

        [Test]
        public void KcajsTabkIsATabkTest()
        {
            LaunchFlags f = LaunchFlags.Read(Page + "?test=softserve");
            Assert.IsTrue(f.SoftServe);
            Assert.IsTrue(f.Tank, "it parks one, as ?test=tank does");
            Assert.IsFalse(f.KieTabk);
            Assert.IsFalse(f.KcajTank, "kcaj's boss tabk is ?test=kcajtank");
            Assert.IsFalse(LaunchFlags.Read(Page + "?test=tank").SoftServe);
        }

        [Test]
        public void AWeakPilotNeedsThePilot()
        {
            Assert.IsTrue(LaunchFlags.Read(Page + "?test=pilot,weak").WeakPilot);
            Assert.IsFalse(LaunchFlags.Read(Page + "?test=weak").WeakPilot);
        }

        [Test]
        public void UnlocksAreTheirOwn()
        {
            LaunchFlags f = LaunchFlags.Read(Page + "?unlock=tank,crew");
            Assert.IsTrue(f.UnlockTank);
            Assert.IsTrue(f.UnlockCrew);
            Assert.IsFalse(f.UnlockCoop);
            Assert.IsTrue(LaunchFlags.Read(Page + "?unlock=coop").UnlockCoop);
        }

        [Test]
        public void TheRoomIsTrimmedAndUpperCase()
        {
            Assert.AreEqual("ABCD", LaunchFlags.Read(Page + "?room=%20abcd%20").Room);
        }

        [Test]
        public void DemoAndLogPassThrough()
        {
            LaunchFlags f = LaunchFlags.Read(Page + "?demo=all&log=net:debug,game:warn");
            Assert.AreEqual("all", f.Demo);
            Assert.AreEqual("net:debug,game:warn", f.LogLevels);
        }

        [Test]
        public void TheMissionIsPickedByItsNumber()
        {
            Assert.AreSame(Missions.Two, LaunchFlags.Read(Page + "?mission=2").Mission);
            Assert.AreSame(Missions.One, LaunchFlags.Read(Page + "?mission=1&test=boss").Mission);
            Assert.IsNull(LaunchFlags.Read(Page + "?mission=99").Mission, "a mission this build doesn't have");
            Assert.IsNull(LaunchFlags.Read(Page).Mission);
        }

        [Test]
        public void SpikesAreNotLaunchFlags()
        {
            LaunchFlags f = LaunchFlags.Read(Page + "?spike=boss,crate");
            Assert.IsFalse(f.Boss || f.Crate, "?spike= is Spike's, not ?test=");
        }
    }
}
