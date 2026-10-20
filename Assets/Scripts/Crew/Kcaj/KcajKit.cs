using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// kcaj's kit (<c>docs/design/kits.md</c>, numbers in <see cref="KcajTuning"/>): FIRE is Double Park (two cones lobbed
    /// either side of the aim, <see cref="DoubleParkSpec"/>), SP is Hyperfixate (<see cref="KcajBeam"/>) and SU is
    /// Unmedicated (<see cref="KcajPills"/>). The cones are shots, so they reach a partner as shots; the beam and the
    /// pills go as kit moves (<see cref="KitMove.Fixate"/>, <see cref="KitMove.Pills"/>) and <see cref="KcajCopy"/>
    /// plays them there.
    /// </summary>
    public sealed class KcajKit : CrewKit
    {
        readonly KcajBeam m_Beam;
        readonly KcajPills m_Pills;

        public KcajKit(Player player) : base(player)
        {
            m_Beam = new KcajBeam(new Soldier(player), KcajTuning.Beam);
            m_Pills = new KcajPills(player.World, KcajTuning.Pills);
        }

        public override bool Fixated => m_Beam.Locked;
        public override float SpeedScale => m_Beam.Locked ? KcajTuning.Beam.WalkScale : 1f;

        public override void Always(float dt) => m_Pills.Tick(dt);

        public override void LetGo() => m_Beam.Cancel();

        public override bool OwnPrimary => true;

        /// <summary>Double Park: both cones at once, from the hand, along the aim.</summary>
        public override bool FirePrimary(Vector2 muzzle)
        {
            DoubleParkSpec cones = KcajTuning.Cones;
            WeaponDef w = Content.DoublePark;
            for (int i = 0; i < cones.Count; i++)
                World.Combat.FirePlayerShot(w, muzzle, cones.Velocity(i, Player.Aim, Player.Facing, Player.Vel));
            Sfx.PlayShot(w);
            return true;
        }

        public override void Special(float dt, bool pressed, bool held) => m_Beam.Tick(dt, pressed, held);

        public override void Super()
        {
            if (m_Pills.Raining || !Player.Kit.UseSuper()) return;
            World.Scoring.Popup("UNMEDICATED!", Player.Pos + Vector2.up * 2.2f, KcajArt.Beam);
            m_Pills.Start();
        }

        public override void Render()
        {
            m_Pills.Render();
            m_Beam.Render();
        }

        /// <summary>His beam on foot: from his gun, along his aim, from his specials, and he turns to what it's stuck to.</summary>
        sealed class Soldier : IFixateHost
        {
            readonly Player m_Player;
            public Soldier(Player player) => m_Player = player;
            public World World => m_Player.World;
            public Vector2 BeamFrom => m_Player.MuzzlePos;
            public Vector2 BeamAim => m_Player.Aim;
            public Vector2 Center => m_Player.Center;
            public bool Down => m_Player.Dead;
            public bool HasCharge => m_Player.Specials > 0;
            public void SpendCharge() => m_Player.Kit.UseSpecial();
            public void Face(Vector2 at) => m_Player.Facing = at.x >= m_Player.Pos.x ? 1 : -1;
        }
    }

    /// <summary>Whoever shines Hyperfixate (<see cref="KcajBeam"/>): kcaj on foot, or his tabk's nozzle (<see cref="KcajTabkGuns"/>).</summary>
    public interface IFixateHost
    {
        World World { get; }
        /// <summary>Where the beam leaves from, and which way.</summary>
        Vector2 BeamFrom { get; }
        Vector2 BeamAim { get; }
        /// <summary>Where it's measured from, for how far the target's got.</summary>
        Vector2 Center { get; }
        /// <summary>Down, so the beam isn't drawn.</summary>
        bool Down { get; }
        /// <summary>A charge (a special, a tabk's round) is left to lock with, and spending it.</summary>
        bool HasCharge { get; }
        void SpendCharge();
        /// <summary>Held on something at <paramref name="at"/>: turn to it, if it turns.</summary>
        void Face(Vector2 at);
    }

    /// <summary>
    /// A partner's kcaj on our screen: his beam, drawn from his gun to what it's stuck to, and his pills, rained from
    /// the same seed. Both are harmless here; his screen does the damage. The same plays a partner's SOFT SERVE
    /// (<paramref name="tabk"/>), whoever drives it: its beam from its nozzle (<see cref="RemotePlayer.TabkMuzzle"/>).
    /// </summary>
    public sealed class KcajCopy : KitCopy
    {
        readonly SpriteRenderer m_Beam;
        readonly PillRain m_Rain;
        readonly PillView m_Pills;
        readonly KcajPills.Ground m_Ground;
        readonly HyperfixateSpec m_Spec;
        readonly bool m_Tabk;
        Enemy m_Target;
        float m_Held;
        bool m_Raining;

        public KcajCopy(RemotePlayer remote, bool tabk = false) : base(remote)
        {
            m_Tabk = tabk;
            m_Spec = tabk ? KcajTuning.Tabk.Beam : KcajTuning.Beam;
            m_Beam = Art.Renderer(remote.Go.transform, "Hyperfixate", Art.Pixel, SortOrder.Effects + 2, shown: false);
            m_Rain = new PillRain(tabk ? KcajTuning.Tabk.Pills : KcajTuning.Pills);
            m_Pills = new PillView(remote.Go.transform, m_Rain.Capacity, 1f);
            m_Ground = new KcajPills.Ground(remote.World);
        }

        public override void Tick(float dt, in AvatarPose pose, Vector2 vel)
        {
            HyperfixateSpec spec = m_Spec;
            if (m_Target != null)
            {
                m_Held += dt;
                if (pose.Dead || m_Target.Removed || !m_Target.Shootable || m_Held > spec.MaxHold + 0.5f) m_Target = null; // in case the end got lost
                else if (Rng.Fx.Value < 0.3f) Remote.World.HitFeel.Fizz(m_Target.Center);
            }
            if (!m_Raining) return;
            m_Rain.Step(dt, m_Ground, m_Ground.OnGoon);
            for (int p = 0; p < m_Rain.PopCount; p++) KcajArt.Pop(Remote.World, m_Rain.Pops[p], 3);
            if (m_Rain.PopCount > 0) Sfx.Play(Sfx.PillPopAt(), CrewKit.PartnerVolume * 0.6f);
            if (m_Rain.Done) m_Raining = false;
        }

        public override void Render(in AvatarPose pose)
        {
            bool locked = m_Target != null && (!m_Tabk || Remote.Driving);
            m_Beam.enabled = locked;
            if (locked)
            {
                Vector2 local = PlayerVisual.MuzzleLocal(pose.AimY, pose.Crouching, pose.Diagonal);
                Vector2 from = m_Tabk ? Remote.TabkMuzzle : Remote.Pos + new Vector2(local.x * pose.Facing, local.y);
                KcajArt.DrawBeam(m_Beam, from, m_Target.Center, Mathf.Clamp01(m_Held / m_Spec.RampTime));
            }
            m_Pills.Render(m_Rain, m_Raining);
        }

        public override void OnMove(in KitEvent move)
        {
            World w = Remote.World;
            switch (move.Move)
            {
                case KitMove.Fixate:
                    bool holds = move.Data.y > 0.5f;
                    m_Target = holds ? w.FindEnemy(Mathf.RoundToInt(move.Data.x)) : null;
                    m_Held = 0f;
                    if (holds) Sfx.Play(Sfx.BeamLock, CrewKit.PartnerVolume * 0.7f);
                    break;
                case KitMove.Pills:
                    m_Rain.Start((uint)Mathf.RoundToInt(move.Data.y), move.At.x, move.Data.x, move.At.y);
                    m_Raining = true;
                    Sfx.Play(Sfx.PillsRattle, CrewKit.PartnerVolume);
                    break;
            }
        }
    }

    /// <summary>
    /// kcaj's special, Hyperfixate, on screen. The rules are <see cref="HyperfixateSpec"/> and
    /// <see cref="Hyperfixation"/>; this finds what the beam touches, draws the beam and the dimmed screen edges, burns
    /// the target (through <see cref="Combat.DamageEnemy"/>, so a partner's authority hears of it), and tells the
    /// partner what it's stuck to.
    /// </summary>
    public sealed class KcajBeam
    {
        readonly IFixateHost m_Host;
        readonly HyperfixateSpec m_Spec;
        readonly Hyperfixation m_Fix;
        readonly SpriteRenderer m_Beam;
        readonly SpriteRenderer[] m_Edges = new SpriteRenderer[16]; // four layers on each side, darker towards the edge
        readonly List<Enemy> m_Seen = new List<Enemy>();
        Vector2[] m_Centres = new Vector2[16], m_Halves = new Vector2[16];
        Enemy m_Target;
        Vector2 m_MissEnd;
        float m_Miss;

        public KcajBeam(IFixateHost host, HyperfixateSpec spec)
        {
            m_Host = host;
            m_Spec = spec;
            m_Fix = new Hyperfixation(spec); // its charges are the host's: kcaj's specials (Content.Kcaj), or his tabk's rounds
            Transform root = host.World.Root;
            m_Beam = MakeRenderer(root, "Hyperfixate", SortOrder.Effects + 2);
            for (int i = 0; i < m_Edges.Length; i++)
            {
                m_Edges[i] = MakeRenderer(root, "Dim", SortOrder.Effects + 20);
                m_Edges[i].color = new Color(0.03f, 0.01f, 0.06f, 0f);
            }
        }

        static SpriteRenderer MakeRenderer(Transform parent, string name, int order) => Art.Renderer(parent, name, Art.Pixel, order, shown: false);

        /// <summary>Stuck on something: kcaj only shuffles, and can't jump or fire anything else.</summary>
        public bool Locked => m_Fix.Locked;
        public Enemy Target => Locked ? m_Target : null;

        /// <summary>Its renderers go (its tabk's gone).</summary>
        public void Destroy()
        {
            Object.Destroy(m_Beam.gameObject);
            foreach (SpriteRenderer sr in m_Edges) Object.Destroy(sr.gameObject);
        }

        /// <summary>Lets go at once (he's down, or in a tabk).</summary>
        public void Cancel()
        {
            if (m_Fix.Locked)
            {
                m_Fix.Step(0f, false, false, 0f);
                Tell(m_Target, false);
            }
            m_Target = null;
        }

        /// <summary>Tells the partner the beam stuck to <paramref name="e"/>, or let go of it.</summary>
        void Tell(Enemy e, bool holds) =>
            CrewKit.Send(m_Host.World, KitMove.Fixate, e != null ? e.Center : m_Host.Center, new Vector2(e != null ? e.NetId : -1, holds ? 1f : 0f));

        /// <summary>
        /// A frame with SP <paramref name="pressed"/> or <paramref name="held"/>. Returns whether a press locked on (a
        /// charge spent) or fizzled; false when it held on, wasn't pressed or had no charge to lock with.
        /// </summary>
        public bool Tick(float dt, bool pressed, bool held)
        {
            IFixateHost p = m_Host;
            m_Miss -= dt;

            if (m_Fix.Locked)
            {
                Enemy e = m_Target;
                float burn = m_Fix.Step(dt, held, e != null && e.Shootable, e != null ? Vector2.Distance(p.Center, e.Center) : 0f);
                if (burn > 0f) Burn(e, burn);
                if (!m_Fix.Locked)
                {
                    Tell(e, false);
                    m_Target = null;
                    if (m_Fix.Ended == FixateEnd.Spent || m_Fix.Ended == FixateEnd.OutOfReach) Sfx.Play(Sfx.BeamSnap, 0.6f);
                }
                else p.Face(e.Pos);
                return false;
            }

            if (!pressed || !p.HasCharge) return false;
            Vector2 origin = p.BeamFrom, dir = p.BeamAim.normalized;
            float wall = WallDistance(origin, dir);
            Enemy hit = FirstTouched(origin, dir, wall);
            if (hit == null)
            {
                // Nothing to fixate on: the beam flicks out and fizzles, and the charge isn't spent.
                m_MissEnd = origin + dir * Mathf.Min(wall, m_Spec.Range);
                m_Miss = 0.12f;
                Sfx.Play(Sfx.BeamFizzle, 0.5f);
                return true;
            }
            p.SpendCharge();
            m_Target = hit;
            m_Fix.Lock();
            Tell(hit, true);
            Sfx.Play(Sfx.BeamLock, 0.7f);
            return true;
        }

        Enemy FirstTouched(Vector2 origin, Vector2 dir, float wall)
        {
            m_Seen.Clear();
            foreach (Enemy e in m_Host.World.Enemies)
                if (e.Shootable) m_Seen.Add(e);
            if (m_Centres.Length < m_Seen.Count)
            {
                m_Centres = new Vector2[m_Seen.Count * 2];
                m_Halves = new Vector2[m_Seen.Count * 2];
            }
            for (int i = 0; i < m_Seen.Count; i++)
            {
                Enemy e = m_Seen[i];
                m_Centres[i] = e.Center;
                m_Halves[i] = new Vector2(e.HalfW, e.Height * 0.5f);
            }
            int index = m_Spec.FirstTouched(origin, dir, m_Centres, m_Halves, m_Seen.Count, wall);
            return index >= 0 ? m_Seen[index] : null;
        }

        float WallDistance(Vector2 origin, Vector2 dir)
        {
            Level level = m_Host.World.Level;
            const float step = 0.12f;
            for (float d = 0f; d <= m_Spec.Range; d += step)
                if (level.IsSolidAt(origin + dir * d)) return d;
            return float.MaxValue;
        }

        void Burn(Enemy e, float burn)
        {
            World w = m_Host.World;
            w.ShotsHit++;
            e.Soak += burn;
            while (e.Soak >= 1f && e.Shootable)
            {
                e.Soak -= 1f;
                w.Combat.DamageEnemy(e, 1);
            }
            Vector2 at = e.Center + new Vector2(Rng.Fx.Range(-0.2f, 0.2f), Rng.Fx.Range(-0.3f, 0.3f));
            w.HitFeel.Fizz(at);
            int sparks = 1 + (int)(3f * m_Fix.Held / m_Spec.RampTime);
            for (int i = 0; i < sparks; i++)
                w.SpawnParticle(at, new Vector2(Rng.Fx.Range(-3f, 3f), Rng.Fx.Range(1f, 5f)), i % 2 == 0 ? KcajArt.Beam : KcajArt.Hot, 1.5f, 0.3f);
            float heat = Mathf.Clamp01(m_Fix.Held / m_Spec.RampTime);
            Sfx.Play(Sfx.BeamBurnAt(heat), 0.4f + 0.3f * heat);
        }

        public void Render()
        {
            IFixateHost p = m_Host;
            bool locked = m_Fix.Locked && m_Target != null && !p.Down;
            bool miss = !locked && m_Miss > 0f;
            m_Beam.enabled = locked || miss;
            if (m_Beam.enabled)
            {
                if (locked) KcajArt.DrawBeam(m_Beam, p.BeamFrom, m_Target.Center, Mathf.Clamp01(m_Fix.Held / m_Spec.RampTime));
                else KcajArt.DrawFaintBeam(m_Beam, p.BeamFrom, m_MissEnd, new Color(1f, 1f, 1f, 0.5f));
            }

            // The screen edges close in while he's fixated.
            float dim = locked ? m_Fix.Dim : 0f;
            Rect view = p.World.View.Rect;
            for (int side = 0; side < 4; side++)
            {
                for (int layer = 0; layer < 4; layer++)
                {
                    SpriteRenderer sr = m_Edges[side * 4 + layer];
                    sr.enabled = dim > 0.01f;
                    if (!sr.enabled) continue;
                    bool across = side < 2; // left and right bars span the height
                    float depth = dim * (layer + 1) * (across ? view.width : view.height) * 0.07f;
                    Vector2 centre = side switch
                    {
                        0 => new Vector2(view.xMin + depth * 0.5f, view.center.y),
                        1 => new Vector2(view.xMax - depth * 0.5f, view.center.y),
                        2 => new Vector2(view.center.x, view.yMin + depth * 0.5f),
                        _ => new Vector2(view.center.x, view.yMax - depth * 0.5f),
                    };
                    sr.transform.position = new Vector3(centre.x, centre.y, 0f);
                    sr.transform.rotation = Quaternion.identity;
                    sr.transform.localScale = across
                        ? new Vector3(depth * Art.PPU, view.height * Art.PPU, 1f)
                        : new Vector3(view.width * Art.PPU, depth * Art.PPU, 1f);
                    Color c = sr.color;
                    c.a = dim * 0.5f;
                    sr.color = c;
                }
            }
        }
    }

    /// <summary>
    /// kcaj's super, Unmedicated, on screen. The pills are a <see cref="PillRain"/>: numbers in arrays, drawn here with
    /// a pool of sprites, so a phone keeps up with 120 of them. A rain is laid out from a seed, which is all the partner
    /// is sent (<see cref="KitMove.Pills"/>).
    /// </summary>
    public sealed class KcajPills
    {
        /// <summary>What the pills land on and pop against: the level's ground, and the goons.</summary>
        public sealed class Ground : IPillGround
        {
            readonly World m_World;
            public readonly System.Func<float, float, bool> OnGoon;

            public Ground(World world)
            {
                m_World = world;
                OnGoon = Touches;
            }

            public bool Solid(float x, float y) => m_World.Level.IsSolidAt(new Vector2(x, y));

            bool Touches(float x, float y)
            {
                var at = new Vector2(x, y);
                foreach (Enemy e in m_World.Enemies)
                    if (e.Shootable && e.Bounds.Contains(at)) return true;
                return false;
            }
        }

        readonly World m_World;
        readonly UnmedicatedSpec m_Spec;
        readonly PillRain m_Rain;
        readonly PillView m_View;
        readonly Ground m_Ground;

        /// <param name="parent">What the pills are drawn under: the world's root, unless something that comes and goes owns them.</param>
        public KcajPills(World world, UnmedicatedSpec spec, Transform parent = null)
        {
            m_World = world;
            m_Spec = spec;
            m_Rain = new PillRain(spec);
            m_Ground = new Ground(world);
            m_View = new PillView(parent != null ? parent : world.Root, m_Rain.Capacity, 1f);
        }

        /// <summary>Pills are still falling or bouncing.</summary>
        public bool Raining { get; private set; }

        public void Start()
        {
            World w = m_World;
            Rect view = w.View.Rect;
            uint seed = PillRain.Seed(w.Rng);
            m_Rain.Start(seed, view.xMin, view.xMax, view.yMax);
            CrewKit.Send(w, KitMove.Pills, new Vector2(view.xMin, view.yMax), new Vector2(view.xMax, seed));
            Raining = true;
            Sfx.Play(Sfx.PillsRattle, 0.8f);
        }

        public void Tick(float dt)
        {
            if (!Raining) return;
            m_Rain.Step(dt, m_Ground, m_Ground.OnGoon);
            World w = m_World;
            for (int p = 0; p < m_Rain.PopCount; p++)
            {
                Vector2 at = m_Rain.Pops[p];
                Pop(w, at);
            }
            if (m_Rain.PopCount > 0) Sfx.Play(Sfx.PillPopAt(), Mathf.Min(0.7f, 0.3f + 0.08f * m_Rain.PopCount));
            if (m_Rain.Done) Raining = false;
        }

        void Pop(World w, Vector2 at)
        {
            float reach = m_Spec.PopRadius;
            foreach (Enemy e in w.Enemies)
            {
                if (!e.Shootable) continue;
                Rect b = e.Bounds;
                float dx = Mathf.Max(Mathf.Max(b.xMin - at.x, at.x - b.xMax), 0f), dy = Mathf.Max(Mathf.Max(b.yMin - at.y, at.y - b.yMax), 0f);
                if (dx * dx + dy * dy > reach * reach) continue;
                e.Soak += m_Spec.PopDamage;
                while (e.Soak >= 1f && e.Shootable)
                {
                    e.Soak -= 1f;
                    w.Combat.DamageEnemy(e, 1);
                }
                w.HitFeel.Fizz(at);
            }
            // m_Spec.HurtsSoldiers is false: the crew are never touched.
            KcajArt.Pop(w, at, 3);
        }

        public void Render() => m_View.Render(m_Rain, Raining);
    }

    /// <summary>A rain's pills on screen: a pool of sprites, one a pill, placed each frame from the <see cref="PillRain"/>.</summary>
    public sealed class PillView
    {
        readonly SpriteRenderer[] m_Sprites;
        readonly float[] m_Spin;

        public PillView(Transform parent, int count, float scale)
        {
            m_Sprites = new SpriteRenderer[count];
            m_Spin = new float[count];
            var root = new GameObject("Pills").transform;
            root.SetParent(parent, false);
            for (int i = 0; i < count; i++)
            {
                SpriteRenderer sr = Art.Renderer(root, "Pill", KcajArt.Pills[i % KcajArt.Pills.Length], SortOrder.Projectiles, shown: false);
                sr.transform.localScale = new Vector3(scale, scale, 1f);
                m_Sprites[i] = sr;
                m_Spin[i] = Rng.Fx.Range(-720f, 720f);
            }
        }

        public void Render(PillRain rain, bool raining)
        {
            float t = Time.time;
            for (int i = 0; i < m_Sprites.Length; i++)
            {
                SpriteRenderer sr = m_Sprites[i];
                bool show = raining && rain.IsLive(i);
                if (sr.enabled != show) sr.enabled = show;
                if (!show) continue;
                Vector2 at = rain.Position(i);
                Transform tr = sr.transform;
                tr.position = new Vector3(Entity.Snap(at.x), Entity.Snap(at.y), 0f);
                tr.rotation = Quaternion.Euler(0f, 0f, Mathf.Round(t * m_Spin[i] / 45f) * 45f); // tumbling, in eighths so the pixels stay crisp
            }
        }
    }

    /// <summary>kcaj's colours, and the pills' and cones' sprites.</summary>
    public static class KcajArt
    {
        public static readonly Color Beam = new Color(1f, 0.35f, 0.75f), Hot = new Color(1f, 0.95f, 0.85f);
        public static readonly Color[] PillColours =
        {
            new Color(1f, 0.4f, 0.55f), new Color(0.45f, 0.75f, 1f), Palette.Gold, new Color(0.55f, 0.95f, 0.55f),
        };

        static Sprite[] s_Pills;
        static Sprite s_Cone;

        /// <summary>
        /// Double Park's cone in flight, nose first (it points along its flight, so the waffle leads): a waffle cone
        /// with a scoop of strawberry and a scoop of vanilla.
        /// </summary>
        public static Sprite Cone
        {
            get
            {
                if (s_Cone != null) return s_Cone;
                var c = new PixelCanvas(10, 6);
                var outline = new Color32(40, 22, 20, 255);
                var waffle = new Color32(222, 160, 80, 255);
                var waffleDark = new Color32(168, 108, 48, 255);
                var pink = new Color32(255, 140, 180, 255);
                var cream = new Color32(255, 246, 220, 255);
                // the scoops at the back (left), the cone tapering to its tip at the front (right)
                c.Rect(0, 1, 2, 4, outline);
                c.Rect(1, 0, 3, 6, outline);
                c.Rect(1, 1, 3, 2, pink);
                c.Rect(1, 3, 3, 2, cream);
                c.Rect(4, 0, 4, 6, outline);
                c.Rect(4, 1, 3, 4, waffle);
                c.Rect(7, 1, 2, 4, outline);
                c.Rect(7, 2, 2, 2, waffle);
                c.Rect(9, 2, 1, 2, outline);
                c.Rect(5, 2, 1, 1, waffleDark);
                c.Rect(6, 3, 1, 1, waffleDark);
                c.Rect(4, 4, 1, 1, waffleDark);
                return s_Cone = Art.ToSprite(c, new Vector2(0.5f, 0.5f));
            }
        }

        /// <summary>
        /// Draws a Hyperfixate beam from <paramref name="from"/> to <paramref name="to"/>, kcaj's and his tabk's alike:
        /// <paramref name="heat"/> (0 to 1) thickens it and burns it white, and it flickers.
        /// </summary>
        public static void DrawBeam(SpriteRenderer beam, Vector2 from, Vector2 to, float heat)
        {
            Stretch(beam, from, to, 1f + Mathf.Round(heat * 2f) + Fx.Step(Time.time, 30f, 2));
            beam.color = Color.Lerp(Beam, Hot, heat * (0.6f + 0.4f * Mathf.Sin(Time.time * 40f)));
        }

        /// <summary>Draws a thin, faint beam that does no harm: a miss, or a warning of one to come.</summary>
        public static void DrawFaintBeam(SpriteRenderer beam, Vector2 from, Vector2 to, Color colour)
        {
            Stretch(beam, from, to, 1f);
            beam.color = colour;
        }

        static void Stretch(SpriteRenderer beam, Vector2 from, Vector2 to, float thickness)
        {
            Vector2 d = to - from;
            Transform t = beam.transform;
            t.position = (Vector3)((from + to) * 0.5f);
            t.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            t.localScale = new Vector3(d.magnitude * Art.PPU, thickness, 1f);
        }

        /// <summary>A pill popping: a puff in its colours.</summary>
        public static void Pop(World w, Vector2 at, int flecks)
        {
            for (int i = 0; i < flecks; i++)
                w.SpawnParticle(at, new Vector2(Rng.Fx.Range(-3f, 3f), Rng.Fx.Range(1f, 4f)), PillColours[i % PillColours.Length], 1.5f, 0.3f);
        }

        /// <summary>Capsules, half a colour and half white, one for each colour.</summary>
        public static Sprite[] Pills
        {
            get
            {
                if (s_Pills != null) return s_Pills;
                s_Pills = new Sprite[PillColours.Length];
                var white = new Color32(250, 248, 240, 255);
                var outline = new Color32(30, 20, 40, 255);
                for (int i = 0; i < PillColours.Length; i++)
                {
                    var c = new PixelCanvas(7, 4);
                    Color32 colour = PillColours[i];
                    c.Rect(1, 0, 5, 4, outline);
                    c.Rect(0, 1, 7, 2, outline);
                    c.Rect(1, 1, 2, 2, colour);
                    c.Rect(3, 1, 3, 2, white);
                    s_Pills[i] = Art.ToSprite(c, new Vector2(0.5f, 0.5f));
                }
                return s_Pills;
            }
        }
    }
}
