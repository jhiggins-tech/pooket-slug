using UnityEngine;
using UnityEngine.UI;
using static PooketSlug.StageKit;

namespace PooketSlug
{
    /// <summary>
    /// kcaj's tabk coming home, drawn in the bay (<see cref="KcajHomecoming"/>, in Core, has the timing and the places):
    /// the wreck on the stands (the drone that brings it is the bay's own, flown by <see cref="BayView"/>), the crane
    /// lowering the soft-serve machine onto it with the BRAIN FREEZE sticker on its side, kcaj's KCAJ note slapped over the
    /// sticker, the swirl wobbling as the machine hums, his purple and gold running down the hull in drips, and the name
    /// card; kcaj and tones2 on the bay's <see cref="SkitStage"/>, with kcaj's two cones and his rag. Its tabk pieces are
    /// made with the view, behind the cast; its props once the cast is there (<see cref="Props"/>).
    /// </summary>
    sealed class BayKcajHomeView
    {
        const float Scale = BayMap.Scale;
        /// <summary>How high the crane holds the machine before it lowers it (as kie's projector).</summary>
        const float CraneLift = 70f, CraneY = 168f;
        /// <summary>The sticker's middle on the machine, from the tabk's feet (the machine's side, by its window), and the note's size, in the bay's pixels.</summary>
        static readonly Vector2 StickerAt = new Vector2(11f, 29f), NoteSize = new Vector2(11f, 8f);
        /// <summary>A figure's mouth from its middle (as the den's), and the hand that's up behind the head leaning back (the lounging top's).</summary>
        static readonly Vector2 Mouth = new Vector2(4f, 8f), HandUpBehind = new Vector2(-6.5f, 4f);
        static readonly Color NoteYellow = new Color(1f, 0.92f, 0.35f), Scrawl = new Color(0.42f, 0.23f, 0.63f);

        readonly Image m_Cable, m_Machine, m_Sticker, m_Note;
        readonly Image[] m_Drips = new Image[KcajHomecoming.Drips];
        readonly BayNameCard m_Card;
        Image m_ConeA, m_ConeB, m_Rag;

        /// <param name="world">The bay's world: made after its tabks, before the cast.</param>
        /// <param name="stage">The stage over it, for the name card.</param>
        public BayKcajHomeView(RectTransform world, RectTransform stage)
        {
            m_Cable = Picture(world, "Crane cable", null, 0f, 0f, 1f, 1f, new Color(0.1f, 0.1f, 0.12f));
            // the paint running down: the painted hull in strips over the wreck, each filled from the top
            Sprite paint = KcajTabkArt.Base;
            float w = paint.rect.width / m_Drips.Length;
            for (int i = 0; i < m_Drips.Length; i++)
            {
                Rect strip = new Rect(paint.rect.x + i * w, paint.rect.y, w, paint.rect.height);
                Sprite s = Sprite.Create(paint.texture, strip, new Vector2(0.5f, 0f), paint.pixelsPerUnit, 0, SpriteMeshType.FullRect);
                m_Drips[i] = Ui.Picture(world, "Paint", s, Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, strip.size * Scale * Px, Color.white);
                m_Drips[i].type = Image.Type.Filled;
                m_Drips[i].fillMethod = Image.FillMethod.Vertical;
                m_Drips[i].fillOrigin = (int)Image.OriginVertical.Top;
            }
            Sprite machine = KcajTabkArt.Machine;
            m_Machine = Ui.Picture(world, "Soft-serve machine", machine, Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, machine.rect.size * Scale * Px, Color.white);
            Sprite sticker = KcajTabkArt.Sticker;
            m_Sticker = Ui.Picture(world, "Sticker", sticker, Vector2.zero, Ui.Middle, Vector2.zero, sticker.rect.size * Scale * Px, Color.white);
            m_Note = Picture(world, "KCAJ note", null, 0f, 0f, NoteSize.x, NoteSize.y, NoteYellow);
            m_Note.rectTransform.pivot = Ui.Middle;
            Ui.Label(m_Note.rectTransform, "Scrawl", Ui.Middle, Vector2.zero, 7, TextAnchor.MiddleCenter, Scrawl).text = "KCAJ";
            m_Card = new BayNameCard(stage, KcajHomecoming.CardName, KcajHomecoming.CardTitle);
            Hide();
        }

        /// <summary>Its props in the cast's hands (made once the cast's figures are, so they're drawn over them).</summary>
        public void Props(SkitStage cast)
        {
            Sprite cone = KcajArt.Cone;
            Vector2 size = cone.rect.size * Staging.FigureGrow;
            m_ConeA = cast.Prop("kcaj's cone", cone, size.x, size.y, Color.white);
            m_ConeB = cast.Prop("kcaj's other cone", cone, size.x, size.y, Color.white);
            m_Rag = cast.Prop("kcaj's rag", null, 3f, 2f, new Color(0.9f, 0.86f, 0.78f));
        }

        /// <summary>Nothing of it shows (the cast is the stage's, hidden with it).</summary>
        public void Hide()
        {
            Ui.Show(m_Cable, false);
            Ui.Show(m_Machine, false);
            foreach (Image d in m_Drips) Ui.Show(d, false);
            HideSticker();
            m_Card.Show(0f);
        }

        public void HideSticker()
        {
            Ui.Show(m_Sticker, false);
            Ui.Show(m_Note, false);
        }

        /// <summary>
        /// The sticker on the machine whose feet are at <paramref name="feet"/> (the bay's pixels), and kcaj's note over it
        /// (<paramref name="note"/>, <paramref name="size"/> times its size as it's slapped on). Does it do cones? keeps them up.
        /// </summary>
        public void ShowSticker(Vector2 feet, bool note, float size = 1f)
        {
            Vector2 at = feet + StickerAt;
            Ui.Show(m_Sticker, true);
            m_Sticker.rectTransform.anchoredPosition = at * Px;
            Ui.Show(m_Note, note);
            if (!note) return;
            m_Note.rectTransform.anchoredPosition = at * Px;
            m_Note.rectTransform.localScale = new Vector3(size, size, 1f);
            m_Note.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -8f); // slapped on, not quite straight
        }

        /// <summary>Draws the beat now: <paramref name="body"/> is kcaj's tabk's body in the bay, <paramref name="cast"/> the bay's stage.</summary>
        public void Draw(KcajHomecoming b, Image body, SkitStage cast, float clock)
        {
            float floor = BayMap.FloorY(BayDeck.Floor);
            Vector2 stands = new Vector2(BayMap.StandX, BayMap.StandY);

            // the wreck on the cable, then on the stands; whole once the paint's down
            (float wx, float wy) = b.Wreck;
            body.rectTransform.anchoredPosition = new Vector2(wx, wy) * Px;
            body.sprite = b.Painted ? KcajTabkArt.Parked : KcajTabkArt.Wreck;
            body.color = Color.white;
            if (b.Carrying) body.transform.SetAsLastSibling(); // over the bay's other tabks as it comes down the bay

            // the machine, lowered on by the crane; humming, its swirl wobbles
            bool machine = b.Machine && !b.Painted;
            float lift = (1f - b.Lowered) * CraneLift;
            Vector2 feet = stands + new Vector2(0f, lift);
            Ui.Show(m_Machine, machine);
            if (machine)
            {
                m_Machine.sprite = b.Humming && Fx.Blink(clock, 8f) ? KcajTabkArt.MachineWobble : KcajTabkArt.Machine;
                m_Machine.rectTransform.anchoredPosition = feet * Px;
            }
            bool lowering = b.Machine && b.Lowered < 1f;
            Ui.Show(m_Cable, lowering);
            if (lowering)
            {
                float top = feet.y + 28f * Scale;
                m_Cable.rectTransform.anchoredPosition = new Vector2(feet.x, top) * Px;
                m_Cable.rectTransform.sizeDelta = new Vector2(1f, CraneY - top) * Px;
            }
            if (b.Machine) ShowSticker(feet, b.Note, b.NoteSize);
            else HideSticker();

            // the paint running down the hull, drip by drip: the machine's rows and the outline over it stay as they are
            bool painting = b.T >= KcajHomecoming.PaintFrom && !b.Painted;
            float rows = KcajTabkArt.Base.rect.height, above = KcajTabkArt.MachineRows + 1f;
            for (int i = 0; i < m_Drips.Length; i++)
            {
                Image d = m_Drips[i];
                Ui.Show(d, painting);
                if (!painting) continue;
                float sw = d.rectTransform.sizeDelta.x / Px;
                d.rectTransform.anchoredPosition = new Vector2(BayMap.StandX - BayMap.TabkW / 2f - Scale + (i + 0.5f) * sw, BayMap.StandY) * Px;
                d.fillAmount = (above + b.Paint(i) * (rows - above)) / rows;
            }
            m_Card.Show(b.Card);

            // tones2, on the floor to the right, watching (looking up at the drone while it flies)
            StageFigure t2 = cast.Figure(Content.Tones2);
            SkitStage.Stand(t2, Content.Tones2, new StagePose(StageLegs.Idle, StageTorso.Unarmed, 0f, b.Flying ? 1f : 0f), KcajHomecoming.Tones2X, floor, true);

            // kcaj: in after the drone with his cones, watching, up on the hull at the machine, then leaning back on the nozzle
            StageFigure k = cast.Figure(Content.Kcaj);
            (float kx, float ky) = b.Kcaj;
            StagePose pose;
            if (b.KcajHopping) pose = new StagePose(StageLegs.Jump, StageTorso.Unarmed);
            else if (b.KcajWalking) pose = StagePose.Walking(true, clock, b.Cones ? StageTorso.Carry : StageTorso.Unarmed);
            else if (b.AtMachine) pose = new StagePose(StageLegs.Idle, StageTorso.Carry, 0f, b.Pulling ? -1f : 0f);
            else if (b.Polishing) pose = new StagePose(StageLegs.Idle, StageTorso.Lounge);
            else pose = new StagePose(StageLegs.Idle, b.Cones ? StageTorso.Carry : StageTorso.Unarmed);
            SkitStage.Stand(k, Content.Kcaj, pose, kx, ky, b.KcajFacesLeft);
            Ui.Show(k.Rt, b.KcajIn);
            Ui.Show(m_ConeA, b.Cones && b.KcajIn);
            Ui.Show(m_ConeB, b.Cones && b.KcajIn);
            if (b.Cones && b.KcajIn)
            {
                SkitStage.Hold(m_ConeA, k, b.Biting ? Mouth : SkitStage.HandOut);
                SkitStage.Hold(m_ConeB, k, SkitStage.HandDown);
            }
            Rag(k, b.Polishing, clock);
            cast.Speak(b);
        }

        /// <summary>The rag in kcaj's hand up behind him on the nozzle, rubbing (Does it do cones? keeps it going).</summary>
        public void Rag(StageFigure kcaj, bool on, float clock)
        {
            Ui.Show(m_Rag, on);
            if (on) SkitStage.Hold(m_Rag, kcaj, HandUpBehind + new Vector2(Fx.Step(clock, 4f, 2) == 0 ? 0f : 1f, 0f));
        }
    }
}
