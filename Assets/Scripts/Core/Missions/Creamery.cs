using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Mission two's hazards, the Brain Freeze creamery's (<c>docs/design/levels.md</c>, Mission two): the numbers and
    /// the rules of the conveyor belts (the map's '&gt;' and '&lt;', <see cref="Level.BeltSpeedUnder"/>), the nitrogen
    /// vents and tanks that freeze soldiers and goons alike (<see cref="Frost"/>), the scoop droppers overhead, the
    /// boxes of cones and the Trollogram projectors (<see cref="SwitchedOff"/>). The pieces that draw them are in <c>Missions/Two/</c>. Their timing comes from the world's
    /// clock, so they pulse together closely enough on each phone in co-op.
    /// </summary>
    public static class Creamery
    {
        /// <summary>How fast a belt carries whatever stands on it, tiles a second: a soldier still walks against it, slowly.</summary>
        public const float BeltSpeed = 2.4f;

        /// <summary>A nitrogen vent blasts for <see cref="VentFor"/> of every <see cref="VentCycle"/> seconds, over this column.</summary>
        public const float VentCycle = 3.4f, VentFor = 1.1f, VentTell = 0.6f, VentHalfWidth = 0.45f, VentHeight = 2.8f;
        /// <summary>How long the cold holds a soldier, and a goon (longer: they're shot while they're stuck); then a grace.</summary>
        public const float SoldierFreeze = 1.1f, GoonFreeze = 2.6f, ThawGrace = 1.4f;

        /// <summary>A scoop dropper lets one go every <see cref="ScoopCycle"/> seconds, shaking for <see cref="ScoopTell"/> first.</summary>
        public const float ScoopCycle = 3f, ScoopTell = 0.8f;
        public const int ScoopDamage = 1;

        /// <summary>A nitrogen tank's blast: a little damage, and a wide freeze.</summary>
        public const float TankRadius = 2.6f, TankSoldierFreeze = 1.2f, TankGoonFreeze = 3.5f;
        public const int TankDamage = 1, TankGoonDamage = 3;

        /// <summary>A Trollogram projector, shot out, switches off the goons within this reach (and its health).</summary>
        public const float ProjectorReach = 8f;

        /// <summary>The goons a projector casts: every Trollogram goon, but not the Big Scoop, who's too big for it.</summary>
        public static bool Projects(EnemyBrain brain) =>
            brain == EnemyBrain.Rifleman || brain == EnemyBrain.Runner || brain == EnemyBrain.Drone ||
            brain == EnemyBrain.Shield || brain == EnemyBrain.Grenadier;

        /// <summary>
        /// Whether a goon at <paramref name="x"/> is cast by a projector that's been shot out (at one of
        /// <paramref name="dark"/>): it's switched off, so it never appears, and any already up flicker out.
        /// </summary>
        public static bool SwitchedOff(EnemyBrain brain, float x, System.Collections.Generic.IReadOnlyList<float> dark)
        {
            if (!Projects(brain)) return false;
            for (int i = 0; i < dark.Count; i++)
                if (Mathf.Abs(x - dark[i]) <= ProjectorReach) return true;
            return false;
        }

        /// <summary>What a box of cones drops when it's broken, by weight: mostly a medkit, sometimes bombs.</summary>
        public static readonly (PickupDef pickup, int weight)[] ConeBoxDrops = { (Content.Medkit, 2), (Content.BombCrate, 1) };

        /// <summary>A nitrogen vent's beat, and a scoop dropper's (each keeps its own, by where it stands).</summary>
        public static readonly HazardBeat Vent = new HazardBeat(VentCycle, VentFor, VentTell), Dropper = new HazardBeat(ScoopCycle, 0f, ScoopTell);

        /// <summary>The vent at <paramref name="x"/> is blasting.</summary>
        public static bool VentOn(float elapsed, float x) => Vent.On(elapsed, x);

        /// <summary>The vent at <paramref name="x"/> is about to blast: it hisses first, the tell.</summary>
        public static bool VentSoon(float elapsed, float x) => Vent.Soon(elapsed, x);

        /// <summary>The dropper at <paramref name="x"/> is shaking: a scoop's about to fall.</summary>
        public static bool ScoopTelling(float elapsed, float x) => Dropper.Soon(elapsed, x);

        /// <summary>The dropper at <paramref name="x"/> let a scoop go in the <paramref name="dt"/> seconds up to <paramref name="elapsed"/>.</summary>
        public static bool ScoopDrops(float elapsed, float dt, float x) => Dropper.Went(elapsed, dt, x);

        /// <summary>What a broken box of cones drops (gameplay randomness: the world's seeded <see cref="Rng"/>).</summary>
        public static PickupDef ConeBoxDrop(Rng rng)
        {
            int total = 0;
            foreach (var d in ConeBoxDrops) total += d.weight;
            int roll = rng.Range(0, total);
            foreach (var d in ConeBoxDrops)
            {
                if (roll < d.weight) return d.pickup;
                roll -= d.weight;
            }
            return ConeBoxDrops[0].pickup;
        }
    }
}
