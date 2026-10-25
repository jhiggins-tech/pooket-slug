using System;
using UnityEngine;
using UnityEngine.UI;

namespace PooketSlug
{
    /// <summary>
    /// The kit the staged screens share: the cutscenes (<see cref="CutsceneView"/>), the den (<see cref="DenView"/>), the
    /// tabk bay and the outer rooms (<see cref="BayView"/>, <see cref="RoomView"/>, on <see cref="StageScreen"/>), and the
    /// debrief (<see cref="DebriefView"/>, its figures and colours). A stage is a
    /// 320 by 180 art-pixel picture, letterboxed, drawn at <see cref="Px"/> HUD units a pixel; things on it are placed in
    /// art pixels from its bottom left, by their feet. Its colours, its painted backdrops, its soldiers (legs and torso,
    /// as in the game) and its speech bubbles are all here, so the screens look like one game.
    /// </summary>
    public static class StageKit
    {
        /// <summary>HUD units per art pixel.</summary>
        public const float Px = 2f;
        /// <summary>
        /// HUD units per art pixel for a soldier on a stage: the stage's, a fifth again (<see cref="Staging.FigureGrow"/>):
        /// the rooms' paintings are drawn a touch small for him, and this brings the two back together until the plates
        /// are redrawn. It sizes the figure; where a figure stands is still in the plate's pixels (<see cref="StageFigure.AtPlate"/>).
        /// </summary>
        public const float FigureScale = Px * Staging.FigureGrow;
        /// <summary>The stage's size in art pixels, and the height of a room's floor.</summary>
        public const int StageW = 320, StageH = 180, Floor = 22;

        /// <summary>A bubble's lines and outline, and its paper.</summary>
        public static readonly Color Ink = new Color(0.1f, 0.07f, 0.1f), Paper = new Color(1f, 0.98f, 0.92f);
        public static readonly Color Gold = Palette.Gold, Cyan = Palette.Cyan;
        /// <summary>The bands that captions, names and cards sit on.</summary>
        public static readonly Color Band = new Color(0.06f, 0.04f, 0.08f, 0.9f);

        static Sprite s_White;

        /// <summary>A plain white sprite, tinted to colour a block.</summary>
        public static Sprite White => s_White != null ? s_White : s_White = Sprite.Create(Art.WhiteTex, new Rect(0f, 0f, Art.WhiteTex.width, Art.WhiteTex.height), new Vector2(0.5f, 0.5f), 100f);

        /// <summary>
        /// A stage on <paramref name="root"/>: black letterboxing over the whole screen (returned, so a screen can take
        /// taps on it), the stage, masked to its edges, and the world in it, which a camera can move and zoom; what's
        /// over the world (captions, buttons) goes on the stage.
        /// </summary>
        public static Image Build(RectTransform root, out RectTransform stage, out RectTransform world)
        {
            Image black = Ui.Cover(root, "Letterbox", Color.black);
            stage = Ui.Rect(root, "Stage", Ui.Middle, Ui.Middle, Vector2.zero, new Vector2(StageW, StageH) * Px);
            stage.gameObject.AddComponent<RectMask2D>();
            world = Ui.Rect(stage, "World", Ui.Middle, Ui.Middle, Vector2.zero, new Vector2(StageW, StageH) * Px);
            return black;
        }

        /// <summary>A block (or a sprite) in the world: <paramref name="x"/> its middle, <paramref name="y"/> its bottom, in art pixels.</summary>
        public static Image Picture(RectTransform world, string name, Sprite sprite, float x, float y, float w, float h, Color color) =>
            Ui.Picture(world, name, sprite != null ? sprite : White, Vector2.zero, new Vector2(0.5f, 0f), new Vector2(x, y) * Px, new Vector2(w, h) * Px, color);

        /// <summary>Words in the world, at <paramref name="x"/>, <paramref name="y"/> in art pixels.</summary>
        public static Text Label(RectTransform world, string name, float x, float y, int size, TextAnchor align, Color color) =>
            Ui.Label(world, name, Vector2.zero, new Vector2(x, y) * Px, size, align, color);

        /// <summary>The whole stage's backdrop, painted pixel by pixel.</summary>
        public static Sprite Backdrop(Action<PixelCanvas> draw)
        {
            var c = new PixelCanvas(StageW, StageH);
            draw(c);
            return Sprite.Create(Art.ToTexture(c), new Rect(0, 0, StageW, StageH), Vector2.zero, 1f);
        }

        /// <summary>The backdrop's image, filling the stage's world, under everything else in it.</summary>
        public static Image BackdropImage(RectTransform world)
        {
            Image back = Picture(world, "Backdrop", null, 0f, 0f, StageW, StageH, Color.white);
            back.rectTransform.pivot = Vector2.zero;
            return back;
        }

        /// <summary>A sky that fades from <paramref name="bottom"/> up to <paramref name="top"/>.</summary>
        public static void Sky(PixelCanvas c, uint top, uint bottom)
        {
            Color32 a = Art.Hex(top), b = Art.Hex(bottom);
            for (int y = 0; y < StageH; y++)
                c.Rect(0, y, StageW, 1, Color32.Lerp(b, a, y / (float)StageH));
        }
    }

    /// <summary>A soldier on a stage, drawn from its legs and torso as in the game (24 by 32 art pixels), placed by its feet.</summary>
    public sealed class StageFigure
    {
        public readonly RectTransform Rt;
        public readonly Image Legs, Torso;
        readonly float m_Scale;

        /// <param name="anchor">Where on <paramref name="parent"/> it's placed from: the world's bottom left, or a panel's middle.</param>
        /// <param name="scale">HUD units per art pixel: the stage's, or bigger on a screen like the debrief's.</param>
        public StageFigure(RectTransform parent, string name, Vector2 anchor, float scale = StageKit.FigureScale)
        {
            m_Scale = scale;
            Vector2 size = new Vector2(24f, 32f) * scale;
            Rt = Ui.Rect(parent, name, anchor, new Vector2(0.5f, 0f), Vector2.zero, size);
            Legs = Ui.Picture(Rt, "Legs", null, Ui.Middle, Ui.Middle, Vector2.zero, size, Color.white);
            Torso = Ui.Picture(Rt, "Torso", null, Ui.Middle, Ui.Middle, Vector2.zero, size, Color.white);
        }

        /// <summary>A figure in a stage's world.</summary>
        public StageFigure(RectTransform world, string name) : this(world, name, Vector2.zero) { }

        /// <summary>Stands it with its feet at <paramref name="x"/>, <paramref name="y"/> (art pixels).</summary>
        public void At(float x, float y) => Rt.anchoredPosition = new Vector2(x, y) * m_Scale;

        /// <summary>
        /// Stands it with its feet at <paramref name="x"/>, <paramref name="y"/> in a plate's own art pixels, which is
        /// what a room's painting is drawn in (<see cref="StageKit.Px"/> a pixel, not the figure's
        /// <see cref="StageKit.FigureScale"/>): the figure keeps its size, and stands where the painting puts the floor.
        /// </summary>
        public void AtPlate(float x, float y) => Rt.anchoredPosition = new Vector2(x, y) * StageKit.Px;

        /// <summary>Standing still, facing the camera.</summary>
        public void Stand(PlayerSkin skin) => Pose(skin, StagePose.Stand);

        /// <summary>Strikes <paramref name="pose"/> in <paramref name="skin"/>: its legs (or none, in a hatch), its top and the top's nudge.</summary>
        public void Pose(PlayerSkin skin, StagePose pose)
        {
            Ui.Show(Legs, pose.Legs != StageLegs.Hidden);
            switch (pose.Legs)
            {
                case StageLegs.Walk: Legs.sprite = skin.RunLegs(pose.WalkFrame(4)); break;
                case StageLegs.Crouch: Legs.sprite = skin.Legs(CrewLegs.Crouch); break;
                case StageLegs.Sit: Legs.sprite = skin.Legs(CrewLegs.Sit); break;
                case StageLegs.Jump: Legs.sprite = skin.Legs(CrewLegs.Jump); break;
                default: Legs.sprite = skin.Legs(CrewLegs.Idle); break;
            }
            Torso.sprite = skin.Body(pose.Body);
            Torso.rectTransform.anchoredPosition = new Vector2(pose.TorsoX, pose.TorsoY) * m_Scale;
        }

        /// <summary>Tints the whole figure.</summary>
        public void Tint(Color c) => Legs.color = Torso.color = c;

        /// <summary>Just above its head, where its bubble points.</summary>
        public Vector2 Head => Rt.anchoredPosition + new Vector2(0f, 30f * m_Scale);
    }

    /// <summary>
    /// A speech bubble: a paper box with an ink outline and a tail that points down at whoever's talking. It fits its
    /// line on one row, or, given a width, wraps it in a box that size.
    /// </summary>
    public sealed class SpeechBubble
    {
        public const float Height = 22f;
        public readonly RectTransform Rt;
        readonly Image m_Back, m_Tail;
        readonly Text m_Text;
        readonly Vector2 m_Wrap;

        /// <param name="anchor">Where on <paramref name="parent"/> its position is from (a world's bottom left, or a panel's middle).</param>
        /// <param name="wrap">A fixed size to wrap the line in, or zero to fit one row.</param>
        public SpeechBubble(RectTransform parent, Vector2 anchor, Vector2 wrap = default)
        {
            m_Wrap = wrap;
            Rt = Ui.Rect(parent, "Bubble", anchor, new Vector2(0.5f, 0f), Vector2.zero, wrap != Vector2.zero ? wrap : new Vector2(40f, Height));
            m_Tail = Ui.Picture(Rt, "Tail", StageKit.White, new Vector2(0.5f, 0f), Ui.Middle, Vector2.zero, new Vector2(8f, 8f), StageKit.Paper);
            m_Tail.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            m_Back = Rt.gameObject.AddComponent<Image>();
            m_Back.sprite = StageKit.White;
            m_Back.raycastTarget = false;
            var outline = Rt.gameObject.AddComponent<Outline>();
            outline.effectColor = StageKit.Ink;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            m_Text = Ui.Label(Rt, "Line", Ui.Middle, Vector2.zero, wrap != Vector2.zero ? 10 : 11, TextAnchor.MiddleCenter, StageKit.Ink);
            m_Text.GetComponent<Shadow>().enabled = false;
            if (wrap != Vector2.zero)
            {
                m_Text.rectTransform.sizeDelta = wrap - new Vector2(10f, 4f);
                m_Text.horizontalOverflow = HorizontalWrapMode.Wrap;
            }
            m_Tail.transform.SetAsFirstSibling();
            Ui.Show(Rt, false);
        }

        /// <summary>Shows <paramref name="line"/> (or hides the bubble for null), in ink on paper unless other colours are given; its width.</summary>
        public float Say(string line, Color? ink = null, Color? paper = null)
        {
            Ui.Show(Rt, line != null);
            if (line == null) return 0f;
            if (m_Text.text != line) m_Text.text = line;
            m_Text.color = ink ?? StageKit.Ink;
            m_Back.color = m_Tail.color = paper ?? StageKit.Paper;
            if (m_Wrap != Vector2.zero) return m_Wrap.x;
            float w = m_Text.preferredWidth + 16f;
            Rt.sizeDelta = new Vector2(w, Height);
            return w;
        }

        /// <summary>Puts its bottom middle at <paramref name="at"/>, its tail pointing down at <paramref name="speakerX"/>.</summary>
        public void Place(Vector2 at, float speakerX)
        {
            Rt.anchoredPosition = at;
            float half = Rt.sizeDelta.x / 2f;
            m_Tail.rectTransform.anchoredPosition = new Vector2(Mathf.Clamp(speakerX - at.x, -half + 8f, half - 8f), 0f);
        }

        public void Hide() => Ui.Show(Rt, false);
    }
}
