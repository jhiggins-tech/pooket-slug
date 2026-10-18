using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// The ice on a soldier or goon frozen by nitrogen (<see cref="Frost"/>): a pale block over their body with a glint
    /// running down it, and a burst of chips as it cracks off. Cosmetic: drawn wherever the actor is drawn.
    /// </summary>
    public sealed class FrostLook
    {
        static readonly Color Ice = new Color(0.7f, 0.9f, 1f, 0.55f), Glint = new Color(0.95f, 1f, 1f, 0.85f), Chips = new Color(0.82f, 0.94f, 1f);
        readonly SpriteRenderer m_Block, m_Glint;
        bool m_Was;

        public FrostLook(Transform parent)
        {
            m_Block = Art.Renderer(parent, "Ice", Art.Pixel, SortOrder.Effects - 2, shown: false);
            m_Block.color = Ice;
            m_Glint = Art.Renderer(parent, "Ice glint", Art.Pixel, SortOrder.Effects - 1, shown: false);
            m_Glint.color = Glint;
        }

        /// <summary>Over <paramref name="a"/>, if <paramref name="frozen"/>; chips fly the frame it thaws.</summary>
        public void Render(Actor a, bool frozen)
        {
            m_Block.enabled = m_Glint.enabled = frozen;
            if (frozen)
            {
                float w = a.HalfW * 2f + 0.25f, h = a.Height + 0.2f;
                m_Block.transform.localPosition = new Vector3(0f, h * 0.5f - 0.05f, 0f);
                m_Block.transform.localScale = new Vector3(w * Art.PPU, h * Art.PPU, 1f);
                float t = Mathf.Repeat(a.World.Elapsed * 0.8f, 1f); // the glint slides down the block
                m_Glint.transform.localPosition = new Vector3(-w * 0.25f, h * (1f - t) - 0.05f, 0f);
                m_Glint.transform.localScale = new Vector3(2f, Mathf.Min(h, 0.5f) * Art.PPU, 1f);
            }
            else if (m_Was) a.World.Burst(a.Center, 12, Chips);
            m_Was = frozen;
        }
    }
}
