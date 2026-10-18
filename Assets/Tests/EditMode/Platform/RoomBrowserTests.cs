using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>Rooms: whether a phone arriving in the co-op room plays or watches, and who counts as watching.</summary>
    public class RoomBrowserTests
    {
        static RoomListing Room(string name, int soldiers, int watching = 0, bool underWay = false, int mission = 0) =>
            new RoomListing { Name = name, Soldiers = soldiers, Watching = watching, UnderWay = underWay, Mission = mission, Host = -1 };

        static bool All(int mission) => true;
        static bool None(int mission) => false;

        [Test]
        public void AFreeSeatCanBeJoinedWhateverTheMission()
        {
            Assert.AreEqual(RoomAction.Join, RoomBrowser.ActionFor(Room("ABCD", 1, underWay: true), None), "dropping in needs no clear");
            Assert.AreEqual(RoomAction.Join, RoomBrowser.ActionFor(Room("ABCD", 0), None));
        }

        [Test]
        public void AFullRoomCanOnlyBeWatchedOnceItsMissionIsCleared()
        {
            Assert.AreEqual(RoomAction.Watch, RoomBrowser.ActionFor(Room("ABCD", 2), All));
            Assert.AreEqual(RoomAction.Locked, RoomBrowser.ActionFor(Room("ABCD", 2), None), "no spoilers");
            Assert.AreEqual(RoomAction.Full, RoomBrowser.ActionFor(Room("ABCD", 2, RoomBrowser.MaxWatchers), All), "no room for another watcher");
        }

        [Test]
        public void ARoomHoldsThePlayersAndItsWatchers()
        {
            Assert.AreEqual(Tuning.Coop.MaxPlayers + RoomBrowser.MaxWatchers, RoomBrowser.MaxPeople);
            Assert.LessOrEqual(RoomBrowser.MaxPeople, 20, "Photon's free plan: 20 connections in all");
        }

        [Test]
        public void AJoiningPlayerIsNotCountedAsWatching()
        {
            var count = new WatcherCount();
            for (int i = 0; i < 10; i++) count.Tick(0.1f, 1); // a second without a soldier: they're still arriving
            for (int i = 0; i < 10; i++) count.Tick(0.1f, 0); // then their soldier spawns
            Assert.AreEqual(0, count.Shown);
        }

        [Test]
        public void AWatcherIsCountedOnceTheyveHeldForTwoSeconds()
        {
            var count = new WatcherCount();
            for (int i = 0; i < 19; i++) count.Tick(0.1f, 1);
            Assert.AreEqual(0, count.Shown, "not yet");
            for (int i = 0; i < 3; i++) count.Tick(0.1f, 1);
            Assert.AreEqual(1, count.Shown);
            for (int i = 0; i < 25; i++) count.Tick(0.1f, 0);
            Assert.AreEqual(0, count.Shown, "and uncounted once they've gone");
        }
    }
}
