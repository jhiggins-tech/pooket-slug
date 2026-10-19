using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// The side-scrolling camera. It only moves forward (Metal Slug style), follows the soldiers, shakes, and
    /// moves the parallax backdrop (<see cref="BackdropView"/>). In co-op the authority follows the leading soldier without pushing the
    /// trailing one off screen, and the others chase the authority's scroll.
    /// </summary>
    public sealed class ScrollCamera
    {
        readonly World m_World;
        readonly BackdropView m_Backdrop;
        float m_X, m_Shake;

        public ScrollCamera(World world)
        {
            m_World = world;
            m_Backdrop = new BackdropView(world, world.Mission);
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
            float limit = Mathf.Min(w.Level.ScrollLimit, w.ArenaEdge); // the map's end, the back gate until the coda opens, or a miniboss's yard
            m_X = Mathf.Clamp(m_X, h, Mathf.Max(h, limit - h));

            Vector2 shake = Vector2.zero;
            if (m_Shake > 0f)
            {
                shake = Rng.Fx.InsideUnitCircle() * m_Shake;
                m_Shake = Mathf.Max(0f, m_Shake - dt * Tuning.Camera.ShakeDecay);
            }
            // A view narrower than the shared co-op width (odd fullscreen aspect) keeps our own soldier in frame.
            float view = m_X;
            if (HalfWidth < h && !w.Spectating) view = Mathf.Clamp(player.Pos.x, m_X - (h - HalfWidth), m_X + (h - HalfWidth));
            view = Mathf.Clamp(view, HalfWidth, Mathf.Max(HalfWidth, limit - HalfWidth));
            float cx = Entity.Snap(view + shake.x), cy = Entity.Snap(Tuning.Camera.ViewHeight * 0.5f + shake.y);
            w.Camera.transform.position = new Vector3(cx, cy, -10f);

            m_Backdrop.Place(cx, cy, HalfWidth);
        }
    }
}
