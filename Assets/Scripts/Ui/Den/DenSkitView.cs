using UnityEngine;
using UnityEngine.UI;
using static PooketSlug.DenRoom;
using static PooketSlug.StageKit;

namespace PooketSlug
{
    /// <summary>
    /// The den's skits, drawn (<see cref="Skit"/>, in Core; <c>docs/design/cutscenes/skits.md</c>): Two of us now on and by
    /// the couch, Special delivery with the cargo drone flying kie in under the roller door, and A Trollogram of anything
    /// across the den's two floors. Its cast is drawn here and stands aside in <see cref="DenCrew"/> while it plays, from
    /// where they were, so when it's done they're back where they were. The den's own pieces do the rest: the roller door,
    /// the couch's loose controller and the co-op button (<see cref="DenInfo"/>), and the TV.
    /// </summary>
    sealed class DenSkitView
    {
        static float Ground => DenMap.FloorY(DenFloor.Ground);
        static float Upper => DenMap.FloorY(DenFloor.Upper);

        /// <summary>
        /// Two of us now: where tones2 and kie set off from (where the first visit leaves them: <see cref="DenBeat.PlugX"/>,
        /// <see cref="DenBeat.HeaveX"/>), where kie stands (just past the couch's right arm) and where he perches on it.
        /// </summary>
        const float Tones2From = DenBeat.PlugX, KieFrom = DenBeat.HeaveX, KieStand = 176f, KieArmX = 162f, ArmY = SitY + 5f;
        /// <summary>Special delivery: kie hangs this far under the drone's middle, his feet this far behind it, and lies on the couch at this height.</summary>
        const float HangDrop = 17f, HangBack = 14f;
        /// <summary>A Trollogram of anything: kcaj at his nook's rail (his station, the string board up to his right), kie at his bench, tones2 at his slushie machine.</summary>
        const float KcajRail = 60f, KieBench = 64f, Tones2Machine = 192f;

        readonly RectTransform m_World;
        readonly SkitStage m_Stage;
        readonly StageFigure m_Kcaj, m_Tones2, m_Kie;
        readonly Image m_Drone, m_Cable, m_Pad, m_Remote, m_Bag, m_Cup, m_Note;
        readonly Text m_Zz;

        /// <summary>Made in drawing order (<see cref="DenView"/>): over the room and the den's beats, under the taps.</summary>
        public DenSkitView(RectTransform world)
        {
            m_World = world;
            m_Stage = new SkitStage(world);
            m_Kcaj = m_Stage.Figure(Content.Kcaj); // up on the upper floor, at the back
            m_Tones2 = m_Stage.Figure(Content.Tones2);
            m_Kie = m_Stage.Figure(Content.Kie);
            Sprite drone = Tones2Art.TabkDrone[0];
            m_Cable = m_Stage.Prop("Skit cable", null, 1f, 1f, new Color(0.15f, 0.13f, 0.16f));
            m_Drone = m_Stage.Prop("Skit drone", drone, drone.rect.width, drone.rect.height, Color.white);
            m_Pad = m_Stage.Prop("Skit controller", null, 6f, 3f, Pad); // the couch's loose controller, picked up
            m_Remote = m_Stage.Prop("Skit remote", null, 3f, 4f, new Color(0.24f, 0.24f, 0.28f));
            m_Bag = m_Stage.Prop("Skit bag", CutsceneSnacks.Packet, 6f, 7f, Color.white);
            m_Cup = m_Stage.Prop("Skit slushie", CutsceneSnacks.Tiny, 3f, 5f, Color.white);
            m_Note = m_Stage.Prop("Skit note", null, 3f, 3f, new Color(1f, 0.92f, 0.35f)); // kcaj's sticky note, as in his idle
            m_Zz = Label(world, "Skit zz", 0f, 0f, 14, TextAnchor.MiddleCenter, Paper);
            m_Zz.text = "z Z";
            Hide();
        }

        /// <summary>Nothing of a skit shows.</summary>
        public void Hide()
        {
            m_Stage.Hide();
            Ui.Show(m_Zz, false);
        }

        /// <summary>Draws <paramref name="skit"/> now (<paramref name="clock"/> is the den's, for the walk and the rotors).</summary>
        public void Draw(Skit skit, float clock)
        {
            switch (skit)
            {
                case TwoOfUsSkit s: TwoOfUs(s, clock); break;
                case SpecialDeliverySkit s: SpecialDelivery(s, clock); break;
                case TrollogramSkit s: Trollogram(s, clock); break;
            }
            m_Stage.Speak(skit);
        }

        // ------------------------------------------------------------------ Two of us now

        /// <summary>
        /// tones2 walks over and flops into his seat, gaming; kie walks over past the right arm and picks up the controller
        /// off the empty seat, looks at it, turns to us and holds it out; tones2's hand goes up; kie tosses it at
        /// the camera (it flies at us, growing, and is gone) and drops onto the couch's arm. The button popping up where the
        /// controller lay is the den's own (<see cref="DenConsole"/>).
        /// </summary>
        void TwoOfUs(TwoOfUsSkit s, float clock)
        {
            Ui.Show(m_Kcaj.Rt, false);
            Ui.Show(m_Zz, false);
            // tones2: to his seat, then on it with his controller (the gaming top holds it), a hand up when he's right here
            StageFigure t2 = m_Stage.Figure(Content.Tones2);
            if (s.Flop < 1f)
            {
                float x = Mathf.Lerp(Tones2From, SeatX[0], s.Flop);
                SkitStage.Stand(t2, Content.Tones2, StagePose.Walking(s.Flop > 0f, clock), x, Ground, true);
            }
            else
            {
                StageTorso top = s.HandUp ? StageTorso.Lounge : StageTorso.Pad;
                SkitStage.Stand(t2, Content.Tones2, new StagePose(StageLegs.Sit, top, 0f, -CrewFigure.CrouchDrop), SeatX[0], SitY, false);
            }

            // kie: at the right arm, facing the empty seat; the controller in his hand; facing us with it held out; the toss; perched
            StageFigure kie = m_Stage.Figure(Content.Kie);
            if (s.Perched)
                SkitStage.Stand(kie, Content.Kie, new StagePose(StageLegs.Sit, StageTorso.Unarmed, 0f, -CrewFigure.CrouchDrop + (Fx.Step(clock, 2.5f, 2) == 0 ? 0f : 0.5f)), KieArmX, ArmY, true);
            else if (s.KieWalk < 1f)
                SkitStage.Stand(kie, Content.Kie, StagePose.Walking(s.KieWalk > 0f, clock), Mathf.Lerp(KieFrom, KieStand, s.KieWalk), Ground, false);
            else
            {
                bool followThrough = s.Toss >= 0f && s.Toss < 1f;
                StageTorso top = followThrough ? StageTorso.Lounge : s.Holding ? StageTorso.Carry : StageTorso.Unarmed;
                SkitStage.Stand(kie, Content.Kie, new StagePose(StageLegs.Idle, top), KieStand, Ground, !s.HoldingOut);
            }
            if (s.Holding)
            {
                SkitStage.Hold(m_Pad, kie, SkitStage.HandOut);
                // held out at arm's length, at us: nearer the camera, so a little bigger
                float big = s.HoldingOut ? 1.5f : 1f;
                m_Pad.rectTransform.localScale = new Vector3(big, big, 1f);
            }
            else if (s.Toss >= 0f && s.Toss < 1f)
            {
                // it leaves his hand and flies at the camera: bigger and bigger, down past the front of the screen
                float k = s.Toss;
                m_Stage.Place(m_Pad, Mathf.Lerp(KieStand + 8f, 196f, k), Mathf.Lerp(Ground + 14f, -30f, k * k));
                float size = Mathf.Lerp(1.5f, 9f, k * k);
                m_Pad.rectTransform.localScale = new Vector3(size, size, 1f);
                m_Pad.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -200f * k);
            }
            else Ui.Show(m_Pad, false);
            Ui.Show(m_Remote, false);
            Ui.Show(m_Bag, false);
            Ui.Show(m_Drone, false);
            Ui.Show(m_Cable, false);
            Ui.Show(m_Cup, false);
            Ui.Show(m_Note, false);
        }

        // ------------------------------------------------------------------ Special delivery

        /// <summary>
        /// tones2 alone on the couch with the drone's remote, the TV off, watching the roller door; it rattles up, and the
        /// cargo drone flies in under it with kie hanging off its cable by the belt, face down, asleep, the snack bag in his
        /// hand; it carries him over the couch, lowers him onto it and goes back out; kie rolls over hugging the bag; tones2
        /// puts the remote down and turns the TV on.
        /// </summary>
        void SpecialDelivery(SpecialDeliverySkit s, float clock)
        {
            Ui.Show(m_Kcaj.Rt, false);
            Ui.Show(m_Pad, false);
            Ui.Show(m_Cup, false);
            Ui.Show(m_Note, false);

            // tones2 on the couch, facing the door; the remote in his hand, then down on the cushion beside him
            StageFigure t2 = m_Stage.Figure(Content.Tones2);
            StageTorso top = s.Remote ? StageTorso.Carry : StageTorso.Unarmed;
            SkitStage.Stand(t2, Content.Tones2, new StagePose(StageLegs.Sit, top, 0f, -CrewFigure.CrouchDrop), SeatX[0], SitY, false);
            if (s.Remote) SkitStage.Hold(m_Remote, t2, SkitStage.HandOut + new Vector2(0f, -CrewFigure.CrouchDrop * Staging.FigureGrow));
            else m_Stage.Place(m_Remote, SeatX[0] + 11f, SeatY);

            // the drone, in under the door and out again (dark in the doorway), its rotors turning
            (float dx, float dy) = s.Drone;
            float shade = Mathf.Clamp01((dx - (DenMap.RollerX + 4f)) / 14f);
            Color tint = Color.Lerp(Color.white, new Color(0.15f, 0.15f, 0.2f), shade);
            bool droneIn = s.DroneIn;
            if (droneIn)
            {
                m_Stage.Place(m_Drone, dx, dy - m_Drone.rectTransform.sizeDelta.y / Px / 2f);
                m_Drone.sprite = Tones2Art.TabkDrone[Fx.Rotor.Step(clock)];
                m_Drone.color = tint;
            }
            else Ui.Show(m_Drone, false);

            // kie: hanging face down under it, then lying on the couch where it put him, then rolled over onto his back
            StageFigure kie = m_Stage.Figure(Content.Kie);
            if (!s.KieIn)
            {
                Ui.Show(kie.Rt, false);
                Ui.Show(m_Bag, false);
                Ui.Show(m_Cable, false);
                Ui.Show(m_Zz, false);
                return;
            }
            float lieY = s.Hanging ? dy - HangDrop : SpecialDeliverySkit.LowY - HangDrop;
            float feetX = (s.Hanging ? dx : SpecialDeliverySkit.OverX) - HangBack;
            // the crouch, toppled flat about the feet: turned a quarter clockwise facing right is face down, head to the
            // right; facing left it's on his back, the same way round
            bool onBack = s.RolledOver;
            SkitStage.Stand(kie, Content.Kie, new StagePose(StageLegs.Crouch, StageTorso.Carry), feetX, lieY, onBack);
            kie.Rt.localRotation = Quaternion.Euler(0f, 0f, -90f);
            kie.Tint(s.Hanging ? tint : Color.white);
            SkitStage.Hold(m_Bag, kie, SkitStage.HandOut);
            m_Bag.color = s.Hanging ? tint : Color.white;
            float length = 30f * Staging.FigureGrow; // feet to the top of his head, lying
            m_Stage.HeadAt(Content.Kie, feetX + length - 6f, lieY + 4f);

            // the cable, from the drone's belly down to his belt
            bool cable = droneIn && s.Hanging;
            Ui.Show(m_Cable, cable);
            if (cable)
            {
                float top2 = dy - 5f, bottom = lieY + 4f;
                m_Stage.Place(m_Cable, dx, bottom);
                m_Cable.rectTransform.sizeDelta = new Vector2(1f, Mathf.Max(0f, top2 - bottom)) * Px;
                m_Cable.color = Color.Lerp(new Color(0.15f, 0.13f, 0.16f), Color.black, shade);
                m_Drone.rectTransform.SetAsLastSibling();
            }

            // z Z, drifting up off him, over and over
            Ui.Show(m_Zz, true);
            float k = Mathf.Repeat(s.T - SpecialDeliverySkit.FlyFrom, 1.6f) / 1.6f;
            Vector2 head = new Vector2(feetX + length - 4f, lieY + 8f);
            m_Zz.rectTransform.anchoredPosition = (head + new Vector2(k * 8f, k * 10f)) * Px;
            m_Zz.color = new Color(Paper.r, Paper.g, Paper.b, (1f - k) * (1f - shade));
            m_Zz.rectTransform.SetAsLastSibling();
        }

        // ------------------------------------------------------------------ A Trollogram of anything

        /// <summary>
        /// kcaj at his nook's rail on the upper floor, leaning out over kie at his bench below (kie repairing, his back to
        /// him); tones2 at his slushie machine. The silence by looks alone: kcaj facing the bench and the projector, then
        /// turned to his string board, looking up at it, then leaning back out at kie; tones2 frozen with the straw in his
        /// mouth, looking from one to the other. Then kie stands up from the bench and turns to say it, and kcaj peels a
        /// note off his board, folds it in two and pockets it.
        /// </summary>
        void Trollogram(TrollogramSkit s, float clock)
        {
            Ui.Show(m_Pad, false);
            Ui.Show(m_Remote, false);
            Ui.Show(m_Bag, false);
            Ui.Show(m_Drone, false);
            Ui.Show(m_Cable, false);
            Ui.Show(m_Zz, false);

            // kcaj: leaning out (the top forward and down) when he's on kie, upright at the projector, looking up at his board
            StageFigure kcaj = m_Stage.Figure(Content.Kcaj);
            TrollogramSkit.Look look = s.Kcaj;
            bool atBoard = look == TrollogramSkit.Look.Board;
            float leanX = look == TrollogramSkit.Look.Kie ? 1f : 0f, leanY = look == TrollogramSkit.Look.Kie ? -1f : atBoard ? 1f : 0f;
            SkitStage.Stand(kcaj, Content.Kcaj, new StagePose(StageLegs.Idle, StageTorso.Unarmed, leanX, leanY), KcajRail, Upper, !atBoard);

            // kie: at his bench, his back to kcaj, working (the bench is to his left); then up, turned round to say no
            StageFigure kie = m_Stage.Figure(Content.Kie);
            if (s.Working) SkitStage.Stand(kie, Content.Kie, StagePose.Repairing(clock), KieBench, Ground, true);
            else SkitStage.Stand(kie, Content.Kie, new StagePose(StageLegs.Idle, StageTorso.Unarmed), KieBench, Ground, false);

            // tones2: at his machine, facing them; the cup at his mouth from mid-slurp till he finishes it
            StageFigure t2 = m_Stage.Figure(Content.Tones2);
            SkitStage.Stand(t2, Content.Tones2, new StagePose(StageLegs.Idle, StageTorso.Unarmed, 0f, s.Tones2LooksUp ? 1f : 0f), Tones2Machine, Ground, true);
            SkitStage.Hold(m_Cup, t2, s.Slurping ? new Vector2(5f, 8f) : SkitStage.HandDown);

            // the note: on the board above his head, in his hand, folded in two, then in his pocket
            int note = s.Note;
            Ui.Show(m_Note, note > 0);
            if (note > 0)
            {
                float side = atBoard ? 1f : -1f;
                // on the board: kcaj's corner's string board, up to his right (DenRoom's kcaj layer)
                Vector2 at = note == 1 ? new Vector2(KcajRail + 16f, Upper + 20f) : new Vector2(KcajRail + side * 5f, Upper + 17f);
                m_Stage.Place(m_Note, at.x, at.y);
                m_Note.rectTransform.sizeDelta = new Vector2(3f, note == 3 ? 1.5f : 3f) * Px;
            }
        }
    }
}
