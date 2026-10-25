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
    
        [TestCase("GameIsFull", true), TestCase("GameIsFull", false)]
        public void AFullRoomReadsTheSameWhoeverCounted(string reason, bool connecting)
        {
            Assert.AreEqual("THE ROOM IS FULL", RoomWords.ForShutdown(reason, connecting));
            Assert.AreEqual(RoomWords.Full, RoomWords.ForShutdown(reason, connecting));
        }

        [Test]
        public void AClosedRoomIsCalledTheRoomAndNoGameIsNamed()
        {
            Assert.AreEqual("THE ROOM IS CLOSED", RoomWords.ForShutdown("GameClosed", true));
            foreach (string r in new[] { "GameIsFull", "GameClosed", "GameNotFound", "MaxCcuReached", "Error", "ConnectionRefused", "Nope" })
                foreach (bool c in new[] { true, false })
                    StringAssert.DoesNotContain("GAME IS", RoomWords.ForShutdown(r, c));
        }

        [Test]
        public void ConnectionTroubleIsTheSameWordsAsBefore()
        {
            Assert.AreEqual(RoomWords.Unreachable, RoomWords.ForShutdown("ConnectionTimeout", true));
            Assert.AreEqual("CONNECTION TIMED OUT", RoomWords.ForShutdown("OperationTimeout", false));
            Assert.AreEqual("CONNECTION REFUSED", RoomWords.ForShutdown("ConnectionRefused", false));
            Assert.AreEqual("DISCONNECTED (ERROR)", RoomWords.ForShutdown("Error", false));
            Assert.AreEqual("DISCONNECTED BY THE SERVER", RoomWords.ForShutdown("DisconnectedByPluginLogic", false));
            Assert.AreEqual("PHOTON REJECTED THIS BUILD'S APP ID", RoomWords.ForShutdown("InvalidAuthentication", true));
            Assert.AreEqual("DISCONNECTED (GAMENOTFOUND)", RoomWords.ForShutdown("GameNotFound", false));
        }
    }
}
