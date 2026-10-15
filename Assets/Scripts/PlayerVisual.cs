using UnityEngine;

namespace PooketSlug
{
    /// <summary>Legs + torso + muzzle flash sprites for one soldier, driven by an <see cref="AvatarPose"/>.</summary>
    public sealed class PlayerVisual
    {
        readonly Transform m_Visual;
        readonly SpriteRenderer m_Legs, m_Torso, m_Flash;
        public PlayerSkin Skin;

        public PlayerVisual(Transform visual, PlayerSkin skin, int order)
        {
            m_Visual = visual;
            Skin = skin;
            m_Legs = Add("Legs", skin.LegsIdle, order);
            m_Torso = Add("Torso", skin.TorsoForward, order + 1);
            m_Flash = Add("Muzzle", Art.MuzzleFlash, order + 2);
            m_Flash.enabled = false;
        }

        SpriteRenderer Add(string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(m_Visual, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>Gun tip position relative to the feet, for a right-facing soldier (px / 16).</summary>
        public static Vector2 MuzzleLocal(int aimY, bool crouching, bool diagonal = false)
        {
            if (diagonal && aimY > 0) return new Vector2(10.5f, 22.5f) / Art.PPU;
            if (diagonal && aimY < 0) return new Vector2(9.5f, 7.5f) / Art.PPU;
            if (aimY > 0) return new Vector2(3.5f, 30.5f) / Art.PPU;
            if (aimY < 0) return new Vector2(2.5f, 3.5f) / Art.PPU;
            return new Vector2(11.5f, crouching ? 8.5f : 14.5f) / Art.PPU;
        }

        /// <summary>Applies the pose. The owner flips <c>Visual</c> by facing (see <see cref="Actor.Render"/>).</summary>
        public void Apply(in AvatarPose p)
        {
            int runFrame = (int)(p.Anim * 12f) % 4;
            Sprite legs;
            if (p.Dead || !p.Grounded) legs = Skin.LegsJump;
            else if (p.Crouching) legs = Skin.LegsCrouch;
            else if (p.Running) legs = Skin.LegsRun[runFrame];
            else legs = Skin.LegsIdle;
            Sprite torso = p.AimY > 0 ? (p.Diagonal ? Skin.TorsoUpDiag : Skin.TorsoUp)
                : p.AimY < 0 ? (p.Diagonal ? Skin.TorsoDownDiag : Skin.TorsoDown)
                : Skin.TorsoForward;

            m_Legs.sprite = p.Flash ? Art.Flash(legs) : legs;
            m_Torso.sprite = p.Flash ? Art.Flash(torso) : torso;

            float bob = p.Running && (runFrame == 0 || runFrame == 2) ? -1f : 0f;
            float drop = p.Crouching ? -ArtData.CrouchDrop : bob;
            m_Torso.transform.localPosition = new Vector3(0f, drop / Art.PPU, 0f);

            m_Flash.enabled = p.Muzzle && !p.Dead;
            if (m_Flash.enabled)
            {
                // Visual is mirrored by facing, so work in right-facing local space.
                Vector2 aim = p.AimY == 0 ? Vector2.right : new Vector2(p.Diagonal ? 1f : 0f, p.AimY).normalized;
                Vector2 tip = MuzzleLocal(p.AimY, p.Crouching, p.Diagonal) + aim * (3f / Art.PPU);
                m_Flash.transform.localPosition = new Vector3(tip.x, tip.y, 0f);
            }

            // blink while invulnerable, flicker out when dead
            bool hidden = (p.Blink && (int)(Time.unscaledTime * 20f) % 2 == 0) ||
                          (p.Dead && p.DeadTime > 1.2f && (int)(p.DeadTime * 12f) % 2 == 0) || (p.Dead && p.Gone);
            m_Visual.gameObject.SetActive(!hidden);
            m_Visual.localRotation = p.Dead ? Quaternion.Euler(0f, 0f, Mathf.Min(90f, p.DeadTime * 300f) * p.Facing) : Quaternion.identity;
        }
    }
}
