using System;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Spike tests: small throwaway experiments switched on from the page address, so they ride along in the
    /// normal build without changing the game anyone else plays. <c>?spike=tones2,holo,hits</c> turns on tones2's
    /// kit, the hologram goons and the hit feel for that visit. Spikes are solo only: a co-op game ignores them.
    /// </summary>
    public static class Spike
    {
        public const string Tones2Key = "tones2", HoloKey = "holo", HitsKey = "hits";

        /// <summary>Play as tones2: the water jet, the mud rocket jump and the spew.</summary>
        public static bool Tones2;
        /// <summary>The goons are glitchy holograms, and glitch out when they die.</summary>
        public static bool Holo;
        /// <summary>Hits land with weight: sparks, flashes, and damage numbers floating off you.</summary>
        public static bool Hits;

        public static bool Any => Tones2 || Holo || Hits;

        /// <summary>Reads <c>?spike=</c> from the page address.</summary>
        public static void Configure(string url)
        {
            string[] keys = (PageQuery.Get(url, "spike") ?? "").ToLowerInvariant().Split(',');
            Tones2 = Array.IndexOf(keys, Tones2Key) >= 0;
            Holo = Array.IndexOf(keys, HoloKey) >= 0;
            Hits = Array.IndexOf(keys, HitsKey) >= 0;
        }

        /// <summary>What the start banner says, or null when no spike is on.</summary>
        public static string Label
        {
            get
            {
                if (!Any) return null;
                var parts = new System.Collections.Generic.List<string>();
                if (Tones2) parts.Add("TONES2");
                if (Holo) parts.Add("HOLOGRAM GOONS");
                if (Hits) parts.Add("HIT FEEL");
                return "SPIKE TEST · " + string.Join(" + ", parts);
            }
        }
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

    /// <summary>The spikes' own content. None of it is in <see cref="Content"/>'s tables, so none of it goes on the wire.</summary>
    public static class SpikeContent
    {
        public static readonly JetSpec Jet = new JetSpec();
        public static readonly RocketSpec Rocket = new RocketSpec();
        public static readonly SpewSpec Spew = new SpewSpec();

        // Ids from the top of the range, well away from Content's: these weapons are never sent.
        public static readonly WeaponDef WaterJet = new WeaponDef(250, "ten-1", WeaponBehaviour.Jet, null, hud: "TEN-1");
        public static readonly WeaponDef RocketJump = new WeaponDef(251, "ten-2", WeaponBehaviour.RocketJump, null, hud: "TEN-2");
        public static readonly WeaponDef SpewWeapon = new WeaponDef(252, "ten-3", WeaponBehaviour.Spew, null, hud: "TEN-3");

        /// <summary>tones2, from Pooket Tabks: the water jet, the mud rocket jump and the spew.</summary>
        public static readonly SoldierDef Tones2 = new SoldierDef(250, "tones2", runSpeed: 5.5f, crawlSpeed: 2f, jumpVelocity: 17f,
            halfWidth: 0.3f, standHeight: 1.35f, crouchHeight: 0.8f, maxHp: 5, primary: WaterJet, special: RocketJump, startSpecials: 3,
            super: SpewWeapon, crateSpecials: 3); // the rocket jump is a strong special: a bomb crate gives about 3
    }
}
