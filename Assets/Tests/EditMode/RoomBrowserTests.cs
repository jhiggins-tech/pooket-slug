using System.Collections.Generic;
using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>Rooms: what the room browser offers for each listed room, in what order, and what it says.</summary>
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
        public void RoomsToJoinComeFirstThenGamesToWatch()
        {
            var rows = RoomBrowser.Rows(new[] { Room("WXYZ", 2), Room("MNPQ", 2, RoomBrowser.MaxWatchers), Room("HJKL", 1), Room("ABCD", 2) }, All);
            CollectionAssert.AreEqual(new[] { "HJKL", "ABCD", "WXYZ", "MNPQ" }, rows.ConvertAll(r => r.Room.Name));
            CollectionAssert.AreEqual(new[] { RoomAction.Join, RoomAction.Watch, RoomAction.Watch, RoomAction.Full }, rows.ConvertAll(r => r.Action));
        }

        [Test]
        public void TheBrowserShowsAtMostItsRowsAndSkipsNamelessRooms()
        {
            var rooms = new List<RoomListing> { Room("", 1) };
            for (int i = 0; i < RoomBrowser.MaxRows + 3; i++) rooms.Add(Room("R" + (char)('A' + i) + "AA", 1));
            List<RoomRow> rows = RoomBrowser.Rows(rooms, All);
            Assert.AreEqual(RoomBrowser.MaxRows, rows.Count);
            Assert.IsFalse(rows.Exists(r => r.Room.Name == ""));
        }

        [Test]
        public void ALineSaysWhoWhatAndHowFull()
        {
            var room = Room("ABCD", 2, 3, underWay: true);
            room.Host = Content.Tones2.Id;
            Assert.AreEqual("ABCD  TONES2  TEST MISSION  2/2 PLAYING  3 WATCHING", RoomBrowser.Line(room));
            Assert.AreEqual("HJKL  TEST MISSION  1/2 IN THE DEN", RoomBrowser.Line(Room("HJKL", 1)), "no host soldier yet, nobody watching");
            Assert.AreEqual("WXYZ  A NEWER MISSION  1/2 IN THE DEN", RoomBrowser.Line(Room("WXYZ", 1, mission: 200)));
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
