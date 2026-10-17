using UnityEngine;
using UnityEngine.UI;
using static PooketSlug.StageKit;

namespace PooketSlug
{
    /// <summary>
    /// The cutscenes' camera: the shot's own (in on a point, as the script has it), and a character's intro over it
    /// (<c>docs/design/story.md</c>, Character intros), whose crash zoom moves the world while their name slams up on a
    /// band over it.
    /// </summary>
    public sealed class CutsceneCamera
    {
        readonly RectTransform m_World, m_Stage;
        RectTransform m_IntroBand;
        Text m_IntroName;
        Image m_Flash;
        // the shot's camera: how far in, and the world point (from its middle, art pixels) it centres on
        float m_Zoom = 1f;
        Vector2 m_Look;

        public CutsceneCamera(RectTransform world, RectTransform stage)
        {
            m_World = world;
            m_Stage = stage;
        }

        /// <summary>A character's intro: a white flash, then their name on a slanted band (made in drawing order, by <see cref="CutsceneView"/>).</summary>
        public void MakeIntro()
        {
            m_Flash = Ui.Cover(m_Stage, "Flash", new Color(1f, 1f, 1f, 0f));
            m_IntroBand = Ui.Rect(m_Stage, "Intro", Ui.Middle, Ui.Middle, new Vector2(StageW * Px * 0.2f, 28f), new Vector2(StageW * Px * 0.62f, 74f));
            var introBack = m_IntroBand.gameObject.AddComponent<Image>();
            introBack.color = Band;
            introBack.raycastTarget = false;
            m_IntroBand.localRotation = Quaternion.Euler(0f, 0f, 6f);
            Ui.Picture(m_IntroBand, "Edge", White, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(StageW * Px * 0.62f, 5f), Gold);
            Ui.Picture(m_IntroBand, "Edge", White, new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(StageW * Px * 0.62f, 5f), Gold);
            m_IntroName = Ui.Label(m_IntroBand, "Name", Ui.Middle, Vector2.zero, 52, TextAnchor.MiddleCenter, Gold);
            var nameOutline = m_IntroName.gameObject.AddComponent<Outline>();
            nameOutline.effectColor = Ink;
            nameOutline.effectDistance = new Vector2(2.5f, -2.5f);
        }

        /// <summary>A new scene: the camera back out.</summary>
        public void Begin()
        {
            m_Zoom = 1f;
            m_Look = Vector2.zero;
        }

        /// <summary>Where the shot's script has the camera at <paramref name="t"/>.</summary>
        public void Aim(ShotStage stage, float t) => stage.Camera(t, out m_Zoom, out m_Look);

        /// <summary>
        /// Moves the world for the shot's camera, or for a character's intro (on <paramref name="who"/>; <paramref name="clock"/>
        /// is the scene's clock). First the build-up: the camera creeps in on them and the scene darkens a little as the
        /// riser climbs, with a shake as it peaks. Then it lands: the camera crash-zooms onto them, there's a white flash,
        /// their name slams up on a band, and it all eases back out as the slow-mo ends.
        /// </summary>
        public void Draw(CutscenePlayer p, StageFigure who, float clock)
        {
            const float Creep = 1.18f;
            CutsceneIntro intro = p.Current.Intro;
            bool on = p.Introducing && intro != null;
            float build = p.BuildingUp;
            Ui.Show(m_IntroBand, on);
            Vector2 half = new Vector2(StageW, StageH) * Px / 2f;
            Vector2 focus = who.Rt.anchoredPosition + new Vector2(0f, 16f * Px) - half; // their middle, from the world's centre
            if (!on)
            {
                // the build-up: a slow creep in on them (they stay put on screen), a darkening, and a shake at the peak
                float creep = Mathf.Lerp(1f, Creep, build * build);
                Vector2 shake = build > 0.8f ? new Vector2(Mathf.Sin(clock * 90f), Mathf.Cos(clock * 70f)) * (build - 0.8f) * 10f : Vector2.zero;
                if (build <= 0f)
                {
                    // the shot's own camera: centred on m_Look, m_Zoom in
                    m_World.localScale = new Vector3(m_Zoom, m_Zoom, 1f);
                    m_World.anchoredPosition = -m_Look * Px * m_Zoom;
                    m_Flash.color = new Color(0f, 0f, 0.05f, 0f);
                    return;
                }
                m_World.localScale = new Vector3(creep, creep, 1f);
                m_World.anchoredPosition = focus - focus * creep + shake;
                m_Flash.color = new Color(0f, 0f, 0.05f, 0.3f * build * build);
                return;
            }
            float k = p.IntroT, end = CutscenePlayer.IntroSeconds;
            float zoomIn = Mathf.Clamp01(k / 0.12f), zoomOut = Mathf.Clamp01((end - k) / 0.3f);
            float e = Mathf.Min(1f - (1f - zoomIn) * (1f - zoomIn), zoomOut * zoomOut * (3f - 2f * zoomOut));

            Vector2 target = new Vector2(-StageW * Px * 0.22f, -12f); // where they end up: left of centre, the name to their right
            float scale = Mathf.Lerp(k < end / 2f ? Creep : 1f, 2.6f, e); // on from the creep, back out to where it started
            m_World.localScale = new Vector3(scale, scale, 1f);
            m_World.anchoredPosition = Vector2.Lerp(focus, target, e) - focus * scale; // they stay where the creep had them

            m_Flash.color = new Color(1f, 1f, 1f, Mathf.Clamp01(0.85f - k / 0.18f));
            m_IntroName.text = intro.Name;
            float slam = Mathf.Clamp01((k - 0.08f) / 0.1f);
            float size = k < 0.08f ? 0f : Mathf.Lerp(2.4f, 1f, slam);
            m_IntroBand.localScale = new Vector3(size * zoomOut, size, 1f);
        }
    }
}
