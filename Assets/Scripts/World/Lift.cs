using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// A lift: a steel platform that rides up and down its rails on a fixed timetable, so it's in the same place
    /// on every client. Our soldier can stand on it (each client carries its own soldier).
    /// </summary>
    public sealed class Lift : Entity
    {
        /// <summary>How far it rises, in tiles.</summary>
        public const int Rise = 5;
        const float Speed = 2f, Pause = 1.2f, HalfWidth = 1f;
        readonly float m_Bottom;

        /// <summary><paramref name="feet"/> is its marker: the left of its two tiles, at the bottom of its run.</summary>
        public Lift(World world, Vector2 feet) : base(world, "Lift", new Vector2(feet.x + 0.5f, feet.y))
        {
            m_Bottom = feet.y;
            Pos.y = HeightAt(world.Elapsed);
            AddRenderer(Go.transform, "Car", Art.LiftCar, SortOrder.Pickups);
        }

        float HeightAt(float time)
        {
            float travel = Rise / Speed, t = time % (2f * (travel + Pause));
            if (t < Pause) return m_Bottom;
            t -= Pause;
            if (t < travel) return m_Bottom + t * Speed;
            t -= travel;
            if (t < Pause) return m_Bottom + Rise;
            t -= Pause;
            return m_Bottom + Rise - t * Speed;
        }

        public override void Tick(float dt)
        {
            // Our soldier has just moved (soldiers tick first). One standing on the car, or landing on it, rides it.
            Player p = World.Player;
            bool riding = !p.Dead && Mathf.Abs(p.Pos.x - Pos.x) < HalfWidth && p.Vel.y <= 0.01f &&
                          p.Pos.y <= Pos.y + 0.05f && p.Pos.y >= Pos.y - 0.45f;
            Pos.y = HeightAt(World.Elapsed);
            if (!riding) return;
            p.Pos.y = Pos.y;
            p.Vel.y = 0f;
            p.Grounded = true;
        }
    }
}
