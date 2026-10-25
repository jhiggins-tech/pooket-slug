using UnityEngine;

namespace PooketSlug
{
    /// <summary>kie's kit as numbers (<c>docs/design/kits.md</c>): Weasel Pop, Trollogram and Steal.</summary>
    public static class KieTuning
    {
        public static readonly WeaselPopSpec Weasels = new WeaselPopSpec();
        public static readonly TrollogramSpec Trollogram = new TrollogramSpec();
        public static readonly StealSpec Steal = new StealSpec();
        /// <summary>His tabk's guns, THE PROJECTIONIST's.</summary>
        public static readonly KieTabkSpec Tabk = new KieTabkSpec();
        public static readonly KiePilotSpec Pilot = new KiePilotSpec();
        public static readonly PilotFreeingSpec Freeing = new PilotFreeingSpec();
    }

    /// <summary>
    /// Hologram kie beaten, the big glitch-out and the real kie freed (<c>docs/design/cutscenes/mission-one.md</c>, Freeing kie, shots 1
    /// and 2): he freezes mid-taunt and the boss music cuts, he tears apart like
    /// the goons but bigger, a cage of light flickers out where he stood, and the real kie drops to the floor, dazed.
    /// </summary>
    public sealed class PilotFreeingSpec
    {
        /// <summary>Frozen mid-taunt, glitching harder and harder, before he tears apart (seconds).</summary>
        public float Freeze = 1.3f;
        /// <summary>His tear-apart is the goons' this many times as long, its bands thrown this many pixels (the goons': 5).</summary>
        public float Stretch = 2.4f, TearPx = 12f;
        /// <summary>The cage of light flickers this long before it's out and he drops.</summary>
        public float Cage = 1.5f;
        /// <summary>Dazed on the floor this long, saying his line, before the mission's won.</summary>
        public float Dazed = 2.6f;

        /// <summary>The same flicker-out over <paramref name="length"/> seconds (a scene can time its own: <c>0-1.5 cage flicker-out</c>).</summary>
        public static bool CageLitFor(float t, float length)
        {
            if (t < 0f || t >= length) return false;
            float k = t / length;
            if (k < 0.3f) return true;
            int step = (int)(t * Mathf.Lerp(8f, 30f, k));
            return step % (k < 0.7f ? 4 : 2) != 0;
        }
    }

    /// <summary>
    /// Hologram kie, the pilot who jumps out of his wrecked tank (<c>docs/design/tabks/bosses.md</c>, The pilot round): his kit
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
    /// How every roulette of kie's spins, his own (<c>StealRoulette</c>) and his tabk's and pilot's
    /// (<c>KieRoulette</c>), so they slow and settle alike: the faces flick by, slowing, then it settles on what it
    /// lands on for the last quarter and the frame flashes.
    /// </summary>
    public static class RouletteSpin
    {
        /// <summary>How far through the spin (0 to 1) it settles.</summary>
        public const float Settles = 0.75f;
        /// <summary>Face changes over a whole spin, and frame flashes a second once it's settled.</summary>
        const float Flicks = 12f, Flashes = 16f;

        /// <summary>Settled on what it lands on, <paramref name="t"/> (0 to 1) through the spin.</summary>
        public static bool Settled(float t) => t > Settles;

        /// <summary>The face showing <paramref name="t"/> through the spin (before it settles), of <paramref name="faces"/>.</summary>
        public static int Face(float t, int faces) => Step(t) % faces;

        /// <summary>Whether a face changed between <paramref name="before"/> and <paramref name="now"/> (both 0 to 1): it ticks.</summary>
        public static bool Ticked(float before, float now) => Step(before) != Step(now);

        /// <summary>The frame lit, <paramref name="seconds"/> into a spin <paramref name="t"/> through.</summary>
        public static bool Lit(float t, float seconds) => Settled(t) && (int)(seconds * Flashes) % 2 == 0;

        static int Step(float t) => (int)(Mathf.Sqrt(Mathf.Max(0f, t)) * Flicks);
    }

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
    /// nearest one to it). Holding SP swaps him with the hologram furthest from him (free), and so does a tap once
    /// there's no charge or room for another. The first thing that hits one blows it up on every goon nearby. These are the ones that went rogue.
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

    /// <summary>
    /// Co-op: what happened to one of a player's Trollograms, sent to the partner as a kit move
    /// (<see cref="KitMove.Trollogram"/>, the hologram's feet as where, and this packed into its data). Each
    /// screen keeps a copy of the partner's holograms, so the goons shoot them on every screen.
    /// </summary>
    public enum TrollEvent
    {
        None,
        /// <summary>The sender dropped a hologram, or swapped with one (it now stands at the origin).</summary>
        Drop, Moved,
        /// <summary>The sender's hologram blew up, or switched off.</summary>
        Blown, Off,
        /// <summary>A goon hit the receiver's hologram on the sender's screen: blow it up (with its damage) there.</summary>
        HitYours,
    }

    /// <summary>Packs a <see cref="TrollEvent"/> into a shot event's velocity, and back: exact in a float.</summary>
    public static class TrollNet
    {
        /// <param name="tabk">It's a Trollogram of kie's tabk (<see cref="KieTabkSpec.Trollogram"/>), not of him.</param>
        public static Vector2 Pack(TrollEvent e, int id, int facing, bool crouching, bool tabk = false) =>
            new Vector2((int)e, (id & 255) * 8 + (facing > 0 ? 1 : 0) + (crouching ? 2 : 0) + (tabk ? 4 : 0));

        public static TrollEvent Unpack(Vector2 v, out int id, out int facing, out bool crouching) => Unpack(v, out id, out facing, out crouching, out _);

        public static TrollEvent Unpack(Vector2 v, out int id, out int facing, out bool crouching, out bool tabk)
        {
            int bits = Mathf.RoundToInt(v.y);
            id = bits >> 3;
            facing = (bits & 1) != 0 ? 1 : -1;
            crouching = (bits & 2) != 0;
            tabk = (bits & 4) != 0;
            int e = Mathf.RoundToInt(v.x);
            return e > (int)TrollEvent.None && e <= (int)TrollEvent.HitYours ? (TrollEvent)e : TrollEvent.None;
        }
    }

    /// <summary>What a press of SP does with Trollogram (<see cref="TrollogramSpec.OnSpecial"/>).</summary>
    public enum TrollAction { Nothing, Drop, Swap, Wait }

    /// <summary>What a weasel does next (<see cref="WeaselSpec"/>).</summary>
    public enum WeaselMove { Run, Pop, Gone }

    /// <summary>
    /// One weasel's rules, whoever threw it: kie's own (<see cref="WeaselPopSpec"/>), and the ones his tabk and his
    /// pilot throw at the soldiers (<see cref="KieTankSpec.Weasel"/>). Every copy runs these, so they can't drift
    /// apart: it tumbles until it lands and pops on anything it hunts that it touches on the way (or gives up after
    /// <see cref="AirTime"/>), then scurries under the nearest one and pops, or pops when its fuse runs out. Off the
    /// screen it's gone, without a pop.
    /// </summary>
    public class WeaselSpec
    {
        /// <summary>Its own gravity while tumbling, floatier than a soldier's.</summary>
        public float Gravity = 26f;
        /// <summary>How fast a landed weasel scurries, and how far ahead and up or down it will hunt.</summary>
        public float ScurrySpeed = 6.5f, HuntRange = 9f, HuntHeight = 2.5f;
        /// <summary>Seconds after landing before a weasel pops anyway, and in the air before it gives up tumbling.</summary>
        public float Fuse = 2.5f, AirTime = 3f;
        /// <summary>How close under its prey (from the middle) a weasel gets before it pops, and how far up or down that counts.</summary>
        public float Reach = 0.35f, PopHeight = 1.2f;
        /// <summary>The pop: its reach, damage, and size on screen.</summary>
        public float BlastRadius = 1.1f, BlastSize = 0.55f;
        public int Damage = 2;

        /// <summary>
        /// Whether a weasel at <paramref name="weasel"/> (its feet) will hunt something whose feet are at
        /// <paramref name="prey"/>: on the ground, in reach along it, and not much higher or lower.
        /// </summary>
        public bool CanHunt(Vector2 weasel, Vector2 prey, bool flies) =>
            !flies && Mathf.Abs(prey.x - weasel.x) <= HuntRange && Mathf.Abs(prey.y - weasel.y) <= HuntHeight;

        /// <summary>
        /// Weasels only work on screen: one whose middle leaves it (either side) is gone, quietly, and only goons on
        /// screen are hunted, so nothing ahead is blown up before you get there (owner, 2026-10-15).
        /// </summary>
        public static bool OnScreen(float x, float left, float right) => x >= left && x <= right;

        /// <summary>Close enough under the prey (its middle at <paramref name="preyX"/>) to pop.</summary>
        public bool Under(float weaselX, float preyX, float preyHalfWidth) => Mathf.Abs(preyX - weaselX) <= Mathf.Max(Reach, preyHalfWidth * 0.6f);

        /// <summary>Off the screen, or fallen out of the world: gone, without a pop.</summary>
        public static bool Leaves(Vector2 weasel, float left, float right) => weasel.y < -2f || !OnScreen(weasel.x, left, right);

        /// <summary>Tumbling, <paramref name="time"/> seconds since it was thrown: pops on touching its prey, or once it's been up too long.</summary>
        public WeaselMove InAir(float time, bool touchingPrey) => touchingPrey || time >= AirTime ? WeaselMove.Pop : WeaselMove.Run;

        /// <summary>
        /// Landed, <paramref name="fuse"/> seconds ago: pops under its prey (feet at <paramref name="prey"/>, when it
        /// has one), or when the fuse runs out.
        /// </summary>
        public WeaselMove OnGround(float fuse, Vector2 weasel, bool hasPrey, Vector2 prey, float preyHalfWidth)
        {
            if (hasPrey && Under(weasel.x, prey.x, preyHalfWidth) && Mathf.Abs(prey.y - weasel.y) < PopHeight) return WeaselMove.Pop;
            return fuse >= Fuse ? WeaselMove.Pop : WeaselMove.Run;
        }
    }

    /// <summary>
    /// kie's primary, Weasel Pop, as numbers and rules. Each press tumbles out three weasels, lobbed along the aim
    /// (so they reach over cover). Where each lands it scurries under the nearest goon on the ground within reach
    /// and pops, to Pop Goes the Weasel (<see cref="WeaselSpec"/>); one with nothing to hunt pops when its fuse runs
    /// out. Flyers are out of reach: the weasels only hunt along the ground, and the pop is small.
    /// </summary>
    public sealed class WeaselPopSpec : WeaselSpec
    {
        /// <summary>Weasels a press tumbles out, and degrees between neighbours (Tabks' ±4°, opened up a little).</summary>
        public int Count = 3;
        public float SpreadDegrees = 7f;
        /// <summary>The throw: speed along the aim, plus a lift upward so a level aim lobs, and how much of kie's own speed it keeps.</summary>
        public float ThrowSpeed = 8.5f, Lift = 6.5f, Carry = 0.3f;
        /// <summary>Each weasel after the first is thrown this much softer, so a volley lands spread out.</summary>
        public float Soften = 0.12f;
        /// <summary>Seconds between presses, and the most weasels kie can have out at once.</summary>
        public float Cooldown = 0.5f;
        public int MaxOut = 9;
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
    }
}
