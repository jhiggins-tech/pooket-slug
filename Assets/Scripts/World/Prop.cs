using UnityEngine;

namespace PooketSlug
{
    /// <summary>Static decoration (end-of-level flag).</summary>
    public sealed class Prop : Entity
    {
        public Prop(World world, Sprite sprite, Vector2 feet, int order) : base(world, "Prop", feet)
        {
            AddRenderer(Go.transform, "Sprite", sprite, order);
        }

        public override void Tick(float dt) { }
    }
}
