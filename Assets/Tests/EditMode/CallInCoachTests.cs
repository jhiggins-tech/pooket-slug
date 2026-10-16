using NUnit.Framework;

namespace PooketSlug.Tests
{
    public class CallInCoachTests
    {
        const float Dt = 0.1f;

        static int Run(CallInCoach coach, float seconds, bool canCall = true, bool calm = true, bool calling = false)
        {
            int shown = 0;
            for (float t = 0f; t < seconds - 1e-4f; t += Dt)
                if (coach.Tick(Dt, canCall, calm, calling)) shown++;
            return shown;
        }

        [Test]
        public void ItWaitsForTheStartBannerToGo()
        {
            var coach = new CallInCoach(false);
            Run(coach, coach.Delay - 0.5f);
            Assert.AreEqual(CallInCoach.Phase.Waiting, coach.State);
            Assert.AreEqual(1, Run(coach, 1f), "it shows once, a moment after");
            Assert.IsTrue(coach.Showing);
        }

        [Test]
        public void ItWaitsForACalmMoment()
        {
            var coach = new CallInCoach(false);
            Run(coach, 10f, calm: false);
            Assert.AreEqual(CallInCoach.Phase.Waiting, coach.State, "not in a fight");
            Run(coach, coach.CalmFor - 0.5f);
            Run(coach, Dt, calm: false); // a goon walks on: the calm starts over
            Run(coach, coach.CalmFor - 0.5f);
            Assert.AreEqual(CallInCoach.Phase.Waiting, coach.State);
            Run(coach, 1f);
            Assert.IsTrue(coach.Showing);
        }

        [Test]
        public void GoonsKeepComingSoItShowsAnywayLater()
        {
            var coach = new CallInCoach(false);
            Run(coach, coach.Latest - 0.5f, calm: false);
            Assert.AreEqual(CallInCoach.Phase.Waiting, coach.State);
            Assert.AreEqual(1, Run(coach, 1f, calm: false));
            Assert.IsTrue(coach.Showing);
        }

        [Test]
        public void ItGoesWhenThePlayerCallsOrAfterItsTime()
        {
            var coach = new CallInCoach(false);
            Run(coach, 6f);
            Assert.IsTrue(coach.Showing);
            Run(coach, Dt, calling: true);
            Assert.AreEqual(CallInCoach.Phase.Done, coach.State);

            coach = new CallInCoach(false);
            Run(coach, 6f);
            Run(coach, coach.ShowFor + Dt);
            Assert.AreEqual(CallInCoach.Phase.Done, coach.State);
            Assert.AreEqual(0, Run(coach, 30f), "only once");
        }

        [Test]
        public void NoTankToCallNoHint()
        {
            var coach = new CallInCoach(false);
            Assert.AreEqual(0, Run(coach, 30f, canCall: false));
            Assert.AreEqual(CallInCoach.Phase.Waiting, coach.State);
            Assert.AreEqual(0, Run(new CallInCoach(true), 30f), "already seen");
        }
    }
}
