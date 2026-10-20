using System.Collections.Generic;
using UnityEngine;
using static PooketSlug.StageKit;

namespace PooketSlug
{
    /// <summary>
    /// The cutscenes' speech bubbles: one over each speaker's head for each line up now, kept on screen where the shot's
    /// camera has it, stacked comic-book style so none sits on another, and glitching for the Trollograms.
    /// </summary>
    public sealed class CutsceneBubbles
    {
        const float BubbleH = SpeechBubble.Height, BubbleGap = 4f;

        readonly RectTransform m_World;
        readonly CutsceneCast m_Cast;
        readonly List<SpeechBubble> m_Bubbles = new List<SpeechBubble>();
        readonly List<Rect> m_Placed = new List<Rect>(); // this frame's bubbles so far, so the next doesn't sit on them
        int m_Shown;

        public CutsceneBubbles(RectTransform world, CutsceneCast cast)
        {
            m_World = world;
            m_Cast = cast;
        }

        /// <summary>Starts a frame's bubbles: none yet.</summary>
        public void Begin()
        {
            m_Shown = 0;
            m_Placed.Clear();
        }

        /// <summary>Ends a frame's bubbles: hides the ones it didn't need.</summary>
        public void End()
        {
            for (int i = m_Shown; i < m_Bubbles.Count; i++) Ui.Show(m_Bubbles[i].Rt, false);
        }

        /// <summary>The next bubble this frame, saying <paramref name="line"/> (<paramref name="now"/> is the scene's clock).</summary>
        public void Show(CutsceneLine line, float now)
        {
            int i = m_Shown++;
            while (m_Bubbles.Count <= i) m_Bubbles.Add(new SpeechBubble(m_World, Vector2.zero));
            SpeechBubble b = m_Bubbles[i];
            bool glitchy = line.Who == Speaker.Goon || line.Who == Speaker.Hologram || line.Who == Speaker.TrolloKcaj;
            float w = glitchy ? b.Say(line.Text, Cyan, new Color(0.05f, 0.12f, 0.16f, 0.95f)) : b.Say(line.Text);

            StageFigure who = m_Cast.Figure(SceneScript.FigureOf(line.Who));
            Vector2 at = who.Head + new Vector2(0f, 16f);
            // kept on screen, where the shot's camera has it (its bubble shrinks with the zoom, so it's in world units)
            float zoom = m_World.localScale.x, half = w / 2f + 4f, view = StageW * Px / 2f / zoom;
            float middle = StageW * Px / 2f - m_World.anchoredPosition.x / zoom;
            at.x = Mathf.Clamp(at.x, middle - view + half, middle + view - half);
            // never on top of another bubble: comic-book style, a later one stacks above the ones already up, or below
            // them if there's no room left above in the shot
            float top = StageH * Px / 2f - m_World.anchoredPosition.y / zoom + StageH * Px / 2f / zoom;
            at.y = Stack(at.x, at.y, w, top);
            m_Placed.Add(new Rect(at.x - w / 2f, at.y, w, BubbleH));
            if (glitchy && Fx.Step(now, 12f, 5) == 0) at.x += 2f; // the holograms' bubbles glitch too
            b.Place(at, who.Head.x);
        }

        /// <summary>The height for a bubble at <paramref name="x"/> that clears every bubble already placed this frame.</summary>
        float Stack(float x, float y, float w, float top)
        {
            for (int step = 0; step < 8; step++)
            {
                float up = y + step * (BubbleH + BubbleGap);
                if (up + BubbleH <= top - 2f && Clear(x, up, w)) return up;
            }
            for (int step = 1; step < 8; step++)
            {
                float down = y - step * (BubbleH + BubbleGap);
                if (Clear(x, down, w)) return down;
            }
            return y;
        }

        bool Clear(float x, float y, float w)
        {
            foreach (Rect r in m_Placed)
                if (Mathf.Abs(x - r.center.x) < (w + r.width) / 2f + BubbleGap && Mathf.Abs(y - r.y) < BubbleH + BubbleGap)
                    return false;
            return true;
        }
    }
}
