using System;
using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>A press held till it's taken, once; and a handler made from its Set method sets the latch itself.</summary>
    public class LatchTests
    {
        [Test]
        public void APressIsTakenOnce_AndClearForgetsIt()
        {
            var back = new Latch();
            Assert.IsFalse(back.Take());
            back.Set();
            Assert.IsTrue(back.Take());
            Assert.IsFalse(back.Take(), "taken");
            back.Set();
            back.Clear();
            Assert.IsFalse(back.Take());
        }

        [Test]
        public void AHandlerMadeFromSet_SetsTheLatchItself()
        {
            var skip = new Latch();
            Action onClick = skip.Set; // as a button's click handler is made
            onClick();
            Assert.IsTrue(skip.Take());

            var place = new Latch<DenPlace>();
            Action<DenPlace> onTap = place.Set;
            onTap(DenPlace.Couch);
            Assert.IsTrue(place.Take(out DenPlace p));
            Assert.AreEqual(DenPlace.Couch, p);
            Assert.IsFalse(place.Take(out p), "taken");
            Assert.AreEqual(default(DenPlace), p);
        }
    }
}
