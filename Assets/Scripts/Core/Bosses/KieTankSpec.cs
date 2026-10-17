using UnityEngine;

namespace PooketSlug
{
    /// <summary>What kie's boss tank does with a charged shot instead of a shell (<see cref="KieTankSpec.AttackFor"/>).</summary>
    public enum KieAttack { Shell, Weasels, Holograms, Roulette }

    /// <summary>What kie's roulette lands on.</summary>
    public enum KieSteal { Nothing, Special, Super }

    /// <summary>
    /// kie's boss tank (<c>docs/design/tanks.md</c>, Tank bosses): Mission one's artillery tank with a three-attack cycle in
    /// place of its plain shells (<see cref="Content.KieTank"/>). Each charged shot is the next of: a wave of weasels lobbed at
    /// you that scurry over and pop; hologram copies of itself, where only the real one can be hurt; and a roulette
    /// that steals your special (or your super's charge) until you hit the tank hard enough to knock it loose. It
    /// still gets quicker as it's hurt (<see cref="BossTankSpec.Rage"/>). The numbers and rules, kept apart from
    /// the tank so the tests can reach them.
    /// </summary>
    public sealed class KieTankSpec
    {
        /// <summary>Weasels in a wave, and how much harder or softer than the middle one each neighbour is thrown.</summary>
        public int WeaselCount = 3;
        public float WeaselSpread = 0.18f;
        /// <summary>The hardest throw (the weasels fall at <see cref="Weasel"/>'s gravity).</summary>
        public float WeaselMaxPower = 18.5f;
        /// <summary>
        /// The weasels it and its pilot throw at the soldiers: kie's own rules (<see cref="WeaselSpec"/>), slower so
        /// they can be shot first (the tank's rage speeds them up), a smaller pop for a soldier's few hits, and they
        /// chase you right across the screen.
        /// </summary>
        public readonly WeaselSpec Weasel = new WeaselSpec { ScurrySpeed = 4.6f, HuntRange = 99f, Damage = 1, BlastRadius = 1f, BlastSize = 0.6f };

        /// <summary>Copies it throws up, how far apart the three tanks stand, and how long the copies last.</summary>
        public int Decoys = 2;
        public float DecoyGap = 5f, DecoyTime = 10f;

        /// <summary>Seconds the roulette spins before it lands.</summary>
        public float SpinTime = 1.8f;
        /// <summary>Damage to knock loose what it stole, as a fraction of its full health.</summary>
        public float DropFraction = 0.1f;

        /// <summary>The roulette, as its tank's network object carries it: spinning, holding, and whether the shot charging lands.</summary>
        public const int RouletteSpinning = 1, RouletteHolding = 2, RoulettePhase = 3, NoMarker = 4;

        /// <summary>
        /// The attack for its <paramref name="turn"/>th charged shot (from 0): weasels, holograms, roulette, round
        /// again. A turn whose attack is already going (copies still up, a steal still held) is a plain shell.
        /// </summary>
        public static KieAttack AttackFor(int turn, bool decoysUp, bool holding)
        {
            switch (((turn % 3) + 3) % 3)
            {
                case 0: return KieAttack.Weasels;
                case 1: return decoysUp ? KieAttack.Shell : KieAttack.Holograms;
                default: return holding ? KieAttack.Shell : KieAttack.Roulette;
            }
        }

        /// <summary>
        /// What the roulette takes: whichever the soldier has, picked by <paramref name="roll"/> (0 to 1) when they
        /// have both; nothing when they have neither.
        /// </summary>
        public static KieSteal Pick(int specials, float superCharge, float roll)
        {
            bool special = specials > 0, super = superCharge > 0f;
            if (special && super) return roll < 0.5f ? KieSteal.Special : KieSteal.Super;
            return special ? KieSteal.Special : super ? KieSteal.Super : KieSteal.Nothing;
        }

        /// <summary>Damage it must take, after a steal, before it drops what it stole.</summary>
        public int DropDamage(int maxHp) => Mathf.Max(1, Mathf.CeilToInt(maxHp * DropFraction));

        /// <summary>Where the three tanks stand for the hologram trick: home, and a gap either side.</summary>
        public float[] Slots(float homeX) => new[] { homeX - DecoyGap, homeX, homeX + DecoyGap };
    }
}
