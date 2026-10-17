using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// The side-scrolling camera. It only moves forward (Metal Slug style), follows the soldiers, shakes, and
    /// moves the parallax background. In co-op the authority follows the leading soldier without pushing the
    /// trailing one off screen, and the others chase the authority's scroll.
    /// </summary>
    public sealed class ScrollCamera
    {
        readonly World m_World;
        readonly Transform[] m_Mountains = new Transform[3], m_Ruins = new Transform[3];
        Transform m_Sky;
        // Spike gen-plates: the generated plates, when ?spike=plates found them (else null and the drawn layers show).
        Sprite m_SkyPlate, m_FarPlate, m_MidPlate;
        Transform[] m_SkyTiles;
        float m_X, m_Shake;

        public ScrollCamera(World world)
        {
            m_World = world;
            BuildBackground();
            m_X = GameHalfWidth;
            // Joining a game in progress: start where the others are.
            if (world.Coop && !world.Net.IsAuthority) m_X = Mathf.Max(m_X, world.Net.CamX);
        }

        /// <summary>The scroll position: the middle of the view the game plays in.</summary>
        public float X => m_X;
        /// <summary>Half the width this screen shows.</summary>
        public float HalfWidth => Tuning.Camera.ViewHeight * 0.5f * m_World.Camera.aspect;
        /// <summary>Half the width the game plays in: the screen's, or in co-op the width both clients share.</summary>
        float GameHalfWidth => m_World.Coop ? Tuning.Coop.HalfWidth : HalfWidth;
        public float Left => m_X - GameHalfWidth;
        public float Right => m_X + GameHalfWidth;
        public Rect Rect => new Rect(m_X - Mathf.Max(HalfWidth, GameHalfWidth), 0f, 2f * Mathf.Max(HalfWidth, GameHalfWidth), Tuning.Camera.ViewHeight);

        /// <summary>Screen shake is on (the SCREEN SHAKE setting).</summary>
        public static bool Shaking = true;

        public void Shake(float amount)
        {
            if (Shaking) m_Shake = Mathf.Max(m_Shake, amount);
        }

        /// <summary>Moves straight to <paramref name="x"/>, for a test that starts partway through a level.</summary>
        public void JumpTo(float x) => m_X = Mathf.Max(m_X, x);

        public void Tick(float dt)
        {
            World w = m_World;
            Player player = w.Player;
            float h = GameHalfWidth;
            float step = Tuning.Camera.ScrollSpeed * Mathf.Max(dt, 0.0001f);
            if (!w.Coop)
            {
                // Scroll forward only (Metal Slug style), with a little look-ahead.
                float desired = player.Pos.x + Tuning.Camera.LookAhead;
                if (desired > m_X) m_X = Mathf.Min(desired, m_X + step);
            }
            else if (w.IsAuthority)
            {
                // Follow the leading soldier, but never push the trailing one off the left edge (ScrollHold). A downed
                // soldier doesn't hold it, nor (spike coop-camera) does one who's switched away.
                var hold = new ScrollHold();
                if (!player.IsDown) hold.Add(player.Pos.x);
                foreach (RemotePlayer r in w.Remotes)
                    if (!r.IsDown && !(Spike.Partner && r.Away)) hold.Add(r.Pos.x);
                if (hold.Any)
                {
                    float desired = hold.Desired(h);
                    if (desired > m_X) m_X = Mathf.Min(desired, m_X + step);
                }
            }
            else if (w.Net.CamX > m_X)
            {
                m_X = Mathf.Min(w.Net.CamX, m_X + Tuning.Coop.FollowerCatchUp * step);
            }
            m_X = Mathf.Clamp(m_X, h, Mathf.Max(h, w.Level.ScrollLimit - h)); // the map's end, or the back gate until the coda opens

            Vector2 shake = Vector2.zero;
            if (m_Shake > 0f)
            {
                shake = Rng.Fx.InsideUnitCircle() * m_Shake;
                m_Shake = Mathf.Max(0f, m_Shake - dt * Tuning.Camera.ShakeDecay);
            }
            // A view narrower than the shared co-op width (odd fullscreen aspect) keeps our own soldier in frame.
            float view = m_X;
            if (HalfWidth < h && !w.Spectating) view = Mathf.Clamp(player.Pos.x, m_X - (h - HalfWidth), m_X + (h - HalfWidth));
            view = Mathf.Clamp(view, HalfWidth, Mathf.Max(HalfWidth, w.Level.ScrollLimit - HalfWidth));
            float cx = Entity.Snap(view + shake.x), cy = Entity.Snap(Tuning.Camera.ViewHeight * 0.5f + shake.y);
            w.Camera.transform.position = new Vector3(cx, cy, -10f);

            // Parallax: layer origin moves with the camera by `factor` (1 = infinitely far away).
            m_Sky.position = new Vector3(cx, cy, 0f);
            m_Sky.localScale = new Vector3((HalfWidth * 2f + 2f) * Art.PPU, (Tuning.Camera.ViewHeight + 2f) / 2f, 1f);
            if (m_SkyTiles != null) PlaceLayer(m_SkyTiles, m_SkyPlate, 0.95f, 0f, cx);
            PlaceLayer(m_Mountains, m_FarPlate ?? Art.Mountains, 0.85f, 1.2f, cx);
            PlaceLayer(m_Ruins, m_MidPlate ?? Art.Ruins, 0.6f, 1.0f, cx);
        }

        void PlaceLayer(Transform[] tiles, Sprite sprite, float factor, float y, float camX)
        {
            float width = sprite.rect.width / Art.PPU;
            float origin = camX * factor;
            float left = camX - HalfWidth;
            float first = origin + Mathf.Floor((left - origin) / width) * width;
            for (int i = 0; i < tiles.Length; i++)
                tiles[i].position = new Vector3(Entity.Snap(first + i * width), y, 0f);
        }

        void BuildBackground()
        {
            if (Spike.Plates && !m_World.Coop)
            {
                m_SkyPlate = LoadPlate("m1-sky");
                m_FarPlate = LoadPlate("m1-far");
                m_MidPlate = LoadPlate("m1-mid");
            }
            m_Sky = MakeBg("Sky", Art.Sky, SortOrder.Sky);
            if (m_SkyPlate != null)
            {
                m_SkyTiles = new Transform[3];
                for (int i = 0; i < 3; i++) m_SkyTiles[i] = MakeBg("SkyPlate" + i, m_SkyPlate, SortOrder.Sky + 1);
            }
            for (int i = 0; i < 3; i++)
            {
                m_Mountains[i] = MakeBg("Mountains" + i, m_FarPlate ?? Art.Mountains, SortOrder.Mountains);
                m_Ruins[i] = MakeBg("Ruins" + i, m_MidPlate ?? Art.Ruins, SortOrder.Ruins);
            }
        }

        /// <summary>
        /// Spike gen-plates: a generated plate from Resources/Plates (a PNG kept as .bytes so Unity leaves it alone):
        /// <c>&lt;id&gt;</c> is the first model's, <c>&lt;model&gt;__&lt;id&gt;</c> each model's take (<c>&amp;take=</c>
        /// picks one by the start of its model's name). Game pixels, 16 to a unit, pivot at the bottom left. Null if missing.
        /// </summary>
        static Sprite LoadPlate(string id)
        {
            TextAsset file = null;
            if (string.IsNullOrEmpty(Spike.PlatesTake)) file = Resources.Load<TextAsset>("Plates/" + id);
            else
                foreach (TextAsset t in Resources.LoadAll<TextAsset>("Plates"))
                    if (t.name.EndsWith("__" + id) && t.name.StartsWith(Spike.PlatesTake, System.StringComparison.OrdinalIgnoreCase)) file = t;
            if (file == null)
            {
                Log.Game.Info("plates: no " + id + (string.IsNullOrEmpty(Spike.PlatesTake) ? "" : " for " + Spike.PlatesTake));
                return null;
            }
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            if (!tex.LoadImage(file.bytes)) return null;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero, Art.PPU, 0, SpriteMeshType.FullRect);
        }

        Transform MakeBg(string name, Sprite sprite, int order)
        {
            return Art.Renderer(m_World.Root, name, sprite, order).transform;
        }
    }
}
