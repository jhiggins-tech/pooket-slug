using UnityEngine;

namespace PooketSlug
{
    /// <summary>A crate or medkit in the world. What it does comes from its <see cref="PickupDef"/>.</summary>
    public sealed class Pickup : Entity
    {
        public readonly PickupDef Def;
        public int NetId = -1;      // its NetIds id; -1 stays local
        public bool ClaimPending;   // co-op: asked the authority for it, waiting for the answer
        public bool Granted;        // co-op authority: already given to someone, so later claims lose
        readonly SpriteRenderer m_Sr;
        float m_Time;

        public Pickup(World world, PickupDef def, Vector2 feet) : base(world, "Pickup", feet)
        {
            Def = def;
            m_Sr = AddRenderer(Go.transform, "Sprite", Art.PickupSprite(def), SortOrder.Pickups);
        }

        public Rect Bounds => new Rect(Pos.x - 0.45f, Pos.y, 0.9f, 0.8f);

        public override void Tick(float dt) => m_Time += dt;

        public override void Render()
        {
            base.Render();
            // gentle hop so pickups read as interactive
            float hop = Mathf.Abs(Mathf.Sin(m_Time * 4f)) * 2f;
            m_Sr.transform.localPosition = new Vector3(0f, Mathf.Round(hop) / Art.PPU, 0f);
        }

        /// <summary>Gives the pickup's effect to the local soldier.</summary>
        public void ApplyTo(Player player) => Give(Def, player);

        public static void Give(PickupDef def, Player player)
        {
            switch (def.Effect)
            {
                case PickupEffect.Gun: player.GiveGun(def.Gun); break;
                case PickupEffect.Specials:
                    player.Specials += player.Def.CrateSpecials;
                    player.World.Scoring.Popup(player.Def.Special.Hud + " +" + player.Def.CrateSpecials, player.Pos + Vector2.up * 2.4f, def.AnnounceColor);
                    break;
                case PickupEffect.Heal:
                    int healed = Mathf.Min(def.Amount, player.MaxHp - player.Hp);
                    player.Heal(def.Amount);
                    player.World.Scoring.Popup(healed > 0 ? "+" + healed + (healed == 1 ? " HEART" : " HEARTS") : "FULL HEALTH", player.Pos + Vector2.up * 2.4f, def.AnnounceColor);
                    break;
                case PickupEffect.Life: player.GiveLife(); break;
            }
        }

        /// <summary>Removes the pickup with its popup and sound (on every client).</summary>
        public void Collect()
        {
            World.Scoring.Popup(Def.Announce, Pos + Vector2.up * 1.6f, Def.AnnounceColor);
            if (World.IsAuthority) World.Scoring.Add(Def.Score, Pos + Vector2.up, false);
            Sfx.Play(Sfx.Pickup);
            Remove();
        }
    }
}
