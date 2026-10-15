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
                // Follow the leading soldier, but never push the trailing one off the left edge.
                float lead = float.MinValue, trail = float.MaxValue;
                if (!player.IsDown)
                {
                    lead = trail = player.Pos.x;
                }
                foreach (RemotePlayer r in w.Remotes)
                {
                    if (r.IsDown) continue;
                    lead = Mathf.Max(lead, r.Pos.x);
                    trail = Mathf.Min(trail, r.Pos.x);
                }
                if (lead > float.MinValue)
                {
                    float desired = Mathf.Min(lead + Tuning.Camera.LookAhead, trail - Tuning.Coop.TrailMargin + h);
                    if (desired > m_X) m_X = Mathf.Min(desired, m_X + step);
                }
            }
            else if (w.Net.CamX > m_X)
            {
                m_X = Mathf.Min(w.Net.CamX, m_X + Tuning.Coop.FollowerCatchUp * step);
            }
            m_X = Mathf.Clamp(m_X, h, Mathf.Max(h, w.Level.Width - h));

            Vector2 shake = Vector2.zero;
            if (m_Shake > 0f)
            {
                shake = Rng.Fx.InsideUnitCircle() * m_Shake;
                m_Shake = Mathf.Max(0f, m_Shake - dt * Tuning.Camera.ShakeDecay);
            }
            // A view narrower than the shared co-op width (odd fullscreen aspect) keeps our own soldier in frame.
            float view = m_X;
            if (HalfWidth < h) view = Mathf.Clamp(player.Pos.x, m_X - (h - HalfWidth), m_X + (h - HalfWidth));
            view = Mathf.Clamp(view, HalfWidth, Mathf.Max(HalfWidth, w.Level.Width - HalfWidth));
            float cx = Entity.Snap(view + shake.x), cy = Entity.Snap(Tuning.Camera.ViewHeight * 0.5f + shake.y);
            w.Camera.transform.position = new Vector3(cx, cy, -10f);

            // Parallax: layer origin moves with the camera by `factor` (1 = infinitely far away).
            m_Sky.position = new Vector3(cx, cy, 0f);
            m_Sky.localScale = new Vector3((HalfWidth * 2f + 2f) * Art.PPU, (Tuning.Camera.ViewHeight + 2f) / 2f, 1f);
            PlaceLayer(m_Mountains, Art.Mountains, 0.85f, 1.2f, cx);
            PlaceLayer(m_Ruins, Art.Ruins, 0.6f, 1.0f, cx);
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
            m_Sky = MakeBg("Sky", Art.Sky, SortOrder.Sky);
            for (int i = 0; i < 3; i++)
            {
                m_Mountains[i] = MakeBg("Mountains" + i, Art.Mountains, SortOrder.Mountains);
                m_Ruins[i] = MakeBg("Ruins" + i, Art.Ruins, SortOrder.Ruins);
            }
        }

        Transform MakeBg(string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(m_World.Root, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return go.transform;
        }
    }
}
