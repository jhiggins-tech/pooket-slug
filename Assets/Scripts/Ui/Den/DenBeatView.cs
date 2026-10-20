using UnityEngine;
using UnityEngine.UI;
using static PooketSlug.DenRoom;
using static PooketSlug.StageKit;

namespace PooketSlug
{
    /// <summary>
    /// The den's beats, drawn (<see cref="DenBeat"/>, in Core): on the first visit, the raid's wreck over the den, fading
    /// as the room warms up, with kie heaving at the couch and tones2 down at the TV's plug; moving in, the newcomer under
    /// the roller door and walking in, their corner lighting up. The crewmates in it are drawn here, and hidden in
    /// <see cref="DenCrew"/> while it plays; the line is a speech bubble.
    /// </summary>
    sealed class DenBeatView
    {
        /// <summary>Where kie heaves the couch from (by its left arm) and where tones2 kneels at the TV's plug, on the ground floor.</summary>
        public const float HeaveX = 76f, PlugX = 162f;

        readonly RectTransform m_World;
        readonly Image m_Raid;
        readonly StageFigure m_A, m_B;
        readonly SpeechBubble m_Bubble;
        Image m_Layer;

        /// <summary>Made in drawing order (<see cref="DenView"/>): over the room and the TV, under the taps.</summary>
        public DenBeatView(RectTransform world)
        {
            m_World = world;
            m_Raid = Picture(world, "Raid", Painting("raid-den"), 0f, 0f, StageW, StageH, Color.white);
            m_Raid.rectTransform.pivot = Vector2.zero;
            m_Raid.raycastTarget = false;
            m_A = new StageFigure(world, "Beat A");
            m_B = new StageFigure(world, "Beat B");
            m_Bubble = new SpeechBubble(world, Vector2.zero);
            Hide();
        }

        /// <summary>Starts drawing <paramref name="beat"/>: a newcomer's moved-in layer (in <paramref name="paintings"/>) starts dark.</summary>
        public void Begin(DenBeat beat, RectTransform paintings)
        {
            m_Layer = null;
            if (beat.Newcomer != null)
            {
                Transform layer = paintings.Find(beat.Newcomer.Name + " moved in");
                if (layer != null) m_Layer = layer.GetComponent<Image>();
            }
        }

        /// <summary>The beat's over: the room as it is.</summary>
        public void Hide()
        {
            Ui.Show(m_Raid, false);
            Ui.Show(m_A.Rt, false);
            Ui.Show(m_B.Rt, false);
            m_Bubble.Hide();
            if (m_Layer != null) m_Layer.color = Color.white;
            m_Layer = null;
        }

        /// <summary>Draws <paramref name="beat"/> now; <paramref name="speaker"/> is where the line comes from, if not one of the beat's own figures.</summary>
        public void Draw(DenBeat beat, float clock, Vector2? speaker)
        {
            Vector2 head;
            if (beat.Kind == DenBeatKind.FirstVisit)
            {
                Ui.Show(m_Raid, beat.Warm < 1f);
                m_Raid.color = new Color(1f, 1f, 1f, 1f - beat.Warm);
                m_Raid.rectTransform.SetAsLastSibling(); // over the TV's words, which aren't on yet
                Heave(m_A, beat, clock);
                Plug(m_B, beat);
                m_A.Rt.SetAsLastSibling();
                m_B.Rt.SetAsLastSibling();
                head = m_A.Head;
            }
            else
            {
                Ui.Show(m_Raid, false);
                Ui.Show(m_B.Rt, false);
                MoveIn(m_A, beat, clock);
                m_A.Rt.SetAsLastSibling();
                if (m_Layer != null) m_Layer.color = new Color(1f, 1f, 1f, beat.Lit);
                head = speaker.HasValue ? (speaker.Value + new Vector2(0f, 30f)) * Px : m_A.Head;
            }
            if (!beat.Speaking)
            {
                m_Bubble.Hide();
                return;
            }
            m_Bubble.Rt.SetAsLastSibling();
            float w = m_Bubble.Say(beat.Line);
            Vector2 at = head + new Vector2(0f, 16f);
            float half = w / 2f + 4f;
            at.x = Mathf.Clamp(at.x, half, StageW * Px - half);
            m_Bubble.Place(at, head.x);
        }

        /// <summary>kie by the couch's arm, heaving it back up: leaning in, straining, then standing back.</summary>
        void Heave(StageFigure f, DenBeat beat, float clock)
        {
            PlayerSkin skin = Art.SkinFor(Content.Kie, 0);
            Ui.Show(f.Rt, true);
            bool heaving = beat.Heave > 0f && beat.Heave < 1f;
            f.Pose(skin, new StagePose(StageLegs.Idle, heaving ? StageTorso.Lounge : StageTorso.Unarmed, 0f, heaving ? -1f : 0f));
            float strain = heaving ? Fx.Shiver(clock, 18f, 0.5f) : 0f;
            f.At(HeaveX + strain, DenMap.FloorY(DenFloor.Ground));
            f.Rt.localScale = Vector3.one; // facing the couch, to his right
            f.Tint(Color.white);
        }

        /// <summary>tones2 down at the TV's plug, then up again once it's in.</summary>
        void Plug(StageFigure f, DenBeat beat)
        {
            PlayerSkin skin = Art.SkinFor(Content.Tones2, 0);
            Ui.Show(f.Rt, true);
            bool down = beat.Plug > 0f && beat.Plug < 1f;
            f.Pose(skin, new StagePose(down ? StageLegs.Jump : StageLegs.Idle, down ? StageTorso.Lounge : StageTorso.Unarmed, 0f, down ? -3f : 0f));
            f.At(PlugX, DenMap.FloorY(DenFloor.Ground));
            f.Rt.localScale = new Vector3(-1f, 1f, 1f); // facing the TV, to his left
            f.Tint(Color.white);
        }

        /// <summary>The newcomer: out of the dark under the door, then walking in, looking round.</summary>
        void MoveIn(StageFigure f, DenBeat beat, float clock)
        {
            PlayerSkin skin = Art.SkinFor(beat.Newcomer, 0);
            Ui.Show(f.Rt, true);
            Vector2 sill = DenRollerDoor.Sill;
            float through = beat.Through, x = NewcomerX(beat);
            bool walking = through < 1f && beat.Walked < 1f;
            f.Pose(skin, StagePose.Walking(walking, clock));
            f.At(x, Mathf.Lerp(DenMap.FloorY(DenFloor.Ground), sill.y, through));
            Color shade = Color.Lerp(Color.white, new Color(0.15f, 0.15f, 0.2f), through);
            f.Tint(shade);
            float size = 1f - 0.2f * through;
            bool lookBack = beat.Walked >= 1f && Fx.Blink(clock, 1.5f); // looking round their corner
            f.Rt.localScale = new Vector3((lookBack ? 1f : -1f) * size, size, 1f);
        }

        /// <summary>Where the newcomer is along the ground floor now.</summary>
        public static float NewcomerX(DenBeat beat) => DenRollerDoor.Sill.x - DenBeat.WalkIn * beat.Walked;
    }
}
