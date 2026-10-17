using UnityEngine;
using UnityEngine.UI;

namespace PooketSlug
{
    /// <summary>
    /// Two of the crew, on the title screen (<c>docs/design/story.md</c>): kie walks in beside tones2 in the title's
    /// world, with a speech bubble over him, and a tap anywhere moves on. The menu growing is
    /// <see cref="MenuView"/>'s, from the same <see cref="CrewReveal"/>.
    /// </summary>
    public sealed class CrewRevealView
    {
        const float StandBeside = 1.4f, WalkSeconds = 1.6f;

        readonly Transform m_Parent;
        readonly RectTransform m_Root;
        readonly SpeechBubble m_Bubble; // the stage's (StageKit), over the title's world
        Transform m_Kie;
        PlayerVisual m_Visual;
        bool m_Tapped;
        float m_From, m_To, m_Y, m_Anim;

        public CrewRevealView(Transform parent)
        {
            m_Parent = parent;
            m_Root = Ui.Canvas(parent, "Two of the crew", 12);
            m_Root.gameObject.AddComponent<GraphicRaycaster>();
            Image catcher = Ui.Cover(m_Root, "Tap", Color.clear);
            catcher.raycastTarget = true;
            catcher.gameObject.AddComponent<Button>().onClick.AddListener(() => m_Tapped = true); // a tap anywhere

            m_Bubble = new SpeechBubble(m_Root, Ui.Middle, new Vector2(150f, 30f));
            Ui.Show(m_Root, false);
        }

        public bool Showing => m_Kie != null;

        /// <summary>Starts it: kie will walk in from off the right of <paramref name="world"/>'s view, to tones2.</summary>
        public void Begin(World world)
        {
            End();
            m_Tapped = false;
            m_Y = world.Player.Pos.y;
            m_To = world.Player.Pos.x + StandBeside;
            m_From = world.View.Right + 1f;
            m_Kie = new GameObject("kie, walking in").transform;
            m_Kie.SetParent(m_Parent, false);
            m_Visual = new PlayerVisual(m_Kie, Art.SkinFor(Content.Kie, 0), SortOrder.Player - 1);
            m_Kie.position = new Vector3(m_From, m_Y, 0f);
            Ui.Show(m_Root, true);
            m_Bubble.Hide();
        }

        public void End()
        {
            if (m_Kie != null) Object.Destroy(m_Kie.gameObject);
            m_Kie = null;
            m_Visual = null;
            Ui.Show(m_Root, false);
        }

        /// <summary>It's over and control's back: the bubble and tap catcher go, and kie stays stood by tones2.</summary>
        public void Finish()
        {
            Ui.Show(m_Root, false);
            m_Visual?.Apply(new AvatarPose { Facing = -1, Grounded = true });
        }

        /// <summary>A tap on the screen since the last call.</summary>
        public bool TakeTap()
        {
            bool t = m_Tapped;
            m_Tapped = false;
            return t;
        }

        public void Refresh(CrewReveal reveal, Camera camera, float dt)
        {
            if (m_Kie == null) return;
            // kie: off screen, then walking in, then stood beside tones2 (and there for good once it's done)
            float walk = reveal.Now < CrewReveal.Shot.KieWalksIn ? 0f : reveal.Now > CrewReveal.Shot.KieWalksIn ? 1f : Mathf.Clamp01(reveal.T / WalkSeconds);
            bool walking = walk > 0f && walk < 1f;
            float x = Mathf.Lerp(m_From, m_To, walk);
            m_Kie.position = new Vector3(x, m_Y, 0f);
            m_Kie.localScale = new Vector3(-1f, 1f, 1f); // facing tones2
            m_Anim += dt;
            m_Visual.Apply(new AvatarPose { Facing = -1, Grounded = true, Running = walking, Anim = m_Anim, AimY = reveal.Now == CrewReveal.Shot.KiesLine ? 1 : 0 });

            string line = reveal.Line;
            m_Bubble.Say(line);
            if (line == null) return;
            Vector3 screen = camera.WorldToScreenPoint(new Vector3(x, m_Y + 2.3f, 0f));
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_Root, screen, null, out Vector2 local))
                m_Bubble.Place(new Vector2(Mathf.Max(local.x, -m_Root.rect.width * 0.5f + 80f), local.y), local.x);
        }
    }
}
