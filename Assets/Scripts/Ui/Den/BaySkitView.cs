using UnityEngine;
using UnityEngine.UI;
using static PooketSlug.StageKit;

namespace PooketSlug
{
    /// <summary>
    /// The bay's skit, drawn (<see cref="ConesSkit"/>, in Core: Does it do cones?): SOFT SERVE on the stands as kcaj's
    /// homecoming leaves it (its KCAJ note still over the sticker, kcaj leaning back on it polishing the nozzle), its nozzle
    /// on its own so it swings round over the top onto tones2 and its swirl wobbling as it pipes, the two soft-serve cones
    /// flying from it into tones2's hands (squashed) with the splat on him, kie's sprint in from the den's door grinning and
    /// his face falling, and the walk back out. On the bay's <see cref="SkitStage"/>, as the homecoming is; its nozzle is
    /// made with the view, behind the cast, and its props once the cast is there (<see cref="Props"/>).
    /// </summary>
    sealed class BaySkitView
    {
        const float Scale = BayMap.Scale;
        static readonly Vector2 Mouth = new Vector2(4f, 8f);

        readonly Image m_Nozzle;
        Image m_ConeA, m_ConeB, m_Splat;

        /// <param name="world">The bay's world: made after its tabks, before the cast.</param>
        public BaySkitView(RectTransform world)
        {
            Sprite nozzle = KcajTabkArt.Nozzle;
            m_Nozzle = Ui.Picture(world, "SOFT SERVE's nozzle", nozzle, Vector2.zero, nozzle.pivot / nozzle.rect.size, Vector2.zero, nozzle.rect.size * Scale * Px, Color.white);
            Hide();
        }

        /// <summary>Its props (made once the cast's figures are, so they're drawn over them): the two cones and the splat, the figures' size.</summary>
        public void Props(SkitStage cast)
        {
            Sprite cone = CutsceneSnacks.SoftServe, splat = CutsceneSnacks.Splat[0];
            Vector2 c = cone.rect.size * Staging.FigureGrow, s = splat.rect.size * Staging.FigureGrow;
            m_ConeA = cast.Prop("Soft serve", cone, c.x, c.y, Color.white);
            m_ConeB = cast.Prop("Soft serve", cone, c.x, c.y, Color.white);
            m_Splat = cast.Prop("Splat", splat, s.x, s.y, Color.white);
        }

        public void Hide() => Ui.Show(m_Nozzle, false);

        /// <summary>
        /// Draws the skit now: <paramref name="body"/> is kcaj's tabk's body on the stands, <paramref name="cast"/> the bay's
        /// stage, <paramref name="home"/> the homecoming's view (its sticker, note and rag stay up).
        /// </summary>
        public void Draw(ConesSkit s, Image body, SkitStage cast, BayKcajHomeView home, float clock)
        {
            float floor = BayMap.FloorY(BayDeck.Floor);
            Vector2 stands = new Vector2(BayMap.StandX, BayMap.StandY);

            // SOFT SERVE: its hull (the swirl wobbling as it pipes), the nozzle swung round over the top onto tones2
            body.sprite = s.Wobbling && Fx.Blink(clock, 8f) ? KcajTabkArt.HullCharging : KcajTabkArt.Hull;
            body.color = Color.white;
            Ui.Show(m_Nozzle, true);
            RectTransform n = m_Nozzle.rectTransform;
            n.anchoredPosition = new Vector2(ConesSkit.Pivot.x, ConesSkit.Pivot.y) * Px;
            float turn = s.NozzleTurn;
            n.localRotation = Quaternion.Euler(0f, 0f, turn);
            n.localScale = new Vector3(1f, turn < -90f ? -1f : 1f, 1f); // past straight up it's turned over: kept the right way up
            home.ShowSticker(stands, true);

            // kcaj: leaning back on it with his rag, then round to the nozzle, swinging it onto tones2
            StageFigure k = cast.Figure(Content.Kcaj);
            bool working = s.T >= ConesSkit.SwingFrom;
            SkitStage.Stand(k, Content.Kcaj, new StagePose(StageLegs.Idle, working ? StageTorso.Carry : StageTorso.Lounge), ConesSkit.KcajX, floor, !working);
            home.Rag(k, !working, clock);

            // tones2: up behind the stands eyeing the swirl, then a cone in each hand, the splat on him, and a lick
            StageFigure t2 = cast.Figure(Content.Tones2);
            bool holding = s.Hit(0);
            StagePose pose = s.Tones2Walking ? StagePose.Walking(true, clock)
                : new StagePose(StageLegs.Idle, holding ? StageTorso.Carry : StageTorso.Unarmed, 0f, s.Tones2LooksUp ? 1f : 0f);
            SkitStage.Stand(t2, Content.Tones2, pose, s.Tones2X, floor, true);
            Cone(m_ConeA, s, 0, t2, cast);
            Cone(m_ConeB, s, 1, t2, cast);
            float since = s.SplatSince;
            Ui.Show(m_Splat, since >= 0f);
            if (since >= 0f)
            {
                m_Splat.sprite = CutsceneSnacks.Splat[SnackShow.SplatFrame(since)];
                cast.Place(m_Splat, s.Tones2X, floor);
                m_Splat.rectTransform.localScale = new Vector3(-1f, 1f, 1f); // the way he faces
            }

            // kie: the sprint in grinning, the skid, his face falling as he looks from the cones to tones2, then the walk out
            StageFigure kie = cast.Figure(Content.Kie);
            if (!s.KieIn) Ui.Show(kie.Rt, false);
            else
            {
                StagePose p;
                if (s.KieRunning) p = StagePose.Grinning(true, s.T, ConesSkit.SprintFps);
                else if (s.KieLeaving) p = StagePose.Glum(true, s.T, ConesSkit.GlumFps);
                else if (s.Falling)
                {
                    p = StagePose.FaceFalling(s.FallSince);
                    p.TorsoY = s.KieLooksDown ? -1f : 0f;
                }
                else
                {
                    p = StagePose.Grinning(false, s.T);
                    if (s.Skidding) p.TorsoX = -1f; // back on his heels
                }
                SkitStage.Stand(kie, Content.Kie, p, s.KieX, floor, s.KieLeaving);
            }
            cast.Speak(s);
        }

        /// <summary>Cone <paramref name="i"/>: hidden till it's piped, then arcing from the nozzle, then squashed in tones2's hand (the front one at his mouth for the lick).</summary>
        static void Cone(Image cone, ConesSkit s, int i, StageFigure tones2, SkitStage cast)
        {
            float k = s.Cone(i);
            Ui.Show(cone, k >= 0f);
            if (k < 0f) return;
            if (k < 1f)
            {
                cone.sprite = CutsceneSnacks.SoftServe;
                (float x, float y) = s.ConeAt(i);
                cast.Place(cone, x, y);
                return;
            }
            cone.sprite = CutsceneSnacks.Squashed;
            SkitStage.Hold(cone, tones2, i == 1 ? SkitStage.HandDown : s.Licking ? Mouth : SkitStage.HandOut);
        }
    }
}
