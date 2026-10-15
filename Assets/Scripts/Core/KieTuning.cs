using UnityEngine;

namespace PooketSlug
{
    /// <summary>kie's kit as numbers (<c>docs/design/kits.md</c>): Weasel Pop, Trollogram and Steal.</summary>
    public static class KieTuning
    {
        public static readonly WeaselPopSpec Weasels = new WeaselPopSpec();
        public static readonly TrollogramSpec Trollogram = new TrollogramSpec();
        public static readonly StealSpec Steal = new StealSpec();
        public static readonly KiePilotSpec Pilot = new KiePilotSpec();
    }

    /// <summary>
    /// Hologram kie, the pilot who jumps out of his wrecked tank (<c>docs/design/tanks.md</c>, The pilot round): his kit
    /// in the holograms' hands, a little softer than a player's so a duel stays readable. The brain is
    /// <see cref="PilotBrain"/> with <see cref="PilotPlaybook.Kie"/>.
    /// </summary>
    public sealed class KiePilotSpec
    {
        /// <summary>
        /// Its health: a boss duel of 30 to 45 seconds. "Three times a soldier's" in feel; a soldier's 5 hearts take
        /// whole hits, while a pistol lands a point every 0.18 s, so the pilot's bar is counted in shots.
        /// </summary>
        public int Hp = 40;
        /// <summary>How fast it runs and how high it jumps, next to kie himself.</summary>
        public float RunScale = 0.85f, JumpScale = 1f;
        /// <summary>Weasel Pop: weasels a press, seconds between presses, the most it has out at once.</summary>
        public int WeaselsPerThrow = 2;
        public float WeaselCooldown = 1f;
        public int WeaselsOut = 4;
        /// <summary>Trollogram: charges, and seconds for a spent one to come back.</summary>
        public int Specials = 2;
        public float SpecialRefill = 9f;
        /// <summary>Its hologram's blast when you shoot it (on you, not on goons): reach, damage and size.</summary>
        public float DecoyBlastRadius = 2f, DecoyBlastSize = 1.1f;
        public int DecoyDamage = 1;
        /// <summary>Steal charges from its hits on you, not from kills (a duel has nothing else to kill): this many hearts.</summary>
        public int SuperHits = 3;
        /// <summary>Seconds it's untouchable after a swap, glitching into place.</summary>
        public float SwapShield = 0.3f;
        /// <summary>How hard it jumps out of the wreck, and how long it can't be hurt while it does.</summary>
        public Vector2 JumpOut = new Vector2(-3f, 15f);
        public float JumpOutShield = 1f;

        /// <summary>Its super's meter after the soldiers lost <paramref name="hearts"/> to it, from <paramref name="meter"/>.</summary>
        public int ChargeSuper(int meter, int hearts) => Mathf.Min(SuperHits, meter + Mathf.Max(0, hearts));
        public bool SuperReady(int meter) => meter >= SuperHits;
    }

    /// <summary>What kie's Steal can lift off a goon: a burst of its own weapon, or a boss tank's cannon.</summary>
    public enum StealPrize { Nothing, Rifle, Knives, Bombs, Cannon }

    /// <summary>
    /// kie's super, Steal, as rules. SU spins a roulette over the toughest enemy on screen, and kie lifts a power-up
    /// off it: the tougher it is, the bigger the prize. A goon gives a burst of its own weapon (a rifleman's rifle,
    /// a knife runner's knives, a drone's bombs); a boss tank gives its whole cannon. The prize takes the primary's
    /// place, like a crate's gun, until its ammo runs out. With nothing on screen to rob, the super isn't spent.
    /// </summary>
    public sealed class StealSpec
    {
        /// <summary>Seconds the roulette spins before it lands.</summary>
        public float SpinTime = 1.2f;

        /// <summary>The prize a <paramref name="brain"/> gives (a hologram copy of a tank, or a barrel, gives nothing).</summary>
        public static StealPrize PrizeFor(EnemyBrain brain)
        {
            switch (brain)
            {
                case EnemyBrain.Rifleman: return StealPrize.Rifle;
                case EnemyBrain.Weasel: return StealPrize.Rifle; // a weasel has nothing; you get a rifle off its handler
                case EnemyBrain.Runner: return StealPrize.Knives;
                case EnemyBrain.Drone: return StealPrize.Bombs;
                case EnemyBrain.Tank: return StealPrize.Cannon;
                default: return StealPrize.Nothing;
            }
        }

        /// <summary>Whether a <paramref name="brain"/> can be robbed at all.</summary>
        public static bool Robbable(EnemyBrain brain) => PrizeFor(brain) != StealPrize.Nothing;

        /// <summary>
        /// Which of the candidates to rob (index, or -1 for none): the toughest by full health, the nearest of equals.
        /// </summary>
        public static int Toughest(int[] maxHp, float[] distance)
        {
            int best = -1;
            for (int i = 0; i < maxHp.Length; i++)
                if (best < 0 || maxHp[i] > maxHp[best] || (maxHp[i] == maxHp[best] && distance[i] < distance[best])) best = i;
            return best;
        }
    }

    /// <summary>
    /// kie's special, Trollogram, as numbers and rules. A tap of SP drops a hologram of kie where he stands (a charge),
    /// up to <see cref="MaxUp"/> at once, and every goon shoots a hologram instead of him while one's on screen (the
    /// nearest one to it). Holding SP swaps him with the hologram furthest from him (free), and so does a tap with no
    /// charges left. The first thing that hits one blows it up on every goon nearby. These are the ones that went rogue.
    /// (Several at once, and always the goons' target on screen: owner, 2026-10-15.)
    /// </summary>
    public sealed class TrollogramSpec
    {
        /// <summary>Seconds a hologram stands before it switches itself off.</summary>
        public float Life = 12f;
        /// <summary>Seconds between swaps, so a held or doubled tap doesn't swap straight back.</summary>
        public float SwapCooldown = 0.35f;
        /// <summary>Holograms kie can have up at once, and how long SP is held (with one up) before it's a swap, not a drop.</summary>
        public int MaxUp = 3;
        public float HoldToSwap = 0.3f;
        /// <summary>Seconds kie can't be hurt after a swap, while he glitches into place.</summary>
        public float SwapShield = 0.3f;
        /// <summary>The blast when it's shot: its reach, damage and size on screen.</summary>
        public float BlastRadius = 2.2f, BlastSize = 1.2f;
        public int Damage = 4;

        /// <summary>What SP does: swap with a hologram that's up (once the cooldown allows), or drop one with a charge.</summary>
        public TrollAction OnSpecial(bool decoyUp, float sinceSwap, int charges)
        {
            if (decoyUp) return sinceSwap >= SwapCooldown ? TrollAction.Swap : TrollAction.Nothing;
            return charges > 0 ? TrollAction.Drop : TrollAction.Nothing;
        }

        /// <summary>
        /// kie's SP pressed: with no hologram up, drop one at once (with a charge); with one up, wait to see if it's a
        /// tap or a hold (<see cref="TrollAction.Wait"/>).
        /// </summary>
        public TrollAction OnPress(int up, int charges) => up > 0 ? TrollAction.Wait : charges > 0 ? TrollAction.Drop : TrollAction.Nothing;

        /// <summary>A tap of SP with <paramref name="up"/> holograms up: another (a charge, room for it), or else a swap.</summary>
        public TrollAction OnTap(int up, int charges, float sinceSwap)
        {
            if (charges > 0 && up < MaxUp) return TrollAction.Drop;
            return up > 0 && sinceSwap >= SwapCooldown ? TrollAction.Swap : TrollAction.Nothing;
        }

        /// <summary>SP held this long, with a hologram up: a swap.</summary>
        public bool HoldSwaps(float held) => held >= HoldToSwap;

        /// <summary>Whether goons take the hologram for kie: it's up, and on screen (between <paramref name="left"/> and <paramref name="right"/>).</summary>
        public static bool Draws(bool up, float x, float left, float right) => up && x >= left && x <= right;
    }

    /// <summary>What a press of SP does with Trollogram (<see cref="TrollogramSpec.OnSpecial"/>).</summary>
    public enum TrollAction { Nothing, Drop, Swap, Wait }

    /// <summary>
    /// kie's primary, Weasel Pop, as numbers and rules. Each press tumbles out three weasels, lobbed along the aim
    /// (so they reach over cover). Where each lands it scurries under the nearest goon on the ground within reach
    /// and pops, to Pop Goes the Weasel; one with nothing to hunt pops when its fuse runs out. Flyers are out of
    /// reach: the weasels only hunt along the ground, and the pop is small.
    /// </summary>
    public sealed class WeaselPopSpec
    {
        /// <summary>Weasels a press tumbles out, and degrees between neighbours (Tabks' ±4°, opened up a little).</summary>
        public int Count = 3;
        public float SpreadDegrees = 7f;
        /// <summary>The throw: speed along the aim, plus a lift upward so a level aim lobs, and how much of kie's own speed it keeps.</summary>
        public float ThrowSpeed = 8.5f, Lift = 6.5f, Carry = 0.3f;
        /// <summary>Each weasel after the first is thrown this much softer, so a volley lands spread out.</summary>
        public float Soften = 0.12f;
        /// <summary>The weasels' own gravity while tumbling, floatier than a soldier's.</summary>
        public float Gravity = 26f;
        /// <summary>Seconds between presses, and the most weasels kie can have out at once.</summary>
        public float Cooldown = 0.5f;
        public int MaxOut = 9;
        /// <summary>How fast a landed weasel scurries, and how far ahead and up or down it will hunt a goon.</summary>
        public float ScurrySpeed = 6.5f, HuntRange = 9f, HuntHeight = 2.5f;
        /// <summary>Seconds after landing before a weasel pops anyway, and in the air before it gives up tumbling.</summary>
        public float Fuse = 2.5f, AirTime = 3f;
        /// <summary>How close under a goon (from its middle) a weasel gets before it pops.</summary>
        public float Reach = 0.35f;
        /// <summary>The pop: its reach, damage, and size on screen.</summary>
        public float BlastRadius = 1.1f, BlastSize = 0.55f;
        public int Damage = 2;
        /// <summary>Seconds that the tune, once played, waits before another pop plays it again (plain pops in between).</summary>
        public float TuneGap = 1.6f;

        /// <summary>
        /// The throws of one press, from <paramref name="aim"/> (a unit vector) by a thrower moving at
        /// <paramref name="carry"/>: each weasel turned a little off the aim, softer one by one, plus a lift.
        /// </summary>
        public Vector2[] Volley(Vector2 aim, Vector2 carry)
        {
            var throws = new Vector2[Count];
            for (int i = 0; i < Count; i++)
            {
                float turn = (i - (Count - 1) * 0.5f) * SpreadDegrees * Mathf.Deg2Rad;
                float cos = Mathf.Cos(turn), sin = Mathf.Sin(turn);
                var dir = new Vector2(aim.x * cos - aim.y * sin, aim.x * sin + aim.y * cos);
                float speed = ThrowSpeed * (1f - Soften * i);
                throws[i] = dir * speed + new Vector2(carry.x * Carry, Lift);
            }
            return throws;
        }

        /// <summary>
        /// Whether a weasel at <paramref name="weasel"/> (its feet) will hunt a goon whose feet are at
        /// <paramref name="goon"/>: on the ground, in reach along it, and not much higher or lower.
        /// </summary>
        public bool CanHunt(Vector2 weasel, Vector2 goon, bool flies) =>
            !flies && Mathf.Abs(goon.x - weasel.x) <= HuntRange && Mathf.Abs(goon.y - weasel.y) <= HuntHeight;

        /// <summary>
        /// Weasels only work on screen: one whose middle leaves it (either side) is gone, quietly, and only goons on
        /// screen are hunted, so nothing ahead is blown up before you get there (owner, 2026-10-15).
        /// </summary>
        public static bool OnScreen(float x, float left, float right) => x >= left && x <= right;

        /// <summary>Close enough under the goon (its middle at <paramref name="goonX"/>) to pop.</summary>
        public bool Under(float weaselX, float goonX, float goonHalfWidth) => Mathf.Abs(goonX - weaselX) <= Mathf.Max(Reach, goonHalfWidth * 0.6f);
    }
}
