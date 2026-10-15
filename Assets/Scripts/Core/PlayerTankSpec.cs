using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Your tank as numbers (<c>docs/design/tanks.md</c>, Driving a tank): the one you climb into or call in, not the
    /// boss tank (<see cref="BossTankSpec"/>). It drives and aims Metal Slug style: the stick drives it and points
    /// the gun the eight ways, FIRE shoots at once, and its rounds never refill. Placeholders, to tune in play
    /// (spike: <c>?spike=drive</c>).
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

        /// <summary>Rounds for the whole ride: they never refill.</summary>
        public int MainShots = 5, Specials = 3, Supers = 1;
        /// <summary>FIRE: a cannon shell straight along the aim.</summary>
        public float ShellSpeed = 15f, ShellCooldown = 0.35f;
        public int ShellDamage = 8;
        public float ShellBlast = 1.5f;
        /// <summary>SP: a mortar lobbed high over cover, forward.</summary>
        public Vector2 MortarThrow = new Vector2(6f, 12f);
        public int MortarDamage = 12;
        public float MortarBlast = 2f;
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
}
