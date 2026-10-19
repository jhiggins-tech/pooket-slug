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
        public void ARealTankHasMoreFuelThanTheSpikesTwelveSeconds()
        {
            Assert.Greater(Content.PlayerTank.Fuel, 12f);
        }

        [Test]
        public void DrivingOverGoonsOnFootSquashesThem()
        {
            PlayerTankSpec spec = Content.PlayerTank;
            Assert.IsTrue(spec.Crushes(Content.Rifleman));
            Assert.IsTrue(spec.Crushes(Content.Runner));
            Assert.IsTrue(spec.Crushes(Content.Weasel));
            Assert.IsFalse(spec.Crushes(Content.Barrel), "barrels aren't squashed");
            Assert.IsFalse(spec.Crushes(Content.Tank), "nor the boss");
            Assert.IsFalse(spec.Crushes(Content.TankCopy));
            Assert.IsFalse(spec.Crushes(Content.Pilot), "nor the pilot, a boss duel");
            Assert.IsFalse(spec.Crushes(Content.PilotDecoy));
            Assert.IsFalse(spec.Crushes(null));
        }

        [Test]
        public void ItsGunsHaveTheirOwnIdsOnTheWire()
        {
            Assert.AreEqual(18, Content.PlayerTankCannon.Id);
            Assert.AreEqual(19, Content.PlayerTankMortar.Id);
            Assert.AreEqual(9, Content.PlayerTankShellShot.Id);
            Assert.AreEqual(10, Content.PlayerTankMortarShot.Id);
            Assert.AreSame(Content.PlayerTankCannon, Content.Weapon(18));
        }

        [Test]
        public void APartnerSeesTheTankAsItIs()
        {
            var pose = new TankPose { State = TankPose.Mode.Driven, Facing = -1, AimOctant = 3, Hp = 12, Flash = true, Muzzle = false, Blown = 5 };
            TankPose back = TankPose.Unpack(pose.Pack());
            Assert.AreEqual(TankPose.Mode.Driven, back.State);
            Assert.AreEqual(-1, back.Facing);
            Assert.AreEqual(3, back.AimOctant);
            Assert.AreEqual(12, back.Hp);
            Assert.IsTrue(back.Flash);
            Assert.IsFalse(back.Muzzle);
            Assert.AreEqual(5, back.Blown);
            Assert.AreEqual(TankPose.Mode.None, TankPose.Unpack(0).State, "no tank out");
            Assert.AreEqual(0, TankPose.Unpack(new TankPose { Blown = 16 }.Pack()).Blown, "the blast count wraps");
        }

        [Test]
        public void APartnerSeesWhoseTabkItIs()
        {
            foreach (TabkCard card in TabkCards.All)
            {
                TankPose back = TankPose.Unpack(new TankPose { State = TankPose.Mode.Driven, Facing = 1, Hp = 63, Blown = 15, Tabk = card.Id, Spin = true }.Pack());
                Assert.AreEqual(card.Id, back.Tabk);
                Assert.IsTrue(back.Spin);
                Assert.AreEqual(63, back.Hp, "nothing else is touched");
                Assert.AreEqual(15, back.Blown);
            }
            Assert.IsFalse(TankPose.Unpack(new TankPose { State = TankPose.Mode.Driven }.Pack()).Spin);
        }

        [Test]
        public void TheEightAimsSurviveTheTrip()
        {
            Vector2[] aims = { Vector2.right, new Vector2(Diag, Diag), Vector2.up, new Vector2(-Diag, Diag), Vector2.left,
                new Vector2(-Diag, -Diag), Vector2.down, new Vector2(Diag, -Diag) };
            for (int i = 0; i < aims.Length; i++)
            {
                Assert.AreEqual(i, TankPose.Octant(aims[i]));
                Same(aims[i], TankPose.AimOf(i));
            }
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
