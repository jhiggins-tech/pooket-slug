using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// How every weasel moves and looks, whoever's side it's on: kie's own (<see cref="PopWeasel"/>, run by the
    /// screen that threw it) and his tabk's (<see cref="Weasel"/>, an enemy the co-op authority runs). It tumbles
    /// where it's thrown until it lands, then scurries, hopping up steps; it runs on two frames and spins in the air.
    /// Both run the same rules (<see cref="WeaselSpec"/>) through <see cref="Step"/>; what it hunts and who its pop
    /// hurts stay with each side.
    /// </summary>
    public static class WeaselBody
    {
        /// <summary>A weasel's clocks: since it was thrown, and since it landed.</summary>
        public struct Run
        {
            public float Time, Fuse;
            public bool Landed;
        }

        /// <summary>
        /// One tick of a weasel hunting <paramref name="prey"/> (null: nothing to hunt, so it keeps running the way it
        /// faces), scurrying at <paramref name="speed"/> once down. Says whether it runs on, pops, or is gone.
        /// </summary>
        public static WeaselMove Step(Actor weasel, ref Run run, WeaselSpec spec, float dt, Actor prey, float speed)
        {
            run.Time += dt;
            ScrollCamera view = weasel.World.View;
            if (WeaselSpec.Leaves(weasel.Pos, view.Left, view.Right)) return WeaselMove.Gone;

            if (!run.Landed)
            {
                bool down = Tumble(weasel, dt, spec.Gravity);
                bool touching = prey != null && prey.Bounds.Overlaps(weasel.Bounds);
                if (touching || !down) return spec.InAir(run.Time, touching);
                run.Landed = true;
                return WeaselMove.Run;
            }

            run.Fuse += dt;
            if (prey != null)
            {
                float dx = prey.Pos.x - weasel.Pos.x;
                if (Mathf.Abs(dx) > 0.05f) weasel.Facing = dx > 0f ? 1 : -1;
            }
            WeaselMove move = spec.OnGround(run.Fuse, weasel.Pos, prey != null, prey != null ? prey.Pos : default, prey != null ? prey.HalfW : 0f);
            if (move == WeaselMove.Run) Scurry(weasel, dt, speed);
            return move;
        }

        /// <summary>The hop that takes a scurrying weasel up a step.</summary>
        const float Hop = 10f;

        /// <summary>In the air: falls at <paramref name="gravity"/> and moves. True once it's landed.</summary>
        public static bool Tumble(Actor weasel, float dt, float gravity)
        {
            weasel.Fall(dt, gravity);
            weasel.MoveWithTiles(dt);
            return weasel.Grounded;
        }

        /// <summary>On the ground: runs the way it faces at <paramref name="speed"/> (0 stands still), hopping up steps.</summary>
        public static void Scurry(Actor weasel, float dt, float speed)
        {
            weasel.Vel.x = weasel.Facing * speed;
            weasel.Walk(dt, Hop);
        }

        /// <summary>
        /// Its frame <paramref name="time"/> seconds in: legs going while it runs on the ground. Spins it while it's
        /// in the air, upright once <paramref name="landed"/>.
        /// </summary>
        public static Sprite Pose(Actor weasel, bool landed, float time)
        {
            bool running = landed && weasel.Grounded && Mathf.Abs(weasel.Vel.x) > 0.1f;
            weasel.VisualRoot.localRotation = landed ? Quaternion.identity : Quaternion.Euler(0f, 0f, time * -720f * weasel.Facing);
            return KieArt.Weasel[running && !Fx.Blink(time, 16f) ? 1 : 0];
        }
    }
}
