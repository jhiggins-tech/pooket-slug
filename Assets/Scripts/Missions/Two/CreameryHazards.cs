using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Mission two's hazards in the world (their numbers and rules are <see cref="Creamery"/>'s): the conveyor belts'
    /// moving tread, the nitrogen vents, the scoop droppers and their scoops, the cold store's doors and the corridor's fog. Like Mission
    /// one's (<see cref="Hazard"/>), they run on every phone and hurt or freeze only that phone's own soldier; the
    /// authority freezes the goons. Nothing about them goes over the network.
    /// </summary>
    public sealed class BeltTread : Entity
    {
        readonly SpriteRenderer[] m_Tiles;
        readonly int m_Dir;

        BeltTread(World world, int x0, int x1, int y, int dir) : base(world, "Belt", new Vector2(x0, y + 10f / Art.PPU))
        {
            m_Dir = dir;
            m_Tiles = new SpriteRenderer[x1 - x0 + 1];
            for (int i = 0; i < m_Tiles.Length; i++)
            {
                m_Tiles[i] = AddRenderer(Go.transform, "Tread", CreameryArt.Tread[0], SortOrder.Tiles + 1);
                m_Tiles[i].transform.localPosition = new Vector3(i + (dir < 0 ? 1f : 0f), 0f, 0f);
                m_Tiles[i].flipX = dir < 0;
            }
        }

        /// <summary>A tread for each run of belt tiles on the map.</summary>
        public static void Lay(World world)
        {
            Level level = world.Level;
            for (int y = 0; y < level.Height; y++)
                for (int x = 0; x < level.Width; x++)
                {
                    int dir = level.Belt(x, y);
                    if (dir == 0) continue;
                    int end = x;
                    while (level.Belt(end + 1, y) == dir) end++;
                    world.Props.Add(new BeltTread(world, x, end, y, dir));
                    x = end;
                }
        }

        public override void Tick(float dt) { }

        public override void Render()
        {
            base.Render();
            // the chevrons move 2 px a frame, at the belt's speed
            int frame = (int)(World.Elapsed * Creamery.BeltSpeed * Art.PPU / 2f) & 3;
            foreach (SpriteRenderer sr in m_Tiles) sr.sprite = CreameryArt.Tread[frame];
        }
    }

    /// <summary>A grate in the floor that blasts liquid nitrogen every few seconds: whoever's in it freezes, goons too.</summary>
    public sealed class NitrogenVent : Hazard
    {
        static readonly Color Mist = new Color(0.85f, 0.95f, 1f, 0.75f);

        bool m_Soon, m_On;

        public NitrogenVent(World world, Vector2 feet) : base(world, "NitrogenVent", feet)
        {
            AddRenderer(Go.transform, "Sprite", CreameryArt.Vent, SortOrder.Pickups);
        }

        public override void Tick(float dt)
        {
            bool on = Creamery.VentOn(World.Elapsed, Pos.x), soon = !on && Creamery.VentSoon(World.Elapsed, Pos.x);
            // the hiss as it warns, the whoosh as it blows: only the vents in view, so a row of them isn't a din
            if (soon && !m_Soon && World.OnScreen(Pos.x)) Sfx.Play(Sfx.VentHiss, 0.6f);
            if (on && !m_On && World.OnScreen(Pos.x)) Sfx.Play(Sfx.VentBlast, 0.6f);
            m_Soon = soon;
            m_On = on;
            if (!on)
            {
                if (soon && Rng.Fx.Range(0f, 1f) < 0.25f) Puff(1.5f, 0.25f); // the tell: a hiss of mist
                return;
            }
            var column = new Rect(Pos.x - Creamery.VentHalfWidth, Pos.y, Creamery.VentHalfWidth * 2f, Creamery.VentHeight);
            Player p = World.Player;
            if (!p.Dead && column.Overlaps(p.Bounds)) p.Freeze(Creamery.SoldierFreeze);
            if (World.IsAuthority)
                foreach (Enemy e in World.Enemies)
                    if (column.Overlaps(e.Bounds)) e.Freeze(Creamery.GoonFreeze);
            if (Rng.Fx.Range(0f, 1f) < 0.7f) Puff(Rng.Fx.Range(5f, 8f), Rng.Fx.Range(0.35f, 0.55f)); // the blast itself is cosmetic
        }

        void Puff(float speed, float life)
        {
            var vel = new Vector2(Rng.Fx.Range(-0.7f, 0.7f), speed);
            World.SpawnParticle(Pos + new Vector2(Rng.Fx.Range(-0.35f, 0.35f), 0.25f), vel, Mist, Rng.Fx.Range(3f, 5f), life);
        }
    }

    /// <summary>
    /// A scoop dropper over the production line: it shakes, a shadow marks the floor under it, and it lets a scoop of
    /// ice cream fall that hurts whoever it lands on.
    /// </summary>
    public sealed class ScoopDropper : Hazard
    {
        readonly SpriteRenderer m_Body, m_Shadow;
        readonly float m_Floor;

        public ScoopDropper(World world, Vector2 feet) : base(world, "ScoopDropper", feet)
        {
            m_Body = AddRenderer(Go.transform, "Sprite", CreameryArt.Dropper, SortOrder.Scenery + 1);
            m_Floor = world.Level.TopSurfaceY(feet.x);
            m_Shadow = AddRenderer(Go.transform, "Shadow", CreameryArt.Shadow, SortOrder.Pickups - 1);
            m_Shadow.transform.localPosition = new Vector3(0f, m_Floor - feet.y, 0f);
            m_Shadow.enabled = false;
        }

        public override void Tick(float dt)
        {
            if (Creamery.ScoopDrops(World.Elapsed, dt, Pos.x))
            {
                World.Props.Add(new FallingScoop(World, Pos, m_Floor));
                if (World.OnScreen(Pos.x)) Sfx.Play(Sfx.ScoopDispense, 0.5f);
            }
        }

        public override void Render()
        {
            base.Render();
            bool telling = Creamery.ScoopTelling(World.Elapsed, Pos.x);
            m_Shadow.enabled = telling;
            m_Body.transform.localPosition = telling && Fx.Blink(World.Elapsed, 30f) ? new Vector3(1f / Art.PPU, 0f, 0f) : Vector3.zero;
        }
    }

    /// <summary>A scoop on its way down: it hurts our soldier if it hits them, and splats on the floor.</summary>
    public sealed class FallingScoop : Hazard
    {
        static readonly Color Splash = new Color(1f, 0.6f, 0.8f);
        readonly float m_Floor;
        float m_Vel;

        public FallingScoop(World world, Vector2 from, float floor) : base(world, "Scoop", from)
        {
            AddRenderer(Go.transform, "Sprite", CreameryArt.Scoop, SortOrder.Projectiles - 1);
            m_Floor = floor;
        }

        public override void Tick(float dt)
        {
            m_Vel -= Tuning.Physics.Gravity * dt;
            Pos.y += m_Vel * dt;
            HurtInside(new Rect(Pos.x - 0.4f, Pos.y, 0.8f, 0.8f), Creamery.ScoopDamage);
            if (Pos.y > m_Floor) return;
            Pos.y = m_Floor;
            World.Burst(Pos + Vector2.up * 0.3f, 12, Splash);
            if (World.OnScreen(Pos.x)) Sfx.Play(Sfx.ScoopPlop, 0.7f);
            Remove();
        }
    }

    /// <summary>
    /// Fog along the floor of the freezer corridor, from one 'f' to the next (<see cref="Level.FogEnd"/>): soft blobs
    /// that drift and swell, knee-high, in front of the goons and behind the soldiers. Only for looks.
    /// </summary>
    public sealed class FogBank : Entity
    {
        const float Spacing = 1.1f;
        readonly SpriteRenderer[] m_Blobs;
        readonly float[] m_Phase, m_Home;

        public FogBank(World world, Vector2 from, float to) : base(world, "Fog", from)
        {
            int n = Mathf.Max(1, Mathf.CeilToInt((to - from.x) / Spacing));
            m_Blobs = new SpriteRenderer[n];
            m_Phase = new float[n];
            m_Home = new float[n];
            for (int i = 0; i < n; i++)
            {
                m_Blobs[i] = AddRenderer(Go.transform, "Blob", CreameryArt.Fog, SortOrder.Enemies + 6);
                m_Phase[i] = Rng.Fx.Range(0f, 6.3f);
                m_Home[i] = i * Spacing + Rng.Fx.Range(-0.3f, 0.3f);
            }
        }

        public override void Tick(float dt) { }

        public override void Render()
        {
            base.Render();
            float t = World.Elapsed;
            for (int i = 0; i < m_Blobs.Length; i++)
            {
                float p = m_Phase[i];
                m_Blobs[i].transform.localPosition = new Vector3(m_Home[i] + Mathf.Sin(t * 0.4f + p) * 0.5f, -0.1f + Mathf.Sin(t * 0.7f + p * 2f) * 0.06f, 0f);
                float s = 1.4f + 0.25f * Mathf.Sin(t * 0.55f + p * 1.7f);
                m_Blobs[i].transform.localScale = new Vector3(s, s * 0.9f, 1f);
            }
        }
    }

    /// <summary>
    /// The cold store's doors, at the right of the boss's yard: fog spills out under them, and billows as kcaj's tabk
    /// rolls out of them (<see cref="TankBoss"/> makes its entrance from here). With the mission's plates in, the arena's
    /// painted doors are the doors (Design's, shut, over this spot at the lock), so only the fog is drawn here.
    /// </summary>
    public sealed class ColdStoreDoor : Entity
    {
        static readonly Color Fog = new Color(0.92f, 0.95f, 1f, 0.6f);
        bool m_Billowed;

        public ColdStoreDoor(World world, Vector2 feet) : base(world, "ColdStore", feet)
        {
            if (!BackdropView.Painted(world.Mission, "mid")) AddRenderer(Go.transform, "Sprite", CreameryArt.ColdStoreDoor, SortOrder.Scenery);
        }

        public override void Tick(float dt)
        {
            bool woken = World.Boss != null && World.Boss.Engaged;
            if (woken && !m_Billowed)
            {
                m_Billowed = true;
                for (int i = 0; i < 40; i++) Drift(Rng.Fx.Range(3f, 7f), Rng.Fx.Range(0.8f, 1.6f));
            }
            if (Rng.Fx.Range(0f, 1f) < 0.3f) Drift(Rng.Fx.Range(0.8f, 2f), Rng.Fx.Range(0.8f, 1.4f));
        }

        void Drift(float speed, float life)
        {
            var vel = new Vector2(-speed, Rng.Fx.Range(0f, 0.6f)); // out along the floor, into the yard
            World.SpawnParticle(Pos + new Vector2(Rng.Fx.Range(-1.4f, 0.6f), Rng.Fx.Range(0.1f, 0.6f)), vel, Fog, Rng.Fx.Range(4f, 7f), life);
        }
    }
}
