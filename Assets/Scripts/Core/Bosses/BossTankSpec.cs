using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// The boss tank, fighting like a Pocket Tanks tank (<c>docs/design/tanks.md</c>, Two kinds of tank), and its numbers. It drives
    /// a short way, stops, swings its turret round to the angle it wants, charges the shot (a meter fills and a
    /// marker shows where it'll land) and lobs it. Every third shot is a spread of three. It gets quicker as it's
    /// hurt: at no health left, everything runs <see cref="MaxRage"/> times as fast.
    /// </summary>
    public sealed class BossTankSpec
    {
        /// <summary>How far either side of where it starts it patrols, and how fast it drives.</summary>
        public float PatrolHalfWidth = 3f, DriveSpeed = 1.6f;
        /// <summary>Seconds of driving between shots, at most.</summary>
        public float DriveTime = 1.6f;
        /// <summary>How fast the turret swings, degrees per second.</summary>
        public float TurnRate = 70f;
        /// <summary>The turret's reach, degrees up from facing right: it can lob over its own back.</summary>
        public float MinAngle = 15f, MaxAngle = 165f;
        /// <summary>The steepest and flattest lobs it picks from, degrees above the horizontal.</summary>
        public float LowLob = 40f, HighLob = 65f;
        /// <summary>Seconds charging a shot, with the meter filling and the landing marker showing.</summary>
        public float ChargeTime = 1.1f;
        /// <summary>The fastest it can throw a shell. A shot that needs more falls short.</summary>
        public float MaxPower = 16f;
        /// <summary>Random error in the power of each shot, as a fraction: it isn't a perfect shot.</summary>
        public float PowerError = 0.05f;
        /// <summary>Every this many shots is a spread of three, at these powers.</summary>
        public int SpreadEvery = 3;
        public float SpreadPower = 0.15f;
        /// <summary>Seconds it rests after firing.</summary>
        public float RecoilTime = 0.45f;
        public float MaxRage = 1.8f;
        /// <summary>What the HUD and the intro call it.</summary>
        public string Name = "KIE'S TABK";
        /// <summary>
        /// The entrance (<c>docs/design/tanks.md</c>, The boss intro): it rolls out of the motor pool's shutter to its
        /// patrol at this speed, taking at most this many seconds, and can't be hurt or fire until it stops.
        /// </summary>
        public float EntranceSpeed = 2.4f, EntranceTime = 3.5f;

        /// <summary>How much quicker it is at <paramref name="health"/> (0 to 1) left.</summary>
        public float Rage(float health) => Mathf.Lerp(MaxRage, 1f, Mathf.Clamp01(health));

        /// <summary>
        /// Whether the tabk wakes: once the view's right edge reaches past it (<paramref name="tankX"/> is the nearer of
        /// where it is and where it parks), or the view's as far right as the map lets it go: a tabk parked by the map's
        /// end (kcaj's, at the cold store) can't be shown with room to spare on a narrow co-op screen.
        /// </summary>
        public static bool Wakes(float viewRight, float tankX, float halfWidth, float scrollLimit) =>
            viewRight >= Mathf.Min(tankX + halfWidth + 0.5f, scrollLimit - 0.25f);

        /// <summary>
        /// The launch speed that lands a shell <paramref name="dx"/> across and <paramref name="dy"/> up, thrown at
        /// <paramref name="angle"/> degrees above the horizontal under <paramref name="gravity"/>; or
        /// <see cref="float.NaN"/> if that angle can't reach it.
        /// </summary>
        public static float PowerFor(float dx, float dy, float angle, float gravity)
        {
            float a = angle * Mathf.Deg2Rad, x = Mathf.Abs(dx);
            float cos = Mathf.Cos(a), denom = 2f * cos * cos * (x * Mathf.Tan(a) - dy);
            if (denom <= 0f) return float.NaN;
            return Mathf.Sqrt(gravity * x * x / denom);
        }
    }
}
