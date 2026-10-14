using UnityEngine;

namespace PooketSlug
{
    /// <summary>Everything needed to draw a soldier. Produced by the local <see cref="Player"/> and replicated for remote ones.</summary>
    public struct AvatarPose
    {
        const int FacingRightBit = 1 << 0, AimUpBit = 1 << 1, AimDownBit = 1 << 2, GroundedBit = 1 << 3, CrouchBit = 1 << 4;
        const int RunningBit = 1 << 5, DeadBit = 1 << 6, MuzzleBit = 1 << 7, BlinkBit = 1 << 8, FlashBit = 1 << 9;

        public int Facing;  // 1 = right, -1 = left
        public int AimY;    // 1 = up, -1 = down, 0 = forward
        public bool Grounded, Crouching, Running, Dead, Muzzle, Blink, Flash;
        public float Anim, DeadTime;

        public int Pack() =>
            (Facing > 0 ? FacingRightBit : 0) | (AimY > 0 ? AimUpBit : 0) | (AimY < 0 ? AimDownBit : 0) |
            (Grounded ? GroundedBit : 0) | (Crouching ? CrouchBit : 0) | (Running ? RunningBit : 0) |
            (Dead ? DeadBit : 0) | (Muzzle ? MuzzleBit : 0) | (Blink ? BlinkBit : 0) | (Flash ? FlashBit : 0);

        /// <summary>Unpacks replicated flags; animation clocks are kept locally by the caller.</summary>
        public static AvatarPose Unpack(int flags, float anim, float deadTime) => new AvatarPose
        {
            Facing = (flags & FacingRightBit) != 0 ? 1 : -1,
            AimY = (flags & AimUpBit) != 0 ? 1 : (flags & AimDownBit) != 0 ? -1 : 0,
            Grounded = (flags & GroundedBit) != 0,
            Crouching = (flags & CrouchBit) != 0,
            Running = (flags & RunningBit) != 0,
            Dead = (flags & DeadBit) != 0,
            Muzzle = (flags & MuzzleBit) != 0,
            Blink = (flags & BlinkBit) != 0,
            Flash = (flags & FlashBit) != 0,
            Anim = anim,
            DeadTime = deadTime,
        };
    }

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
        public static Vector2 MuzzleLocal(int aimY, bool crouching)
        {
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
            Sprite torso = p.AimY > 0 ? Skin.TorsoUp : p.AimY < 0 ? Skin.TorsoDown : Skin.TorsoForward;

            m_Legs.sprite = p.Flash ? Art.Flash(legs) : legs;
            m_Torso.sprite = p.Flash ? Art.Flash(torso) : torso;

            float bob = p.Running && (runFrame == 0 || runFrame == 2) ? -1f : 0f;
            float drop = p.Crouching ? -ArtData.CrouchDrop : bob;
            m_Torso.transform.localPosition = new Vector3(0f, drop / Art.PPU, 0f);

            m_Flash.enabled = p.Muzzle && !p.Dead;
            if (m_Flash.enabled)
            {
                // Visual is mirrored by facing, so work in right-facing local space.
                Vector2 aim = p.AimY > 0 ? Vector2.up : p.AimY < 0 ? Vector2.down : Vector2.right;
                Vector2 tip = MuzzleLocal(p.AimY, p.Crouching) + aim * (3f / Art.PPU);
                m_Flash.transform.localPosition = new Vector3(tip.x, tip.y, 0f);
            }

            // blink while invulnerable, flicker out when dead
            bool hidden = (p.Blink && (int)(Time.unscaledTime * 20f) % 2 == 0) ||
                          (p.Dead && p.DeadTime > 1.2f && (int)(p.DeadTime * 12f) % 2 == 0);
            m_Visual.gameObject.SetActive(!hidden);
            m_Visual.localRotation = p.Dead ? Quaternion.Euler(0f, 0f, Mathf.Min(90f, p.DeadTime * 300f) * p.Facing) : Quaternion.identity;
        }
    }
}
