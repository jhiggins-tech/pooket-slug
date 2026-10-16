using NUnit.Framework;

namespace PooketSlug.Tests
{
    public class CallInCoachTests
    {
        const float Dt = 0.1f;

        static int Run(CallInCoach coach, float seconds, bool canCall = true, bool calling = false)
        {
            int shown = 0;
            for (float t = 0f; t < seconds - 1e-4f; t += Dt)
                if (coach.Tick(Dt, canCall, calling)) shown++;
            return shown;
        }

        [Test]
        public void ItWaitsForTheBannerToGo()
        {
            var coach = new CallInCoach(false);
            Run(coach, coach.Delay - 0.5f);
            Assert.AreEqual(CallInCoach.Phase.Waiting, coach.State);
            Assert.AreEqual(1, Run(coach, 1f), "it shows once, a moment after");
            Assert.IsTrue(coach.Showing);
        }

        [Test]
        public void ItStaysUntilYouCall()
        {
            var coach = new CallInCoach(false);
            Run(coach, 60f);
            Assert.IsTrue(coach.Showing, "however long it takes");
            Run(coach, Dt, calling: true);
            Assert.AreEqual(CallInCoach.Phase.Done, coach.State);
            Assert.AreEqual(0, Run(coach, 30f), "only once");
        }

        [Test]
        public void NoTankToCallNoPrompt()
        {
            var coach = new CallInCoach(false);
            Assert.AreEqual(0, Run(coach, 30f, canCall: false));
            Assert.AreEqual(CallInCoach.Phase.Waiting, coach.State);
            Assert.AreEqual(0, Run(new CallInCoach(true), 30f), "already seen");
        }
    }
}
