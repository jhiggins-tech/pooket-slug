using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static PooketSlug.StageKit;

namespace PooketSlug
{
    /// <summary>
    /// What every skit on the house's screens (the den, the bay) is drawn with (<see cref="Skit"/>): a figure for each of
    /// its cast, small things they hold in a hand, and the speech bubble for the line that's up, over whoever says it.
    /// A screen's skit view stands and poses the figures as its script has them; this keeps the rest the same everywhere.
    /// Figures stand in the room's painting's pixels (<see cref="StageFigure.AtPlate"/>), as the room's own crew does.
    /// </summary>
    sealed class SkitStage
    {
        /// <summary>
        /// Where a hand is, from the figure's middle in plate pixels (as the den's slushie is held: <c>DenFigure</c>): the
        /// front hand held out with no gun (<see cref="StageTorso.Carry"/>), and the hand at their side.
        /// </summary>
        public static readonly Vector2 HandOut = new Vector2(7f, -3.5f), HandDown = new Vector2(5f, 1f);

        readonly RectTransform m_World;
        readonly Dictionary<SoldierDef, StageFigure> m_Figures = new Dictionary<SoldierDef, StageFigure>();
        readonly List<Image> m_Props = new List<Image>();
        readonly SpeechBubble m_Bubble;

        /// <summary>Made in the screen's drawing order: its figures and props in front of the room, the bubble over them.</summary>
        public SkitStage(RectTransform world)
        {
            m_World = world;
            m_Bubble = new SpeechBubble(world, Vector2.zero);
        }

        /// <summary>
        /// <paramref name="s"/>'s figure, shown, upright and untinted (made the first time). Figures are drawn in the order
        /// they're first asked for, back to front.
        /// </summary>
        public StageFigure Figure(SoldierDef s)
        {
            if (!m_Figures.TryGetValue(s, out StageFigure f))
            {
                f = new StageFigure(m_World, "Skit " + s.Name);
                m_Figures[s] = f;
            }
            Ui.Show(f.Rt, true);
            f.Rt.localRotation = Quaternion.identity;
            f.Tint(Color.white);
            return f;
        }

        /// <summary>
        /// Stands <paramref name="f"/> as <paramref name="who"/> in <paramref name="pose"/>, feet at <paramref name="x"/>,
        /// <paramref name="y"/> (plate pixels), facing left or right.
        /// </summary>
        public static void Stand(StageFigure f, SoldierDef who, StagePose pose, float x, float y, bool faceLeft)
        {
            f.Pose(Art.SkinFor(who), pose);
            f.AtPlate(x, y);
            f.Rt.localScale = new Vector3(faceLeft ? -1f : 1f, 1f, 1f);
        }

        /// <summary>A prop: a sprite (or a plain block for null), <paramref name="w"/> by <paramref name="h"/> plate pixels, hidden till placed.</summary>
        public Image Prop(string name, Sprite sprite, float w, float h, Color colour)
        {
            Image prop = Picture(m_World, name, sprite, 0f, 0f, w, h, colour);
            prop.raycastTarget = false;
            m_Props.Add(prop);
            Ui.Show(prop, false);
            return prop;
        }

        /// <summary>Puts <paramref name="prop"/> in the world, its bottom middle at <paramref name="x"/>, <paramref name="y"/> (plate pixels), upright.</summary>
        public void Place(Image prop, float x, float y)
        {
            RectTransform rt = prop.rectTransform;
            if (rt.parent != m_World) rt.SetParent(m_World, false);
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(x, y) * Px;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
            rt.SetAsLastSibling();
            Ui.Show(prop, true);
        }

        /// <summary>
        /// <paramref name="prop"/> in <paramref name="f"/>'s hand: <paramref name="at"/> from the figure's middle (plate
        /// pixels, <see cref="HandOut"/> or <see cref="HandDown"/>), so it turns, flips and moves with them.
        /// </summary>
        public static void Hold(Image prop, StageFigure f, Vector2 at)
        {
            RectTransform rt = prop.rectTransform;
            if (rt.parent != f.Rt) rt.SetParent(f.Rt, false);
            rt.anchorMin = rt.anchorMax = Ui.Middle;
            rt.pivot = Ui.Middle;
            rt.anchoredPosition = at * Px;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
            rt.SetAsLastSibling();
            Ui.Show(prop, true);
        }

        SoldierDef m_Lying;
        Vector2 m_LyingHead;

        /// <summary>
        /// <paramref name="who"/>'s head is at <paramref name="x"/>, <paramref name="y"/> (plate pixels) this frame, not over
        /// their feet: they're lying down. Their bubble points there.
        /// </summary>
        public void HeadAt(SoldierDef who, float x, float y)
        {
            m_Lying = who;
            m_LyingHead = new Vector2(x, y) * Px;
        }

        /// <summary>The line that's up in <paramref name="skit"/>, in a bubble over whoever says it (or none).</summary>
        public void Speak(Skit skit)
        {
            SkitLine? line = skit.Line;
            SoldierDef lying = m_Lying;
            m_Lying = null;
            if (line == null || !m_Figures.TryGetValue(line.Value.Who, out StageFigure f) || !f.Rt.gameObject.activeSelf)
            {
                m_Bubble.Hide();
                return;
            }
            Vector2 head = lying == line.Value.Who ? m_LyingHead : f.Head;
            float w = m_Bubble.Say(line.Value.Text);
            Vector2 at = head + new Vector2(0f, 6f);
            float half = w / 2f + 4f;
            at.x = Mathf.Clamp(at.x, half, StageW * Px - half);
            at.y = Mathf.Min(at.y, (StageH - 4f) * Px - SpeechBubble.Height);
            m_Bubble.Rt.SetAsLastSibling();
            m_Bubble.Place(at, head.x);
        }

        /// <summary>The skit's over, or not started: nothing of it shows.</summary>
        public void Hide()
        {
            foreach (StageFigure f in m_Figures.Values) Ui.Show(f.Rt, false);
            foreach (Image p in m_Props) Ui.Show(p, false);
            m_Bubble.Hide();
        }
    }
}
