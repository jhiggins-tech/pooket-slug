using System;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Spike tests: small throwaway experiments switched on from the page address, so they ride along in the
    /// normal build without changing the game anyone else plays. <c>?spike=hits,tank</c> turns on
    /// the hit feel and the artillery boss tank for that visit. Spikes are solo
    /// only: a co-op game ignores them.
    /// </summary>
    public static class Spike
    {
        public const string HitsKey = "hits", TankKey = "tank";

        /// <summary>Hits land with weight: sparks, flashes, and damage numbers floating off you.</summary>
        public static bool Hits;
        /// <summary>The boss tank fights like artillery: it swings its turret and charges up lobbed shots.</summary>
        public static bool Tank;

        public static bool Any => Hits || Tank;

        /// <summary>Reads <c>?spike=</c> from the page address.</summary>
        public static void Configure(string url)
        {
            string[] keys = (PageQuery.Get(url, "spike") ?? "").ToLowerInvariant().Split(',');
            Hits = Array.IndexOf(keys, HitsKey) >= 0;
            Tank = Array.IndexOf(keys, TankKey) >= 0;
        }

        /// <summary>What the start banner says, or null when no spike is on.</summary>
        public static string Label
        {
            get
            {
                if (!Any) return null;
                var parts = new System.Collections.Generic.List<string>();
                if (Hits) parts.Add("HIT FEEL");
                if (Tank) parts.Add("BOSS TANK");
                return "SPIKE TEST · " + string.Join(" + ", parts);
            }
        }
    }

    /// <summary>
    /// The boss tank, fighting like a Pocket Tanks tank (<c>docs/design/tanks.md</c>, Two kinds of tank). It drives
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

        /// <summary>How much quicker it is at <paramref name="health"/> (0 to 1) left.</summary>
        public float Rage(float health) => Mathf.Lerp(MaxRage, 1f, Mathf.Clamp01(health));

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

    /// <summary>The spikes' own content. None of it is in <see cref="Content"/>'s tables, so none of it goes on the wire.</summary>
    public static class SpikeContent
    {
        public static readonly BossTankSpec BossTank = new BossTankSpec();

        // Ids from the top of the range, well away from Content's: these are never sent.
        /// <summary>The boss tank's big shell: a wider blast than the old tank's.</summary>
        public static readonly ProjectileDef BigShell = new ProjectileDef(250, "Big shell", radius: 0.25f, damage: 1, life: 6f, gravity: 18f,
            blastRadius: 1.4f, blastSize: 1.1f);
        public static readonly WeaponDef BossCannon = new WeaponDef(253, "Boss cannon", WeaponBehaviour.Lob, BigShell, muzzleBlast: 0.6f, kick: 0.15f);

    }
}
