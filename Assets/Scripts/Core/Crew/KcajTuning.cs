using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// kcaj's kit as numbers and rules (<c>docs/design/kits.md</c>): Double Park (his primary), Hyperfixate (his special)
    /// and Unmedicated (his super). The kit that plays them is <c>KcajKit</c>.
    /// </summary>
    public static class KcajTuning
    {
        public static readonly DoubleParkSpec Cones = new DoubleParkSpec();
        public static readonly HyperfixateSpec Beam = new HyperfixateSpec();
        public static readonly UnmedicatedSpec Pills = new UnmedicatedSpec();
        /// <summary>His tabk's guns, SOFT SERVE's (<see cref="KcajTabkSpec"/>).</summary>
        public static readonly KcajTabkSpec Tabk = new KcajTabkSpec();
        /// <summary>Trollogram kcaj, Mission two's pilot, playing the same kit against the soldiers.</summary>
        public static readonly KcajPilotSpec Pilot = new KcajPilotSpec();
    }

    /// <summary>
    /// Trollogram kcaj, who jumps out of his wrecked tabk in Mission two to fight on foot (<c>docs/design/tanks.md</c>,
    /// The pilot round): kcaj's kit on the Trollograms' side, played by the pilot brain with kcaj's playbook. Double
    /// Park lobs cones at the soldiers; Hyperfixate sticks to one of them from range and burns while he stands still,
    /// so hurting him hard enough breaks it; Unmedicated rains pills on the soldiers, charged by his hits on them.
    /// </summary>
    public sealed class KcajPilotSpec
    {
        /// <summary>Its health: kie's pilot's, a boss duel of 30 to 45 seconds.</summary>
        public int Hp = 40;
        /// <summary>How fast it runs and how high it jumps, next to kcaj himself.</summary>
        public float RunScale = 0.85f, JumpScale = 1f;
        /// <summary>Double Park: seconds between presses (slower than kcaj's own), and each cone's blast on a soldier.</summary>
        public float ConeCooldown = 1.1f;
        public int ConeDamage = 1;
        /// <summary>Hyperfixate: charges, and seconds for a spent one to come back.</summary>
        public int Specials = 2;
        public float SpecialRefill = 8f;
        /// <summary>
        /// His beam on a soldier: a little shorter and much cooler than kcaj's own, counted in hearts. Held to the end it
        /// burns about two hearts.
        /// </summary>
        public readonly HyperfixateSpec Beam = new HyperfixateSpec
        {
            Range = 10f, BreakRange = 12f, TickInterval = 0.5f, FirstTick = 0.2f, HottestTick = 0.6f, RampTime = 2f,
            MaxHold = 2.5f, WalkScale = 0f,
        };
        /// <summary>Hurting him this much while his beam's stuck breaks it: he's standing still, so shoot him.</summary>
        public int BreakOnHurt = 6;
        /// <summary>Unmedicated: fewer, bigger pills than kcaj's own, each a heart a direct pop on a soldier.</summary>
        public readonly UnmedicatedSpec Rain = new UnmedicatedSpec
        {
            Count = 20, RainTime = 1.8f, MinFall = 1f, MaxFall = 3f, MaxDrift = 1f, Landings = 2,
            Radius = 0.2f, PopRadius = 0.5f, PopDamage = 1f, HurtsSoldiers = true,
        };
        /// <summary>Unmedicated charges from his hits on the soldiers, not from kills: this many hearts.</summary>
        public int SuperHits = 3;
        /// <summary>How hard it jumps out of the wreck, and how long it can't be hurt while it does.</summary>
        public Vector2 JumpOut = new Vector2(-3f, 15f);
        public float JumpOutShield = 1f;

        /// <summary>Its super's meter after the soldiers lost <paramref name="hearts"/> to it, from <paramref name="meter"/>.</summary>
        public int ChargeSuper(int meter, int hearts) => Mathf.Min(SuperHits, meter + Mathf.Max(0, hearts));
        public bool SuperReady(int meter) => meter >= SuperHits;

        /// <summary>
        /// The beam's burns land in hearts: <paramref name="burn"/> joins what's <paramref name="carried"/> over, and
        /// the whole hearts come out now (the rest carries on to the next tick).
        /// </summary>
        public static int Hearts(ref float carried, float burn)
        {
            carried += Mathf.Max(0f, burn);
            int hearts = Mathf.FloorToInt(carried + 1e-4f);
            carried -= hearts;
            return hearts;
        }

        /// <summary>His beam breaks: he's taken <paramref name="hurt"/> since it stuck.</summary>
        public bool Breaks(int hurt) => hurt >= BreakOnHurt;
    }

    /// <summary>
    /// kcaj's primary, Double Park: each press lobs two ice cream cones like a grenade launcher (the owner's note, 18
    /// Oct), one either side of the aim. A cone goes up at <see cref="Lift"/> degrees above the aim, so aiming ahead
    /// throws an arc; the two part by <see cref="Spread"/> either side of it, so one lands short and one long. Aiming
    /// up throws them almost straight up, and aiming down in the air throws them down and ahead.
    /// </summary>
    public sealed class DoubleParkSpec
    {
        /// <summary>How fast a cone leaves the hand, and how hard it falls.</summary>
        public float Speed = 12f, Gravity = 20f;
        /// <summary>Degrees the throw goes up above the aim, and each cone's degrees either side of the throw.</summary>
        public float Lift = 35f, Spread = 8f;
        /// <summary>The lowest and highest a throw goes, in degrees from straight ahead (90 is straight up).</summary>
        public float Lowest = -60f, Highest = 82f;
        /// <summary>Seconds between presses, and how much of kcaj's own speed a cone keeps.</summary>
        public float Cooldown = 0.5f, Carry = 0.3f;
        /// <summary>Cones in one press.</summary>
        public int Count = 2;

        /// <summary>The throw's angle, in degrees from straight ahead, for an aim (any length) and the way kcaj faces.</summary>
        public float Throw(Vector2 aim, int facing)
        {
            float ahead = aim.x * (facing >= 0 ? 1f : -1f);
            float angle = aim.sqrMagnitude > 0f ? Mathf.Atan2(aim.y, Mathf.Max(0f, ahead)) * Mathf.Rad2Deg : 0f;
            return Mathf.Clamp(angle + Lift, Lowest, Highest);
        }

        /// <summary>
        /// Cone <paramref name="index"/>'s velocity (0 the low one, 1 the high one), thrown along <paramref name="aim"/>
        /// facing <paramref name="facing"/>, with <see cref="Carry"/> of <paramref name="thrower"/>'s speed.
        /// </summary>
        public Vector2 Velocity(int index, Vector2 aim, int facing, Vector2 thrower)
        {
            float side = Count > 1 ? Mathf.Lerp(-1f, 1f, index / (float)(Count - 1)) : 0f;
            float a = (Throw(aim, facing) + side * Spread) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(a) * (facing >= 0 ? 1f : -1f), Mathf.Sin(a)) * Speed + thrower * Carry;
        }

        /// <summary>How far ahead a cone thrown at <paramref name="degrees"/> lands, back at the height it left from (on flat ground, standing still).</summary>
        public float Reach(float degrees) => Speed * Speed * Mathf.Sin(2f * degrees * Mathf.Deg2Rad) / Gravity;
    }

    /// <summary>
    /// kcaj's special, Hyperfixate. Pressing SP spends a charge and throws a beam along the aim; it sticks to the first
    /// thing it touches (a wall stops it, and a beam that finds nothing fizzles and gives the charge back). While SP is
    /// held the beam follows its target and burns in ticks that grow the longer it stays on (owner's note: longer
    /// fixate, higher damage ticks), and the screen edges dim. Letting go, the target dying, or the target getting out
    /// of reach ends it, and so does running out of focus (<see cref="MaxHold"/>). While fixated kcaj only shuffles
    /// and can't jump or fire anything else: anything else is free to shoot him.
    /// </summary>
    public sealed class HyperfixateSpec
    {
        /// <summary>How far the beam reaches to find something to stick to: most of a screen.</summary>
        public float Range = 11f;
        /// <summary>Once stuck, the target can get this far away (it's a fixation: walls don't break it, distance does).</summary>
        public float BreakRange = 14f;
        /// <summary>Seconds between burns.</summary>
        public float TickInterval = 0.2f;
        /// <summary>The first burn and the hottest one, and the seconds it takes to get from one to the other.</summary>
        public float FirstTick = 0.15f, HottestTick = 2f, RampTime = 3f;
        /// <summary>The special's charges, and the seconds each one takes to come back.</summary>
        public int Charges = 3;
        public float RechargeTime = 6f;
        /// <summary>The most seconds one charge holds on for.</summary>
        public float MaxHold = 4f;
        /// <summary>How dark the screen edges get (0 to 1), and how quickly they close in.</summary>
        public float MaxDim = 0.65f, DimTime = 1.2f;
        /// <summary>kcaj's speed while fixated, next to his run.</summary>
        public float WalkScale = 0.35f;

        /// <summary>The burn of the tick that lands <paramref name="held"/> seconds into a fixation.</summary>
        public float TickDamage(float held) => Mathf.Lerp(FirstTick, HottestTick, Mathf.Clamp01(held / RampTime));

        /// <summary>How dark the screen edges are <paramref name="held"/> seconds in (0 when not fixated).</summary>
        public float Dim(float held)
        {
            if (held <= 0f) return 0f;
            float t = Mathf.Clamp01(held / DimTime);
            return MaxDim * t * t * (3f - 2f * t);
        }

        /// <summary>Everything one fixation burns if it's held for <paramref name="held"/> seconds (ticks land at the end of each interval).</summary>
        public float TotalDamage(float held)
        {
            float sum = 0f;
            held = Mathf.Min(held, MaxHold);
            for (int i = 1; i * TickInterval <= held + 1e-4f; i++) sum += TickDamage(i * TickInterval);
            return sum;
        }

        /// <summary>
        /// What the beam sticks to: the nearest of the boxes (centre and half size) that the ray from
        /// <paramref name="origin"/> along <paramref name="dir"/> touches within <see cref="Range"/> and before
        /// <paramref name="wall"/> (the distance to the first solid tile along the ray). Returns its index, or -1.
        /// </summary>
        public int FirstTouched(Vector2 origin, Vector2 dir, Vector2[] centres, Vector2[] halves, int count, float wall)
        {
            float reach = Mathf.Min(Range, wall), best = float.MaxValue;
            int hit = -1;
            for (int i = 0; i < count; i++)
            {
                float d = RayBox(origin, dir, centres[i], halves[i]);
                if (d >= 0f && d <= reach && d < best) { best = d; hit = i; }
            }
            return hit;
        }

        /// <summary>The distance along the ray to the box, 0 when it starts inside, or -1 if it misses.</summary>
        public static float RayBox(Vector2 origin, Vector2 dir, Vector2 centre, Vector2 half)
        {
            float tMin = 0f, tMax = float.MaxValue;
            for (int axis = 0; axis < 2; axis++)
            {
                float o = axis == 0 ? origin.x : origin.y, d = axis == 0 ? dir.x : dir.y;
                float lo = (axis == 0 ? centre.x - half.x : centre.y - half.y), hi = (axis == 0 ? centre.x + half.x : centre.y + half.y);
                if (Mathf.Abs(d) < 1e-6f)
                {
                    if (o < lo || o > hi) return -1f;
                    continue;
                }
                float a = (lo - o) / d, b = (hi - o) / d;
                if (a > b) { float s = a; a = b; b = s; }
                tMin = Mathf.Max(tMin, a);
                tMax = Mathf.Min(tMax, b);
                if (tMin > tMax) return -1f;
            }
            return tMin;
        }
    }

    /// <summary>
    /// One Hyperfixate, from SP down to the beam letting go: what the soldier and the screen ask each frame. Pure, so
    /// a co-op partner's screen can run the same one from the same presses and target.
    /// </summary>
    public sealed class Hyperfixation
    {
        readonly HyperfixateSpec spec;
        float sinceTick;

        public Hyperfixation(HyperfixateSpec spec) { this.spec = spec; }

        /// <summary>Is the beam stuck on something.</summary>
        public bool Locked { get; private set; }
        /// <summary>Seconds it has been stuck.</summary>
        public float Held { get; private set; }
        /// <summary>How the screen edges are dimmed now.</summary>
        public float Dim => Locked ? spec.Dim(Held) : 0f;
        /// <summary>Why the last fixation ended.</summary>
        public FixateEnd Ended { get; private set; }

        /// <summary>The beam found something: start burning it.</summary>
        public void Lock()
        {
            Locked = true;
            Held = 0f;
            sinceTick = 0f;
            Ended = FixateEnd.None;
        }

        /// <summary>
        /// A frame of <paramref name="dt"/> seconds with SP <paramref name="held"/>, the target
        /// <paramref name="alive"/> and <paramref name="distance"/> away. Returns the burn that lands this frame.
        /// </summary>
        public float Step(float dt, bool held, bool alive, float distance)
        {
            if (!Locked) return 0f;
            if (!held) { Stop(FixateEnd.LetGo); return 0f; }
            if (!alive) { Stop(FixateEnd.TargetGone); return 0f; }
            if (distance > spec.BreakRange) { Stop(FixateEnd.OutOfReach); return 0f; }
            float burn = 0f;
            float left = dt;
            while (left > 0f)
            {
                float step = Mathf.Min(left, spec.TickInterval - sinceTick);
                step = Mathf.Min(step, spec.MaxHold - Held);
                if (step <= 0f) break;
                Held += step;
                sinceTick += step;
                left -= step;
                if (sinceTick >= spec.TickInterval - 1e-5f)
                {
                    sinceTick = 0f;
                    burn += spec.TickDamage(Held);
                }
            }
            if (Held >= spec.MaxHold - 1e-5f) Stop(FixateEnd.Spent);
            return burn;
        }

        void Stop(FixateEnd why)
        {
            Locked = false;
            Ended = why;
        }
    }

    public enum FixateEnd { None, LetGo, TargetGone, OutOfReach, Spent }

    /// <summary>
    /// kcaj's super, Unmedicated: <see cref="Count"/> pills rain across the whole screen over
    /// <see cref="RainTime"/>, each bouncing twice on the ground and popping on the third landing (or on a goon, or
    /// when it's fallen out of the level). A pop hurts goons nearby and never kcaj or a partner. It's a solo super, so
    /// it isn't a screen clearer: on a screen of riflemen some are left standing (<c>docs/design/team-supers.md</c>).
    /// A rain is laid out from a seed (<see cref="PillRain.Start"/>), so a partner's screen copies it from the seed alone.
    /// </summary>
    public sealed class UnmedicatedSpec
    {
        public int Count = 120;
        /// <summary>Seconds over which the pills start falling.</summary>
        public float RainTime = 1.4f;
        /// <summary>Where they start, above the screen's top, and how fast they're going.</summary>
        public float StartAbove = 1f, MinFall = 2f, MaxFall = 7f, MaxDrift = 1.6f;
        public float Gravity = 26f;
        /// <summary>How much of its speed a pill keeps on a bounce, up and along.</summary>
        public float Bounce = 0.6f, Skid = 0.85f;
        /// <summary>Landings before it pops: it bounces twice, then pops on the third.</summary>
        public int Landings = 3;
        /// <summary>A pill's size, and its pop's reach and damage on goons.</summary>
        public float Radius = 0.14f, PopRadius = 0.8f, PopDamage = 0.25f;
        /// <summary>A pill that hasn't popped by now (it fell into a pit) pops anyway.</summary>
        public float MaxLife = 5f;
        /// <summary>Pops never hurt kcaj or a co-op partner.</summary>
        public bool HurtsSoldiers = false;
    }

    /// <summary>What a level tells the pills: is this point solid ground.</summary>
    public interface IPillGround
    {
        bool Solid(float x, float y);
    }

    /// <summary>
    /// The pills of one Unmedicated, moved by hand in fixed arrays: a phone has to carry 120 of them at once, so no
    /// physics bodies, no allocation per frame, and nothing per pill but numbers. Each frame reports the pops; the
    /// caller hurts goons with them.
    /// </summary>
    public sealed class PillRain
    {
        readonly UnmedicatedSpec spec;
        readonly float[] x, y, vx, vy, startAt, age;
        readonly byte[] landed;
        readonly bool[] live, popped;
        float clock;
        int spawned;

        /// <summary>Where this frame's pops went off; <see cref="PopCount"/> of them are set.</summary>
        public readonly Vector2[] Pops;
        public int PopCount { get; private set; }

        public PillRain(UnmedicatedSpec spec)
        {
            this.spec = spec;
            int n = spec.Count;
            x = new float[n]; y = new float[n]; vx = new float[n]; vy = new float[n];
            startAt = new float[n]; age = new float[n];
            landed = new byte[n];
            live = new bool[n]; popped = new bool[n];
            Pops = new Vector2[n];
        }

        public int Capacity => x.Length;
        /// <summary>Pills in the air or bouncing now.</summary>
        public int Live { get; private set; }
        /// <summary>Every pill has fallen and popped.</summary>
        public bool Done => spawned == Capacity && Live == 0;
        public Vector2 Position(int i) => new Vector2(x[i], y[i]);
        public bool IsLive(int i) => live[i];
        public int Landed(int i) => landed[i];

        /// <summary>A rain's seed, drawn from the game's <paramref name="rng"/>: never 0, and small enough to cross the network in a float.</summary>
        public static uint Seed(Rng rng) => 1u + (rng.NextUInt() & 0xFFFFFEu);

        /// <summary>Starts a rain across the screen from <paramref name="left"/> to <paramref name="right"/>, falling from above <paramref name="top"/>.</summary>
        public void Start(uint seed, float left, float right, float top) => Start(new Rng(seed), left, right, top);

        /// <summary>Starts a rain across the screen from <paramref name="left"/> to <paramref name="right"/>, falling from above <paramref name="top"/>.</summary>
        public void Start(Rng rng, float left, float right, float top)
        {
            clock = 0f;
            spawned = 0;
            Live = 0;
            PopCount = 0;
            int n = Capacity;
            for (int i = 0; i < n; i++)
            {
                // Spread evenly across the screen, a pill a slot, jittered inside its slot, in a shuffled order.
                float slot = (right - left) / n;
                x[i] = left + slot * (i + rng.Value);
                y[i] = top + spec.StartAbove + rng.Range(0f, 1.5f);
                vx[i] = rng.Range(-spec.MaxDrift, spec.MaxDrift);
                vy[i] = -rng.Range(spec.MinFall, spec.MaxFall);
                startAt[i] = rng.Range(0f, spec.RainTime);
                age[i] = 0f;
                landed[i] = 0;
                live[i] = false;
                popped[i] = false;
            }
        }

        /// <summary>
        /// Moves every pill <paramref name="dt"/> seconds. Pills that land a third time, touch a goon
        /// (<paramref name="goon"/>, may be null) or run out of life pop, and their places are in <see cref="Pops"/>.
        /// </summary>
        public void Step(float dt, IPillGround ground, System.Func<float, float, bool> goon)
        {
            clock += dt;
            PopCount = 0;
            int n = Capacity;
            for (int i = 0; i < n; i++)
            {
                if (popped[i]) continue;
                if (!live[i])
                {
                    if (clock < startAt[i]) continue;
                    live[i] = true;
                    spawned++;
                    Live++;
                }
                age[i] += dt;
                vy[i] -= spec.Gravity * dt;
                float nx = x[i] + vx[i] * dt;
                if (ground.Solid(nx, y[i])) vx[i] = -vx[i] * spec.Bounce;
                else x[i] = nx;
                float ny = y[i] + vy[i] * dt;
                if (ground.Solid(x[i], ny - spec.Radius))
                {
                    if (vy[i] < 0f)
                    {
                        landed[i]++;
                        vy[i] = -vy[i] * spec.Bounce;
                        vx[i] *= spec.Skid;
                    }
                    else vy[i] = 0f;
                }
                else y[i] = ny;
                if (landed[i] >= spec.Landings || age[i] >= spec.MaxLife || (goon != null && goon(x[i], y[i])))
                {
                    popped[i] = true;
                    live[i] = false;
                    Live--;
                    Pops[PopCount++] = new Vector2(x[i], y[i]);
                }
            }
        }
    }
}
