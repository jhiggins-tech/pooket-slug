using UnityEngine;

namespace PooketSlug
{
    /// <summary>tones2's kit as numbers (<c>docs/design/kits.md</c>): his jet, his rocket jump and his spew.</summary>
    public static class Tones2Tuning
    {
        public static readonly JetSpec Jet = new JetSpec();
        public static readonly RocketSpec Rocket = new RocketSpec();
        public static readonly SpewSpec Spew = new SpewSpec();
        /// <summary>His tabk's guns: tabk-sized versions of these (<see cref="Tones2TabkSpec"/>).</summary>
        public static readonly Tones2TabkSpec Tabk = new Tones2TabkSpec();
    }

    /// <summary>
    /// tones2's ten-1, the water jet, as numbers: hold FIRE and the pressure builds from a spray to a clean arc in
    /// about a second; letting go resets it. Everything the jet does follows from the pressure (0 to 1).
    /// </summary>
    public sealed class JetSpec
    {
        /// <summary>Seconds of holding FIRE to reach full pressure.</summary>
        public float RampTime = 1f;
        /// <summary>Droplet speed, units per second, at no pressure and at full.</summary>
        public float MinSpeed = 5.5f, MaxSpeed = 17f;
        /// <summary>Random spread either side of the aim, in degrees: a wide spray at first, a clean jet at full.</summary>
        public float MaxSpread = 24f, MinSpread = 1.5f;
        /// <summary>How fast droplets fall: a weak spray drops short, a full jet carries in a long arc.</summary>
        public float MaxGravity = 30f, MinGravity = 8f;
        public float MinDropsPerSecond = 22f, MaxDropsPerSecond = 46f;
        /// <summary>Damage each droplet soaks into what it hits. It adds up: a whole point every few droplets.</summary>
        public float MinDamage = 0.04f, MaxDamage = 0.16f;
        /// <summary>How hard each droplet pushes what it hits, units per second (added up, to <see cref="MaxShove"/>).</summary>
        public float MinPush = 0.25f, MaxPush = 0.9f;
        public float MaxShove = 4.5f;
        public float DropLife = 1.3f;
        /// <summary>Droplets that hit a wall or something big this close to tones2 splash back onto him.</summary>
        public float SplashRange = 2.4f;
        /// <summary>Splashback doesn't hurt: it slows tones2 down to this much of his speed, for this long.</summary>
        public float SplashSlow = 0.55f, SplashSlowTime = 0.35f;

        /// <summary>The pressure after holding FIRE for <paramref name="held"/> seconds: eases in, then holds at 1.</summary>
        public float Pressure(float held)
        {
            float t = Mathf.Clamp01(held / RampTime);
            return t * t * (3f - 2f * t);
        }

        public float Speed(float pressure) => Mathf.Lerp(MinSpeed, MaxSpeed, pressure);
        public float Spread(float pressure) => Mathf.Lerp(MaxSpread, MinSpread, pressure);
        public float Gravity(float pressure) => Mathf.Lerp(MaxGravity, MinGravity, pressure);
        public float DropsPerSecond(float pressure) => Mathf.Lerp(MinDropsPerSecond, MaxDropsPerSecond, pressure);
        public float Damage(float pressure) => Mathf.Lerp(MinDamage, MaxDamage, pressure);
        public float Push(float pressure) => Mathf.Lerp(MinPush, MaxPush, pressure);
    }

    /// <summary>tones2's ten-2, the rocket jump: shake, then blast straight up, carpeting the floor in toxic mud.</summary>
    public sealed class RocketSpec
    {
        /// <summary>Seconds of shaking, rooted to the spot, before the launch.</summary>
        public float ShakeTime = 0.5f;
        /// <summary>Upward speed at launch. A normal jump is 17; this reaches about twice as high.</summary>
        public float LaunchVelocity = 24f;
        /// <summary>The special's charges, and the seconds each one takes to come back.</summary>
        public int Charges = 3;
        public float RechargeTime = 5f;
        /// <summary>How far either side of the launch the mud spreads, and how long it stays.</summary>
        public float MudHalfWidth = 1.75f, MudLife = 7f;
        /// <summary>Goons standing in the mud take a point of poison this often.</summary>
        public float PoisonInterval = 0.5f;
    }

    /// <summary>
    /// tones2's ten-3, the super: a second and a half of spew that melts anything close, and leaves sludge on the
    /// floor that burns goons who walk through it. He can move while it pours out, and nothing hurts him.
    /// </summary>
    public sealed class SpewSpec
    {
        public float Duration = 1.5f;
        /// <summary>Gobbets per second, their speed and spread (degrees either side of the aim) and how fast they fall.</summary>
        public float DropsPerSecond = 70f, Speed = 9f, Spread = 18f, Gravity = 20f;
        /// <summary>Damage each gobbet soaks in: a rifleman melts in about six.</summary>
        public float Damage = 0.5f, Push = 0.35f;
        /// <summary>How long the sludge stays, and how often it burns a goon standing in it (a point each time).</summary>
        public float SludgeLife = 15f, BurnInterval = 0.4f;
    }
}
