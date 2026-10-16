using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// How your tank looks (<see cref="PlayerTank"/>), in whatever paint it's given: the hull, mirrored to face the way it drives, and the gun turning on
    /// the turret. Your own tank and a partner's copy of theirs (<see cref="RemotePlayer"/>) draw with it.
    /// </summary>
    public sealed class TankLook
    {
        static readonly Color RunawayBlink = new Color(1f, 0.6f, 0.5f), MuzzleGlow = new Color(1f, 0.9f, 0.6f);

        readonly GameObject m_Root;
        readonly Transform m_HullRoot, m_BarrelPivot;
        readonly SpriteRenderer m_Hull, m_Barrel;
        readonly Sprite m_HullArt;

        /// <summary>A tank in this paint (<paramref name="hull"/> facing left, <paramref name="barrel"/> pointing left from its pivot).</summary>
        public TankLook(Transform parent, int order, Sprite hull, Sprite barrel)
        {
            m_HullArt = hull;
            m_Root = new GameObject("Tank look");
            m_Root.transform.SetParent(parent, false);
            m_HullRoot = new GameObject("Hull root").transform;
            m_HullRoot.SetParent(m_Root.transform, false);
            m_Hull = Renderer(m_HullRoot, "Hull", hull, order + 1);
            m_BarrelPivot = new GameObject("Turret").transform;
            m_BarrelPivot.SetParent(m_Root.transform, false);
            m_Barrel = Renderer(m_BarrelPivot, "Barrel", barrel, order);
        }

        static SpriteRenderer Renderer(Transform parent, string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>Where the barrel turns, from the tank's feet: the middle of the turret (the art faces left, so it's mirrored).</summary>
        public static Vector2 PivotLocal(int facing) => new Vector2(-facing * 4.5f / Art.PPU, 17.5f / Art.PPU);

        public bool Shown
        {
            set { if (m_Root.activeSelf != value) m_Root.SetActive(value); }
        }

        /// <summary>
        /// Draws it at <paramref name="feet"/> (world units, relative to the parent), facing <paramref name="facing"/>,
        /// with the gun at <paramref name="barrelAngle"/> degrees (0 right, counterclockwise).
        /// </summary>
        public void Apply(Vector2 offset, int facing, float barrelAngle, bool flash, bool muzzle, bool blink)
        {
            m_Root.transform.localPosition = offset;
            m_HullRoot.localScale = new Vector3(-facing, 1f, 1f); // the art faces left
            m_Hull.sprite = flash ? Art.Flash(m_HullArt) : m_HullArt;
            m_Hull.color = blink ? RunawayBlink : Color.white;
            m_BarrelPivot.localPosition = PivotLocal(facing);
            m_BarrelPivot.localRotation = Quaternion.Euler(0f, 0f, barrelAngle - 180f); // the barrel's art points left
            m_Barrel.color = muzzle ? MuzzleGlow : Color.white;
        }
    }
}
