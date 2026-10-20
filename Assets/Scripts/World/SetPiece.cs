using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// A set piece: something real that stands in the level until it's shot (a barrel, Mission two's boxes, tanks and
    /// projectors, the coda's watchtower and last gate). It's an enemy to the game, shot, scored and replicated like one,
    /// but it doesn't move. What each one does is what happens as it goes off (<see cref="GoesOff"/>), once, on every
    /// screen. A new one is a class of this and its row in the spawner.
    /// </summary>
    public abstract class SetPiece : Enemy
    {
        /// <summary>Its body, drawn flashing when hit and hidden once it's gone off (unless <see cref="Render"/> is overridden).</summary>
        protected readonly SpriteRenderer Body;
        protected readonly Sprite BodyLook;
        readonly int m_Column = -1;
        bool m_WentOff;

        /// <param name="bars">It stands in the road: its column is a wall until it's destroyed (<see cref="Level.Bar"/>).</param>
        protected SetPiece(World world, EnemyDef def, Vector2 feet, Sprite look, int order, bool bars = false) : base(world, def, feet)
        {
            BodyLook = look;
            Body = AddRenderer(Visual, "Body", look, order);
            if (!bars) return;
            m_Column = Mathf.FloorToInt(feet.x);
            world.Level.Bar(m_Column, true);
        }

        /// <summary>It falls to the ground and rides belts; false for one that's bolted down.</summary>
        protected virtual bool Falls => false;

        /// <summary>
        /// Seconds it stays once it's gone off, before it's removed (0: at once). Infinity leaves the wreck lying there
        /// until it's left behind off the screen.
        /// </summary>
        protected virtual float Lingers => 0f;

        /// <summary>What it does as it's destroyed: the blast, the drop, the topple. Once, on every screen.</summary>
        protected abstract void GoesOff();

        protected override void TickAlive(float dt)
        {
            if (!Falls)
            {
                Vel = Vector2.zero;
                return;
            }
            Vel.x = 0f;
            Vel.y = Mathf.Max(Vel.y - Gravity * dt, -MaxFall);
            MoveWithTiles(dt);
        }

        protected override void Die()
        {
            base.Die();
            if (m_Column >= 0) World.Level.Bar(m_Column, false);
        }

        protected override void TickDying(float dt)
        {
            if (!m_WentOff)
            {
                m_WentOff = true;
                GoesOff();
            }
            if (DyingTime >= Lingers) Remove();
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);
            if (Dying && !Removed && Pos.x < World.View.Left - 6f) Remove(); // the wreck's left behind
        }

        public override void Render()
        {
            base.Render();
            Body.sprite = Flashed(BodyLook);
            Body.enabled = !Dying;
        }
    }
}
