using NUnit.Framework;
using UnityEngine;

namespace PooketSlug.Tests
{
    /// <summary>
    /// The staged figures (the den, the rooms, the tabk bay): where they stand. A soldier is drawn bigger than the
    /// stage's scale (see <see cref="StageKit.FigureScale"/>), but he still stands on the floor: his feet are placed
    /// with the world's own scale, so a room's way in is where he arrives.
    /// </summary>
    public class StageFigureTests
    {
        RectTransform m_World;

        [SetUp]
        public void MakeWorld()
        {
            var go = new GameObject("World");
            m_World = go.AddComponent<RectTransform>();
            m_World.sizeDelta = new Vector2(StageKit.StageW, StageKit.StageH) * StageKit.Px;
        }

        [TearDown]
        public void DropWorld()
        {
            Object.DestroyImmediate(m_World.gameObject);
            m_World = null;
        }

        [Test]
        public void AFigureStandsWhereItIsPut()
        {
            var f = new StageFigure(m_World, "you");
            f.At(100f, 15f);
            Assert.AreEqual(100f * StageKit.Px, f.Rt.anchoredPosition.x, 0.01f, "his feet are where he was put, in the world's scale");
            Assert.AreEqual(15f * StageKit.Px, f.Rt.anchoredPosition.y, 0.01f, "and on the floor");
        }

        [Test]
        public void AFigureIsBiggerThanTheWorldsScale()
        {
            var f = new StageFigure(m_World, "you");
            f.At(100f, 15f);
            // the figure's own art is drawn at his own scale, so he's taller than the stage's scale would make him
            Assert.Greater(StageKit.FigureScale, StageKit.Px, "the staged soldiers are drawn a touch bigger than the stage");
            Assert.AreEqual(32f * StageKit.FigureScale, f.Rt.sizeDelta.y, 0.01f, "and his art is at that scale");
        }

        [Test]
        public void ARoomsWayInIsWhereYouArrive()
        {
            // the kitchen's way in is at its left edge: standing there puts you at the door, not past it
            var f = new StageFigure(m_World, "you");
            float x = RoomMap.WayX(RoomId.Kitchen, RoomWay.Back);
            f.At(x, RoomMap.FloorY);
            Assert.AreEqual(x * StageKit.Px, f.Rt.anchoredPosition.x, 0.01f, "the way in is where you stand");
        }
    }
}
