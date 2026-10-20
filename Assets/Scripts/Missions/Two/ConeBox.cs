using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// A box of cones (Mission two's breakable): an enemy as far as the game is concerned, shot and replicated like one
    /// and never a Trollogram, that just stands there. Broken, it bursts into wafer and drops an item
    /// (<see cref="Creamery.ConeBoxDrop"/>), placed by the authority so every screen gets the same one.
    /// </summary>
    public sealed class ConeBox : SetPiece
    {
        static readonly Color Wafer = new Color(0.85f, 0.62f, 0.35f);

        public ConeBox(World world, EnemyDef def, Vector2 feet) : base(world, def, feet, CreameryArt.ConeBox, SortOrder.Enemies - 1) { }

        protected override bool Falls => true;

        protected override void GoesOff()
        {
            World.Burst(Center, 14, Wafer);
            Sfx.Play(Sfx.Thump, 0.6f);
            World.Spawner.SpawnPickup(Creamery.ConeBoxDrop(World.Rng), Pos); // null on a follower: it comes in the snapshot
        }
    }
}
