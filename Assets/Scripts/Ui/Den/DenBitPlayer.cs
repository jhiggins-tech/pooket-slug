using System.Collections.Generic;
using UnityEngine;
using static PooketSlug.DenRoom;
using static PooketSlug.StageKit;

namespace PooketSlug
{
    /// <summary>
    /// The den's ambient bits (<see cref="DenBits"/>): every so often, while nobody's heading into the mission, two of the
    /// crew do one, the first walking over to the other (beside them at their station, or to the couch's arm) and the two
    /// trading lines in speech bubbles. A bit ends when it's done, when either of them is picked or leaves, or when the
    /// camera zooms in.
    /// </summary>
    sealed class DenBitPlayer
    {
        readonly RectTransform m_World;
        readonly DenCrew m_Crew;
        readonly System.Random m_Rng;
        readonly List<SpeechBubble> m_Bubbles = new List<SpeechBubble>();
        DenBit m_Bit, m_LastBit;
        DenActor m_BitA, m_BitB;
        float m_BitT, m_NextBit;
        bool m_BitSaidHolo;

        public DenBitPlayer(RectTransform world, DenCrew crew, System.Random rng)
        {
            m_World = world;
            m_Crew = crew;
            m_Rng = rng;
        }

        /// <summary>The slushie bit's playing (it has props: tones2's cup, kie's Trollogram, the splash).</summary>
        public bool Slushie => m_Bit != null && m_Bit.Id == "slushie";
        /// <summary>Seconds into the bit playing.</summary>
        public float T => m_BitT;
        /// <summary>The one who walked over, in the bit playing, or null.</summary>
        public DenActor First => m_BitA;

        /// <summary>The den's opened: no bit, and the first one comes sooner than the rest.</summary>
        public void Begin()
        {
            m_Bit = null;
            m_NextBit = Mathf.Lerp(DenBits.MinGap, DenBits.MaxGap, (float)m_Rng.NextDouble()) * 0.5f; // the first comes sooner
        }

        /// <summary>The den's closed: no bit.</summary>
        public void Stop() => m_Bit = null;

        /// <summary>Ends the bit playing if <paramref name="a"/> is in it (they've been picked).</summary>
        public void EndIfIn(DenActor a)
        {
            if (m_Bit != null && (a == m_BitA || a == m_BitB)) EndBit();
        }

        public void Tick(float dt, Den den)
        {
            if (m_Bit == null)
            {
                HideBubbles(0);
                if (den.Zoom >= 0f) return;
                m_NextBit -= dt;
                if (m_NextBit > 0f) return;
                m_NextBit = Mathf.Lerp(DenBits.MinGap, DenBits.MaxGap, (float)m_Rng.NextDouble());
                var idle = new List<SoldierDef>();
                var seated = new List<SoldierDef>();
                foreach (DenActor a in m_Crew.Actors)
                {
                    if (a.Doing == DenDoing.Station) idle.Add(a.Soldier);
                    else if (a.OnCouch) seated.Add(a.Soldier);
                }
                DenBit bit = DenBits.Choose(idle, seated, m_LastBit, m_Rng);
                if (bit == null) return;
                m_Bit = m_LastBit = bit;
                m_BitA = m_Crew.Find(bit.First);
                m_BitB = m_Crew.Find(bit.Second);
                m_BitT = 0f;
                m_BitSaidHolo = false;
                m_BitA.Doing = DenDoing.Bit;
                // walk over to the other one: beside them at their station, or to the couch's arm
                float to = bit.OnCouch ? CouchArms[m_BitB.Seat == 0 ? 0 : 1] : m_BitB.X + (m_BitB.X > m_BitA.X ? -16f : 16f);
                m_Crew.Send(m_BitA, to);
                return;
            }
            m_BitT += dt;
            if (m_Bit.Id == "slushie" && !m_BitSaidHolo && m_BitT > 2.6f)
            {
                m_BitSaidHolo = true;
                Sfx.Play(Sfx.Glitch, 0.4f);
            }
            if (m_Bit.Id == "slushie" && m_BitT > 3.5f && m_BitT - dt <= 3.5f) Sfx.Play(Sfx.Splat, 0.5f);
            // the walk there doesn't count against the lines: they start once they've arrived
            float bX = m_BitB.OnCouch ? SeatX[m_BitB.Seat] : m_BitB.X;
            if (!m_BitA.Walk.Standing && m_BitT < 1f) m_BitT = Mathf.Min(m_BitT, 0.5f);
            else m_BitA.Walk.Face(bX < m_BitA.X);
            int shown = 0;
            foreach (DenLine line in m_Bit.Lines)
            {
                if (m_BitT < line.At || m_BitT > line.At + 2.2f) continue;
                Bubble(shown++, line.Text, line.Who == 0 ? m_BitA : m_BitB);
            }
            HideBubbles(shown);
            bool partnerGone = m_BitB.Doing == DenDoing.Away || (!m_Bit.OnCouch && m_BitB.Doing != DenDoing.Station) || (m_Bit.OnCouch && !m_BitB.OnCouch);
            if (m_BitT >= m_Bit.Seconds || partnerGone || den.Zoom >= 0f) EndBit();
        }

        void EndBit()
        {
            DenActor a = m_BitA;
            m_Bit = null;
            m_BitA = m_BitB = null;
            HideBubbles(0);
            if (a != null && a.Doing == DenDoing.Bit)
            {
                a.Doing = DenDoing.Walking;
                a.Walk.WalkTo(a.Home.Floor, a.Home.Spot);
            }
        }

        void Bubble(int i, string text, DenActor who)
        {
            while (m_Bubbles.Count <= i) m_Bubbles.Add(new SpeechBubble(m_World, Vector2.zero));
            SpeechBubble b = m_Bubbles[i];
            b.Rt.SetAsLastSibling();
            float w = b.Say(text);
            bool seated = who.OnCouch;
            Vector2 head = new Vector2(seated ? SeatX[who.Seat] : who.X, (seated ? SitY : who.Y) + 30f) * Px;
            Vector2 at = head + new Vector2(0f, 16f);
            float half = w / 2f + 4f;
            at.x = Mathf.Clamp(at.x, half, StageW * Px - half);
            b.Place(at, head.x);
        }

        void HideBubbles(int from)
        {
            for (int i = from; i < m_Bubbles.Count; i++) m_Bubbles[i].Hide();
        }
    }
}
