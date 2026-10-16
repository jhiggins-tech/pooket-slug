using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Your tank as numbers (<c>docs/design/tanks.md</c>, Driving a tank): the one you climb into or call in, not the
    /// boss tank (<see cref="BossTankSpec"/>). It drives and aims Metal Slug style: the stick drives it and points
    /// the gun the eight ways, FIRE shoots at once, and its rounds never refill. Placeholders, to tune in play.
    /// </summary>
    public sealed class PlayerTankSpec
    {
        /// <summary>Its own health, apart from the driver's hearts.</summary>
        public int Hp = 12;
        /// <summary>Seconds it can't be hurt again after a hit, so a burst of bullets doesn't strip it at once.</summary>
        public float HurtGrace = 0.3f;
        /// <summary>Units per second, a little slower than a soldier on foot.</summary>
        public float DriveSpeed = 4.2f;
        /// <summary>JUMP hops it, up a step or over a goon (an open question in the design: tune or drop in play).</summary>
        public float HopVelocity = 12f;
        public float HalfWidth = 1.35f, Height = 1.3f;
        /// <summary>
        /// Seconds of driving in a full tank, as in Pooket Tabks: each tank arrives full and never refuels. Dry, it
        /// can't drive or hop, but can still aim and fire; ejecting is the way out (<see cref="TankFuel"/>).
        /// </summary>
        public float Fuel = 18f; // the spike's 12 was a little short (owner, 2026-10-16)
        /// <summary>Seconds of fuel a hop burns.</summary>
        public float HopFuel = 0.5f;
        /// <summary>
        /// Driving over a goon squashes it, the payoff for spending fuel: anything on foot with this much health or less
        /// (soldiers, runners, weasels), never barrels, tanks or a pilot. Dry, or standing still, the tank squashes nothing.
        /// </summary>
        public int CrushMaxHp = 10;

        /// <summary>Whether driving over this kind of enemy squashes it (<see cref="CrushMaxHp"/>).</summary>
        public bool Crushes(EnemyDef def)
        {
            if (def == null || def.Hp > CrushMaxHp) return false;
            switch (def.Brain)
            {
                case EnemyBrain.Rifleman:
                case EnemyBrain.Runner:
                case EnemyBrain.Drone:
                case EnemyBrain.Weasel:
                    return true;
                default:
                    return false; // barrels, tanks and their copies, the pilot and his decoy
            }
        }

        /// <summary>Rounds for the whole ride: they never refill.</summary>
        public int MainShots = 5, Specials = 3, Supers = 1;
        // FIRE's shell and SP's mortar are Content.PlayerTankCannon and Content.PlayerTankMortar.
        /// <summary>SU: a fan of shells along the aim.</summary>
        public int BarrageShells = 7;
        public float BarrageFan = 35f;

        /// <summary>The barrel turns to the aim this fast (degrees per second), for the look only: shots go along the aim at once.</summary>
        public float BarrelTurn = 900f;
        public float BarrelLength = 20f / 16f;

        /// <summary>A called-in tank drops from above the screen this fast, and its landing blasts what's under it.</summary>
        public float DropSpeed = 14f;
        public int LandingDamage = 6;
        public float LandingBlast = 1.8f;

        /// <summary>Ejected, the empty tank rolls on this fast and blows up on the first goon or wall, or after this long.</summary>
        public float RunawaySpeed = 6.5f, RunawayTime = 2.2f;
        public int RunawayDamage = 25;
        public float RunawayBlast = 3f;
        /// <summary>The driver pops out of the hatch like this, and can't be hurt for a moment.</summary>
        public Vector2 EjectVelocity = new Vector2(-2.5f, 15f);
        public float EjectShield = 1.2f;
    }

    /// <summary>
    /// Where your tank's gun points: wherever the stick points, the eight ways, and where it last pointed when the
    /// stick is let go (Metal Slug's tank keeps its aim while you drive with it centred).
    /// </summary>
    public static class TankAim
    {
        static readonly float Diag = Mathf.Sqrt(0.5f);

        /// <summary>The aim after this frame's stick. Pushing both ways at once cancels out, as with the arrow keys.</summary>
        public static Vector2 Next(Vector2 last, bool left, bool right, bool up, bool down)
        {
            int x = (right ? 1 : 0) - (left ? 1 : 0), y = (up ? 1 : 0) - (down ? 1 : 0);
            if (x == 0 && y == 0) return last;
            if (x != 0 && y != 0) return new Vector2(x * Diag, y * Diag);
            return new Vector2(x, y);
        }

        /// <summary>The aim as an angle in degrees, 0 to the right, counterclockwise.</summary>
        public static float Angle(Vector2 aim) => Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
    }

    /// <summary>A tank's fuel (<see cref="PlayerTankSpec.Fuel"/>): driving burns it, nothing refills it, and dry, the tank stays put.</summary>
    public sealed class TankFuel
    {
        public readonly float Max;
        public float Left { get; private set; }

        public TankFuel(PlayerTankSpec spec)
        {
            Max = Mathf.Max(0.01f, spec.Fuel);
            Left = Max;
        }

        public bool Empty => Left <= 0f;
        /// <summary>How full, 0 to 1.</summary>
        public float Fraction => Mathf.Clamp01(Left / Max);

        /// <summary>
        /// Burns <paramref name="seconds"/> of fuel, if there's any left. False when the tank is already dry; the last of
        /// it still moves the tank, so a tank never stalls short of its full drive.
        /// </summary>
        public bool Burn(float seconds)
        {
            if (Empty) return false;
            Left = Mathf.Max(0f, Left - Mathf.Max(0f, seconds));
            return true;
        }
    }

    /// <summary>A tank's rounds for the ride (<see cref="PlayerTankSpec"/>): spent one at a time, never refilled.</summary>
    public sealed class TankRounds
    {
        public int Main, Specials, Supers;

        public TankRounds(PlayerTankSpec spec)
        {
            Main = spec.MainShots;
            Specials = spec.Specials;
            Supers = spec.Supers;
        }

        public bool UseMain() => Use(ref Main);
        public bool UseSpecial() => Use(ref Specials);
        public bool UseSuper() => Use(ref Supers);

        /// <summary>Nothing left to fire: time to eject.</summary>
        public bool Empty => Main <= 0 && Specials <= 0 && Supers <= 0;

        static bool Use(ref int count)
        {
            if (count <= 0) return false;
            count--;
            return true;
        }
    }

    /// <summary>
    /// A player's tank as their partner sees it, packed into one replicated int beside the soldier's pose: what it's
    /// doing, which way it faces and aims, its health, and how many tanks of theirs have blown up (a change means one
    /// just did, so the partner's screen shows the blast). The tank's position travels on its own.
    /// </summary>
    public struct TankPose
    {
        public enum Mode { None, Dropping, Parked, Driven, Runaway }

        const int ModeMask = 7, FacingRightBit = 1 << 3, AimShift = 4, AimMask = 7 << AimShift, HpShift = 7, HpMask = 63 << HpShift;
        const int FlashBit = 1 << 13, MuzzleBit = 1 << 14, BlownShift = 15, BlownMask = 15 << BlownShift;

        public Mode State;
        public int Facing;
        /// <summary>Which of the eight ways the gun points: 0 right, counterclockwise in 45° steps.</summary>
        public int AimOctant;
        public int Hp;
        public bool Flash, Muzzle;
        /// <summary>Tanks of theirs that have blown up so far (it wraps at 16).</summary>
        public int Blown;

        public static int Octant(Vector2 aim) => ((Mathf.RoundToInt(TankAim.Angle(aim) / 45f) % 8) + 8) % 8;

        public static Vector2 AimOf(int octant)
        {
            float a = octant * 45f * Mathf.Deg2Rad;
            Vector2 v = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            return new Vector2(Mathf.Abs(v.x) < 1e-4f ? 0f : v.x, Mathf.Abs(v.y) < 1e-4f ? 0f : v.y);
        }

        public int Pack() =>
            ((int)State & ModeMask) | (Facing > 0 ? FacingRightBit : 0) | ((AimOctant & 7) << AimShift) |
            (Mathf.Clamp(Hp, 0, 63) << HpShift) | (Flash ? FlashBit : 0) | (Muzzle ? MuzzleBit : 0) | ((Blown & 15) << BlownShift);

        public static TankPose Unpack(int flags) => new TankPose
        {
            State = (Mode)Mathf.Clamp(flags & ModeMask, 0, (int)Mode.Runaway),
            Facing = (flags & FacingRightBit) != 0 ? 1 : -1,
            AimOctant = (flags & AimMask) >> AimShift,
            Hp = (flags & HpMask) >> HpShift,
            Flash = (flags & FlashBit) != 0,
            Muzzle = (flags & MuzzleBit) != 0,
            Blown = (flags & BlownMask) >> BlownShift,
        };
    }
}
