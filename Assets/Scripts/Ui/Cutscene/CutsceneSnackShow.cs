using UnityEngine;
using UnityEngine.UI;
using static PooketSlug.StageKit;

namespace PooketSlug
{
    /// <summary>
    /// The skits' snack effects (<see cref="Staging.Things"/>), drawn as the shot's script has them (<see cref="ShotStage"/>):
    /// the soft serve splatting on whoever it's put on (<c>splat</c>: the burst, then dripping, timed by
    /// <see cref="SnackShow"/>), and the crisp packet's label close up (<c>label</c>: the room dimmed, the label coming up
    /// into view, big enough to read). A part of the cutscene view of its own, beside <see cref="CutsceneEffects"/>; its
    /// pieces are made by <see cref="CutsceneView"/>, in drawing order.
    /// </summary>
    public sealed class CutsceneSnackShow
    {
        /// <summary>The label close up is drawn this many times its art's pixels.</summary>
        const float LabelScale = 2f;

        readonly RectTransform m_World, m_Stage;
        Image m_Splat, m_Dim, m_Label;

        public CutsceneSnackShow(RectTransform world, RectTransform stage)
        {
            m_World = world;
            m_Stage = stage;
        }

        /// <summary>The splat, in the world, in front of the cast: drawn the figures' size, since it sits on one.</summary>
        public void MakeSplat()
        {
            Sprite first = CutsceneSnacks.Splat[0];
            m_Splat = Ui.Picture(m_World, "Splat", first, Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero,
                new Vector2(first.rect.width, first.rect.height) * FigureScale, Color.white);
            Ui.Show(m_Splat, false);
        }

        /// <summary>On the stage: the label's close-up, over a dimmed room.</summary>
        public void MakeLabel()
        {
            m_Dim = Ui.Cover(m_Stage, "Label dim", new Color(0.03f, 0.02f, 0.05f, 0.7f));
            Sprite label = CutsceneSnacks.Label;
            m_Label = Ui.Picture(m_Stage, "Label close-up", label, Ui.Middle, Ui.Middle, Vector2.zero,
                new Vector2(label.rect.width, label.rect.height) * Px * LabelScale, Color.white);
            Ui.Show(m_Dim, false);
            Ui.Show(m_Label, false);
        }

        /// <summary>Shows and animates both as the shot's script has them at <paramref name="t"/>.</summary>
        public void Draw(ShotStage stage, float t, float now, float ground)
        {
            Splat(stage, t, now, ground);
            Label(stage, t);
        }

        /// <summary>The splat, on the soldier it's put on (<c>splat on tones2 0 0</c>: at their feet), the way they face.</summary>
        void Splat(ShotStage stage, float t, float now, float ground)
        {
            bool on = stage.Shown("splat", t);
            Ui.Show(m_Splat, on);
            if (!on) return;
            m_Splat.sprite = CutsceneSnacks.Splat[SnackShow.SplatFrame(t - stage.ShownSince("splat", t))];
            Vector2 at = stage.Where("splat", t, now);
            m_Splat.rectTransform.anchoredPosition = new Vector2(at.x, ground + at.y) * Px;
            StageStep rides = stage.Latest("splat", t, "on");
            float facing = rides != null ? stage.Facing(rides.Word, t) : 1f;
            m_Splat.rectTransform.localScale = new Vector3(facing, 1f, 1f);
            m_Splat.color = stage.Colour("splat", t, now);
        }

        /// <summary>The label, close up: the room dimmed, the label coming up from below and settling, a little turned until it does.</summary>
        void Label(ShotStage stage, float t)
        {
            bool on = stage.Shown("label", t);
            Ui.Show(m_Dim, on);
            Ui.Show(m_Label, on);
            if (!on) return;
            float up = SnackShow.LabelUp(t - stage.ShownSince("label", t));
            m_Label.rectTransform.anchoredPosition = new Vector2(0f, Mathf.Lerp(-150f, 20f, up));
            m_Label.rectTransform.localRotation = Quaternion.Euler(0f, 0f, (1f - up) * 8f);
        }
    }
}
