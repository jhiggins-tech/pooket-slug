using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>One of each tabk in co-op (<see cref="CoopTabks"/>): what's taken, who gives way, and what the drone swaps in.</summary>
    public class CoopTabksTests
    {
        static readonly TabkCard[] All = { TabkCards.Tones2, TabkCards.Kie, TabkCards.Kcaj };

        static DenPartner InDen(TabkCard tabk) => new DenPartner { Flags = Den.InDen, Soldier = Content.Kie, Tabk = tabk.Id };
        static DenPartner Playing(TabkCard tabk) => new DenPartner { Soldier = Content.Kie, Playing = true, Tabk = tabk.Id };

        [Test]
        public void APartnersTabkIsTakenWhileTheyreInTheDenOrTheMission()
        {
            Assert.AreSame(TabkCards.Kie, CoopTabks.Taken(InDen(TabkCards.Kie)), "in the den, seated or not");
            Assert.AreSame(TabkCards.Kcaj, CoopTabks.Taken(Playing(TabkCards.Kcaj)));
            Assert.IsNull(CoopTabks.Taken(default), "nobody there");
            Assert.IsNull(CoopTabks.Taken(new DenPartner { Flags = Den.InDen, Tabk = 0 }), "they haven't said yet");
            Assert.IsNull(CoopTabks.Taken(new DenPartner { Soldier = Content.Kie, Tabk = TabkCards.Kie.Id }), "not in the den, not playing");
        }

        [Test]
        public void AClashGoesToWhoeversInTheMissionElseTheHost()
        {
            TabkCard kie = TabkCards.Kie;
            Assert.IsTrue(CoopTabks.GivesWay(kie, Playing(kie), authority: true), "they're in the mission: even the host gives way");
            Assert.IsTrue(CoopTabks.GivesWay(kie, Playing(kie), authority: false));
            Assert.IsFalse(CoopTabks.GivesWay(kie, InDen(kie), authority: true), "both in the den: the host keeps it");
            Assert.IsTrue(CoopTabks.GivesWay(kie, InDen(kie), authority: false), "and the guest gives way");
            Assert.IsFalse(CoopTabks.GivesWay(kie, InDen(TabkCards.Tones2), authority: false), "no clash");
            Assert.IsFalse(CoopTabks.GivesWay(kie, default, authority: false), "nobody there");
        }

        [Test]
        public void TheDroneSwapsInTheNextWonTabkInBayOrder()
        {
            Assert.AreSame(TabkCards.Kcaj, CoopTabks.Next(TabkCards.Kie, All, TabkCards.Kie), "kie's (space 1) gives way to kcaj's (the top deck)");
            Assert.AreSame(TabkCards.Tones2, CoopTabks.Next(TabkCards.Kcaj, All, TabkCards.Kcaj), "round to the start");
            Assert.AreSame(TabkCards.Kie, CoopTabks.Next(TabkCards.Tones2, All, TabkCards.Tones2));
            Assert.AreSame(TabkCards.Kie, CoopTabks.Next(TabkCards.Tones2, new[] { TabkCards.Tones2, TabkCards.Kie }, TabkCards.Tones2),
                "after Mission one: tones2's and kie's");
            Assert.IsNull(CoopTabks.Next(TabkCards.Tones2, new[] { TabkCards.Tones2 }, TabkCards.Tones2), "only one won: nothing to swap in");
        }

        [Test]
        public void TheNextOneIsNeverThePartnersEither()
        {
            // a tabk that isn't the partner's can't clash, but Next never hands over theirs whatever it's asked
            Assert.AreSame(TabkCards.Kcaj, CoopTabks.Next(TabkCards.Tones2, All, TabkCards.Kie));
        }
    }
}
