using UnityEngine;

namespace PooketSlug
{
    /// <summary>Legs + top half (body and head) + muzzle flash sprites for one soldier, driven by an <see cref="AvatarPose"/>.</summary>
    public sealed class PlayerVisual
    {
        readonly Transform m_Visual;
        readonly SpriteRenderer m_Legs, m_Torso, m_Flash;
        public PlayerSkin Skin;

        public PlayerVisual(Transform visual, PlayerSkin skin, int order)
        {
            m_Visual = visual;
            Skin = skin;
            m_Legs = Add("Legs", skin.Legs(CrewLegs.Idle), order);
            m_Torso = Add("Torso", skin.Portrait, order + 1);
            m_Flash = Add("Muzzle", Art.MuzzleFlash, order + 2);
            m_Flash.enabled = false;
        }

        SpriteRenderer Add(string name, Sprite sprite, int order) => Art.Renderer(m_Visual, name, sprite, order);

        /// <summary>Gun tip position relative to the feet, for a right-facing soldier (px / 16).</summary>
        public static Vector2 MuzzleLocal(int aimY, bool crouching, bool diagonal = false)
        {
            if (diagonal && aimY > 0) return new Vector2(10.5f, 22.5f) / Art.PPU;
            if (diagonal && aimY < 0) return new Vector2(9.5f, 7.5f) / Art.PPU;
            if (aimY > 0) return new Vector2(3.5f, 30.5f) / Art.PPU;
            if (aimY < 0) return new Vector2(2.5f, 3.5f) / Art.PPU;
            return new Vector2(11.5f, crouching ? 8.5f : 14.5f) / Art.PPU;
        }

        /// <summary>
        /// Applies the pose, as the crew's figure plays it (<see cref="CrewFigure.Frame"/>). The owner flips <c>Visual</c> by
        /// facing (see <see cref="Actor.Render"/>). <paramref name="unarmed"/>: hands down, no gun (kie asleep in the coda).
        /// </summary>
        public void Apply(in AvatarPose p, bool unarmed = false)
        {
            CrewFrame f = CrewFigure.Frame(p, Time.unscaledTime);
            if (unarmed) f.Body = CrewBody.Unarmed;
            Sprite legs = Skin.Legs(f.Legs), torso = Skin.Body(f.Body);
            m_Legs.sprite = f.Flash ? Art.Flash(legs) : legs;
            m_Torso.sprite = f.Flash ? Art.Flash(torso) : torso;
            m_Torso.transform.localPosition = new Vector3(0f, f.Lift / Art.PPU, 0f);

            m_Flash.enabled = f.Muzzle;
            if (m_Flash.enabled)
            {
                // Visual is mirrored by facing, so work in right-facing local space.
                Vector2 aim = p.AimY == 0 ? Vector2.right : new Vector2(p.Diagonal ? 1f : 0f, p.AimY).normalized;
                Vector2 tip = MuzzleLocal(p.AimY, p.Crouching, p.Diagonal) + aim * (3f / Art.PPU);
                m_Flash.transform.localPosition = new Vector3(tip.x, tip.y, 0f);
            }

            m_Visual.gameObject.SetActive(!f.Hidden);
            m_Visual.localRotation = Quaternion.Euler(0f, 0f, f.Tilt);
        }
    }
}
