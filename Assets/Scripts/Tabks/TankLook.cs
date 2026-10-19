using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// How your tabk looks (<see cref="PlayerTank"/>), whichever crewmate's it is (<see cref="TabkKit"/>): the hull, mirrored to
    /// face the way it drives, and the gun turning on the turret. Your own tabk and a partner's copy of theirs
    /// (<see cref="RemotePlayer"/>) draw with it.
    /// </summary>
    public sealed class TankLook
    {
        static readonly Color RunawayBlink = new Color(1f, 0.6f, 0.5f), MuzzleGlow = new Color(1f, 0.9f, 0.6f);

        readonly GameObject m_Root;
        readonly Transform m_HullRoot, m_BarrelPivot;
        readonly SpriteRenderer m_Hull, m_Barrel;
        readonly Sprite m_HullArt, m_HullSpun;
        readonly TabkKit m_Kit;

        /// <summary>A crewmate's tabk (its hull facing left, its gun pointing left from its pivot).</summary>
        public TankLook(Transform parent, int order, TabkKit kit)
        {
            m_Kit = kit;
            m_HullArt = kit.Hull;
            m_HullSpun = kit.HullSpun;
            Sprite hull = kit.Hull, barrel = kit.Barrel;
            m_Root = new GameObject("Tank look");
            m_Root.transform.SetParent(parent, false);
            m_HullRoot = new GameObject("Hull root").transform;
            m_HullRoot.SetParent(m_Root.transform, false);
            m_Hull = Renderer(m_HullRoot, "Hull", hull, order + 1);
            m_BarrelPivot = new GameObject("Turret").transform;
            m_BarrelPivot.SetParent(m_Root.transform, false);
            m_Barrel = Renderer(m_BarrelPivot, "Barrel", barrel, order);
        }

        static SpriteRenderer Renderer(Transform parent, string name, Sprite sprite, int order) => Art.Renderer(parent, name, sprite, order);

        /// <summary>Whose tabk it draws.</summary>
        public TabkKit Kit => m_Kit;

        public void Destroy() => Object.Destroy(m_Root);

        public bool Shown
        {
            set { if (m_Root.activeSelf != value) m_Root.SetActive(value); }
        }

        /// <summary>
        /// Draws it at <paramref name="offset"/> (world units, relative to the parent), facing <paramref name="facing"/>,
        /// with the gun at <paramref name="barrelAngle"/> degrees (0 right, counterclockwise); <paramref name="spin"/>
        /// turns its reels, on a tabk that has them.
        /// </summary>
        public void Apply(Vector2 offset, int facing, float barrelAngle, bool flash, bool muzzle, bool blink, bool spin = false)
        {
            m_Root.transform.localPosition = offset;
            m_HullRoot.localScale = new Vector3(-facing, 1f, 1f); // the art faces left
            Sprite hull = spin && m_HullSpun != null && (int)(Time.time * KieTuning.Tabk.ReelFrames) % 2 == 1 ? m_HullSpun : m_HullArt;
            m_Hull.sprite = flash ? Art.Flash(hull) : hull;
            m_Hull.color = blink ? RunawayBlink : Color.white;
            m_BarrelPivot.localPosition = m_Kit.PivotLocal(facing);
            m_BarrelPivot.localRotation = Quaternion.Euler(0f, 0f, barrelAngle - 180f); // the barrel's art points left
            m_Barrel.color = muzzle ? MuzzleGlow : Color.white;
        }
    }
}
