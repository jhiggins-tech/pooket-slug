using UnityEngine;
using UnityEngine.UI;

namespace PooketSlug
{
    /// <summary>
    /// The title: a splash screen (Start flow 5, <c>docs/design/art.md</c>, The title logo). The logo over a gentle
    /// colour ramp with a vignette, START and SETTINGS, and nothing else: missions, the crew and co-op are the den's.
    /// It fades in from black once a visit as the logo makes its entrance (<see cref="TitleLogoView"/>), START and
    /// SETTINGS fading in once it's landed; a tap skips the entrance. Its backdrop stays behind the settings screen.
    /// </summary>
    public sealed class TitleView
    {
        /// <summary>The ramp, top to bottom (royal blue, as the start-art page tried it), and how dark the vignette's corners go.</summary>
        static readonly Color Top = new Color32(6, 11, 58, 255), Bottom = new Color32(43, 58, 168, 255);
        const float Vignette = 0.6f, VignetteFrom = 0.35f;
        /// <summary>The fade in from black, then the logo's (when it's the whole logo, not in parts), in seconds.</summary>
        const float FadeIn = 0.8f, LogoIn = 1.2f;
        const float EngineVolume = 0.7f, CannonVolume = 0.9f;
        /// <summary>The logo's width on screen, in canvas units (the canvas is 360 high), and where its middle sits.</summary>
        const float LogoWidth = 440f, LogoY = 72f;
        static readonly Vector2 ButtonSize = new Vector2(160f, 30f);
        static readonly Color ButtonFill = new Color(0.04f, 0.05f, 0.16f, 0.85f);

        readonly RectTransform m_Backdrop, m_Panel;
        readonly RawImage m_Ramp, m_Logo;
        readonly TitleLogoView m_Parts;
        readonly CanvasGroup m_Menu;
        readonly AudioSource m_Engine;
        readonly Text m_Hint, m_Account, m_PlayKey, m_SettingsKey;
        float m_Shown = -1f; // when the splash first showed this visit (below 0: not yet)
        float m_Was = float.NegativeInfinity; // how far into the entrance the last frame was (for its sounds)

        /// <summary>The splash under <paramref name="parent"/>; a button calls <paramref name="click"/> with its action.</summary>
        public TitleView(RectTransform parent, System.Action<MenuAction> click)
        {
            m_Backdrop = Ui.Fill(parent, "Title backdrop");
            m_Ramp = m_Backdrop.gameObject.AddComponent<RawImage>();
            m_Ramp.texture = RampTexture();
            m_Ramp.raycastTarget = true; // taps on the splash stop here, not on the world under it
            Ui.Tap(m_Ramp, Skip); // and skip the entrance
            m_Engine = Sfx.OwnSource(m_Backdrop.gameObject);

            m_Panel = Ui.Fill(parent, "Title");
            m_Parts = TitleLogoView.Make(m_Panel, new Vector2(0f, LogoY), LogoWidth);
            Texture2D logo = m_Parts == null ? LogoTexture() : null; // a build without the parts: the whole logo, fading in
            if (logo != null)
            {
                float height = LogoWidth * logo.height / logo.width;
                RectTransform rt = Ui.Rect(m_Panel, "Logo", Ui.Middle, Ui.Middle, new Vector2(0f, LogoY), new Vector2(LogoWidth, height));
                m_Logo = rt.gameObject.AddComponent<RawImage>();
                m_Logo.texture = logo;
                m_Logo.raycastTarget = false;
            }
            else if (m_Parts == null)
            {
                // a build without the logo (it's in Resources/Title): the name in the game's gold
                Ui.Label(m_Panel, "Name", Ui.Middle, new Vector2(0f, LogoY), 44, TextAnchor.MiddleCenter, Palette.Gold).text = "POOKET SLUG";
            }
            RectTransform menu = Ui.Fill(m_Panel, "Menu");
            m_Menu = menu.gameObject.AddComponent<CanvasGroup>();
            Button(menu, "START", new Vector2(0f, -40f), () => click(MenuAction.Play), out m_PlayKey);
            Button(menu, "SETTINGS", new Vector2(0f, -84f), () => click(MenuAction.Settings), out m_SettingsKey);
            m_Hint = Ui.Label(menu, "Hint", new Vector2(0.5f, 0f), new Vector2(0f, 26f), 9, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.8f, 0.55f));
            m_Account = Ui.Label(menu, "Account", new Vector2(0.5f, 0f), new Vector2(0f, 12f), 9, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.6f));
            Ui.Show(m_Backdrop, false);
            Ui.Show(m_Panel, false);
        }

        /// <summary>The splash shows on the title, and its backdrop behind the settings opened from it.</summary>
        public void Show(MenuScreen screen)
        {
            bool title = screen == MenuScreen.Title;
            Ui.Show(m_Backdrop, title || screen == MenuScreen.Settings);
            Ui.Show(m_Panel, title);
            if (title && m_Shown < 0f) m_Shown = Time.unscaledTime;
        }

        public void Refresh(in MenuInfo info)
        {
            bool keys = !info.Touch;
            Set(m_Hint, info.Touch ? (info.Options.LeftHanded ? "RIGHT THUMB RUNS · LEFT THUMB FIRES AND JUMPS" : "LEFT THUMB RUNS · RIGHT THUMB FIRES AND JUMPS")
                : "MOVE  Arrows      JUMP  Z      FIRE  X");
            Key(m_PlayKey, keys, "ENTER");
            Key(m_SettingsKey, keys, "O");
            Ui.Show(m_Account, info.Account != null);
            if (info.Account != null) Set(m_Account, info.Account);

            // once a visit: up from black, the logo's entrance, then the menu
            float t = Time.unscaledTime - m_Shown;
            m_Ramp.color = Color.Lerp(Color.black, Color.white, Smooth(t / FadeIn));
            if (m_Logo != null) m_Logo.color = new Color(1f, 1f, 1f, Smooth((t - FadeIn * 0.5f) / LogoIn));
            if (m_Parts == null) return;
            float into = t - TitleMotion.Start;
            m_Parts.Pose(into, info.Options.Shake ? 1f : 0f);
            float menu = TitleMotion.MenuIn(m_Parts.Logo, into);
            m_Menu.alpha = menu;
            m_Menu.interactable = m_Menu.blocksRaycasts = menu > 0.5f; // till then a tap lands on the backdrop: a skip
            if (m_Was < 0f && into >= 0f) Sfx.PlayOn(m_Engine, Sfx.EngineRev, EngineVolume);
            if (TitleMotion.Fired(m_Was, into)) Sfx.Play(Sfx.Cannon, CannonVolume);
            m_Was = into;
        }

        /// <summary>The title music can start: the entrance's shot has rung out (or there's no entrance in this build).</summary>
        public bool MusicDue => m_Parts == null || (m_Shown >= 0f && Time.unscaledTime - m_Shown - TitleMotion.Start >= TitleMotion.MusicAt);

        /// <summary>A tap during the entrance: straight to its end, its sounds cut off (the music starts at once).</summary>
        void Skip()
        {
            if (m_Parts == null || m_Shown < 0f) return;
            float end = TitleMotion.Start + TitleMotion.End(m_Parts.Logo);
            if (Time.unscaledTime - m_Shown >= end) return;
            m_Shown = Time.unscaledTime - end;
            m_Was = end - TitleMotion.Start;
            if (m_Engine != null) m_Engine.Stop();
        }

        /// <summary>A splash button: a dark box edged in gold, its label in gold.</summary>
        static void Button(RectTransform parent, string label, Vector2 offset, System.Action onClick, out Text hint)
        {
            Button b = Ui.Button(parent, label, offset, ButtonSize, onClick, out hint);
            b.GetComponent<Outline>().effectColor = Palette.Gold;
            // The fill is the image's own colour and the tint stays near white: uGUI tints the outline too, so a dark
            // tint (as Ui.Button's) would turn the gold edge black. The multiplier lets a press brighten it.
            ((Image)b.targetGraphic).color = ButtonFill;
            ColorBlock colors = b.colors;
            colors.colorMultiplier = 4f;
            colors.normalColor = colors.selectedColor = new Color(0.25f, 0.25f, 0.25f, 1f);
            colors.highlightedColor = new Color(0.32f, 0.32f, 0.36f, 1f);
            colors.pressedColor = new Color(0.9f, 0.75f, 0.3f, 1f);
            b.colors = colors;
            Text text = b.transform.Find("Label").GetComponent<Text>();
            text.color = Palette.Gold;
            text.fontSize = 13;
        }

        /// <summary>
        /// The backdrop: the ramp, top to bottom, darkened towards the corners. Small, and smoothed when it's stretched
        /// over the screen.
        /// </summary>
        static Texture2D RampTexture()
        {
            const int W = 128, H = 72;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[W * H];
            float corner = Mathf.Sqrt(0.5f * 0.5f * (16f / 9f) * (16f / 9f) + 0.25f);
            for (int y = 0; y < H; y++)
            {
                float v = (y + 0.5f) / H; // 0 at the bottom: textures count up
                Color row = Color.Lerp(Bottom, Top, Mathf.SmoothStep(0f, 1f, v));
                for (int x = 0; x < W; x++)
                {
                    float dx = ((x + 0.5f) / W - 0.5f) * 16f / 9f, dy = v - 0.5f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy) / corner;
                    float k = 1f - Vignette * Smooth((r - VignetteFrom) / (1f - VignetteFrom));
                    Color c = row * k;
                    c.a = 1f;
                    pixels[y * W + x] = c;
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }

        /// <summary>
        /// Logo C1, <c>Resources/Title/logo.bytes</c>: a PNG with its thin dark outline and shadow baked in
        /// (<c>tools/title_logo.py</c>), or null if this build hasn't got it.
        /// </summary>
        static Texture2D LogoTexture()
        {
            TextAsset raw = Resources.Load<TextAsset>("Title/logo");
            if (raw == null)
            {
                Log.Game.Warn("No title logo in this build");
                return null;
            }
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear }; // mipmapped: smaller phones show it smaller
            if (!tex.LoadImage(raw.bytes, true))
            {
                Log.Game.Warn("The title logo didn't load");
                return null;
            }
            return tex;
        }

        static float Smooth(float t) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));

        static void Set(Text text, string value)
        {
            if (text.text != value) text.text = value;
        }

        static void Key(Text hint, bool show, string key)
        {
            Ui.Show(hint, show);
            if (show && hint.text != key) hint.text = key;
        }
    }
}
