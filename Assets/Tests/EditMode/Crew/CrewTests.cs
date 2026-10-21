using System.Collections.Generic;
using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>The crew's records (<see cref="Crew"/>): one per crewmate, and who's been freed.</summary>
    public class CrewTests
    {
        [Test]
        public void OnlyTones2IsThereFromTheStart()
        {
            CollectionAssert.AreEqual(new[] { Content.Tones2 }, Crew.Freed(new Profile()));
            CollectionAssert.AreEqual(new[] { Content.Tones2 }, Crew.Freed(null));
        }

        [Test]
        public void BeatingAMissionFreesItsCrewmate()
        {
            var p = new Profile();
            p.RecordRun(Missions.One, Setting.Normal, 900, won: false, solo: true);
            Assert.IsFalse(Crew.IsFree(Crew.Kie, p), "losing frees nobody");
            p.RecordRun(Missions.One, Setting.Normal, 1200, won: true, solo: true);
            CollectionAssert.AreEqual(new[] { Content.Tones2, Content.Kie }, Crew.Freed(p));
            Assert.AreSame(Crew.Kie, Crew.FreedBy(Missions.One));
        }

        [Test]
        public void ClearingMissionTwoFreesKcaj()
        {
            var p = new Profile();
            p.RecordRun(Missions.One, Setting.Normal, 1200, won: true, solo: true);
            Assert.IsFalse(Crew.IsFree(Crew.Kcaj, p), "Mission one frees kie only");
            p.RecordRun(Missions.Two, Setting.Normal, 1500, won: true, solo: false);
            CollectionAssert.AreEqual(new[] { Content.Tones2, Content.Kie, Content.Kcaj }, Crew.Freed(p));
            Assert.AreSame(Crew.Kcaj, Crew.FreedBy(Missions.Two));
            Assert.AreSame(Content.Kcaj, Crew.Pick(Content.Kcaj.Id, Crew.Freed(p)));
        }

        [Test]
        public void EveryoneIsFreeForTesting()
        {
            Assert.AreEqual(Crew.All.Count, Crew.Freed(new Profile(), everyone: true).Count);
        }

        [Test]
        public void EachCrewmateHasOneRecord()
        {
            var soldiers = new HashSet<SoldierDef>();
            foreach (CrewMember m in Crew.All)
            {
                Assert.IsTrue(soldiers.Add(m.Soldier), m.Name);
                Assert.AreSame(m, Crew.Of(m.Soldier));
                Assert.IsTrue(m.Paint.ContainsKey('r'), m.Name + ": their paint, the same from either seat");
                Assert.Greater(m.Moves.Count, 0, m.Name);
                Assert.AreSame(m.Moves, KitGuide.For(m.Soldier));
                Assert.Greater(m.Card.a, 0f, m.Name);
            }
            Assert.IsNull(Crew.Of(Content.Trooper), "the trooper isn't crew");
            Assert.IsNull(Crew.Tones2.FreedBy, "tones2 is there from the start");
        }

        [Test]
        public void EveryCrewmatesMovesAreInTheGuide()
        {
            int moves = 0;
            foreach (CrewMember m in Crew.All) moves += m.Moves.Count;
            Assert.AreEqual(moves, KitGuide.All.Count);
        }
    }
}
