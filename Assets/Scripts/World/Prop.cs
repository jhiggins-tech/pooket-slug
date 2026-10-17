using UnityEngine;

namespace PooketSlug
{
    /// <summary>Static decoration: the end-of-level flag, the watchtower's scenery, the motor pool's gate and the lift rails.</summary>
    public sealed class Prop : Entity
    {
        public Prop(World world, Sprite sprite, Vector2 feet, int order) : base(world, "Prop", feet)
        {
            AddRenderer(Go.transform, "Sprite", sprite, order);
        }

        public override void Tick(float dt) { }
    }
}
