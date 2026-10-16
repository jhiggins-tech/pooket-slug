using NUnit.Framework;
using UnityEngine;

namespace PooketSlug.Tests
{
    public class PlayerTankTests
    {
        static readonly float Diag = Mathf.Sqrt(0.5f);

        static void Same(Vector2 want, Vector2 got)
        {
            Assert.AreEqual(want.x, got.x, 1e-4f);
            Assert.AreEqual(want.y, got.y, 1e-4f);
        }

        [Test]
        public void TheGunPointsWhereTheStickPointsTheEightWays()
        {
            Vector2 last = Vector2.right;
            Same(Vector2.left, TankAim.Next(last, true, false, false, false));
            Same(Vector2.up, TankAim.Next(last, false, false, true, false));
            Same(Vector2.down, TankAim.Next(last, false, false, false, true));
            Same(new Vector2(Diag, Diag), TankAim.Next(last, false, true, true, false));
            Same(new Vector2(-Diag, Diag), TankAim.Next(last, true, false, true, false));
            Same(new Vector2(Diag, -Diag), TankAim.Next(last, false, true, false, true));
            Same(new Vector2(-Diag, -Diag), TankAim.Next(last, true, false, false, true));
        }

        [Test]
        public void LettingGoOfTheStickKeepsTheAim()
        {
            Vector2 aim = TankAim.Next(Vector2.right, true, false, true, false);
            Same(aim, TankAim.Next(aim, false, false, false, false));
            Same(aim, TankAim.Next(aim, true, true, true, true)); // both ways at once cancel out
        }

        [Test]
        public void TheAimAsAnAngle()
        {
            Assert.AreEqual(0f, TankAim.Angle(Vector2.right), 1e-3f);
            Assert.AreEqual(90f, TankAim.Angle(Vector2.up), 1e-3f);
            Assert.AreEqual(180f, Mathf.Abs(TankAim.Angle(Vector2.left)), 1e-3f);
            Assert.AreEqual(-45f, TankAim.Angle(new Vector2(Diag, -Diag)), 1e-3f);
        }

        [Test]
        public void RoundsAreFiveThreeAndOneAndNeverRefill()
        {
            var rounds = new TankRounds(new PlayerTankSpec());
            Assert.AreEqual(5, rounds.Main);
            Assert.AreEqual(3, rounds.Specials);
            Assert.AreEqual(1, rounds.Supers);
            for (int i = 0; i < 5; i++) Assert.IsTrue(rounds.UseMain());
            Assert.IsFalse(rounds.UseMain());
            for (int i = 0; i < 3; i++) Assert.IsTrue(rounds.UseSpecial());
            Assert.IsFalse(rounds.UseSpecial());
            Assert.IsFalse(rounds.Empty, "the super's still there");
            Assert.IsTrue(rounds.UseSuper());
            Assert.IsFalse(rounds.UseSuper());
            Assert.IsTrue(rounds.Empty);
            Assert.AreEqual(0, rounds.Main);
        }

        [Test]
        public void DrivingBurnsFuelAndADryTankStaysPut()
        {
            var fuel = new TankFuel(new PlayerTankSpec { Fuel = 2f });
            Assert.AreEqual(1f, fuel.Fraction, 1e-4f, "every tank arrives full");
            Assert.IsTrue(fuel.Burn(1.5f));
            Assert.AreEqual(0.25f, fuel.Fraction, 1e-4f);
            Assert.IsTrue(fuel.Burn(1f), "the last of it still moves the tank");
            Assert.IsTrue(fuel.Empty);
            Assert.AreEqual(0f, fuel.Left);
            Assert.IsFalse(fuel.Burn(0.1f), "dry: no more driving, and nothing refills it");
            Assert.IsFalse(fuel.Burn(0f));
        }

        [Test]
        public void EachTankHasItsOwnFullTank()
        {
            var spec = new PlayerTankSpec();
            var first = new TankFuel(spec);
            first.Burn(spec.Fuel);
            Assert.IsTrue(first.Empty);
            Assert.IsFalse(new TankFuel(spec).Empty, "a called-in tank comes full");
        }

        [Test]
        public void YourTankIsItsOwnThingNotTheBoss()
        {
            Assert.IsNotNull(Content.PlayerTank);
            Assert.Greater(Content.PlayerTank.Hp, 0);
            Assert.Greater(Content.PlayerTank.DriveSpeed, 0f);
            Assert.Less(Content.PlayerTank.DriveSpeed, Content.Tones2.RunSpeed, "a tank is a little slower than a soldier on foot");
        }
    }
}
