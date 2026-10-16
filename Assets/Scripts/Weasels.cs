using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// How every weasel moves and looks, whoever's side it's on: kie's own (<see cref="PopWeasel"/>, run by the
    /// screen that threw it) and his tabk's (<see cref="Weasel"/>, an enemy the co-op authority runs). It tumbles
    /// where it's thrown until it lands, then scurries, hopping up steps; it runs on two frames and spins in the air.
    /// What it hunts, when it pops and who it hurts stay with each side.
    /// </summary>
    public static class WeaselBody
    {
        /// <summary>The hop that takes a scurrying weasel up a step.</summary>
        const float Hop = 10f;

        /// <summary>In the air: falls at <paramref name="gravity"/> and moves. True once it's landed.</summary>
        public static bool Tumble(Actor weasel, float dt, float gravity)
        {
            weasel.Vel.y = Mathf.Max(weasel.Vel.y - gravity * dt, -Tuning.Physics.MaxFall);
            weasel.MoveWithTiles(dt);
            return weasel.Grounded;
        }

        /// <summary>On the ground: runs the way it faces at <paramref name="speed"/> (0 stands still), hopping up steps.</summary>
        public static void Scurry(Actor weasel, float dt, float speed)
        {
            weasel.Vel.x = weasel.Facing * speed;
            weasel.Vel.y = Mathf.Max(weasel.Vel.y - Tuning.Physics.Gravity * dt, -Tuning.Physics.MaxFall);
            MoveResult r = weasel.MoveWithTiles(dt);
            if (r.HitWall && weasel.Grounded) weasel.Vel.y = Hop;
        }

        /// <summary>
        /// Its frame <paramref name="time"/> seconds in: legs going while it runs on the ground. Spins it while it's
        /// in the air, upright once <paramref name="landed"/>.
        /// </summary>
        public static Sprite Pose(Actor weasel, bool landed, float time)
        {
            bool running = landed && weasel.Grounded && Mathf.Abs(weasel.Vel.x) > 0.1f;
            weasel.VisualRoot.localRotation = landed ? Quaternion.identity : Quaternion.Euler(0f, 0f, time * -720f * weasel.Facing);
            return KieArt.Weasel[running && (int)(time * 16f) % 2 == 1 ? 1 : 0];
        }
    }
}
