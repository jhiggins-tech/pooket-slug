using UnityEngine;
using UnityEngine.UI;

namespace PooketSlug
{
    /// <summary>
    /// uGUI built in code, since the project has no prefabs or scenes. Canvases made here are laid out in HUD
    /// units: <see cref="Tuning.Hud.Height"/> tall, and as wide as the screen's shape makes them. Each element is
    /// placed at an offset from an anchor on its parent, in uGUI's convention (y goes up).
    /// </summary>
    public static class Ui
    {
        public static readonly Vector2 TopLeft = new Vector2(0f, 1f), TopRight = new Vector2(1f, 1f), TopCenter = new Vector2(0.5f, 1f);
        public static readonly Vector2 BottomLeft = Vector2.zero, BottomRight = new Vector2(1f, 0f), Middle = new Vector2(0.5f, 0.5f);

        /// <summary>The colour of text shadows.</summary>
        public static readonly Color Shade = new Color(0.08f, 0.05f, 0.08f, 0.9f);

        static Font s_Font;
        static Sprite s_Disc, s_Heart, s_Arrow;

        /// <summary>Unity's built-in font, the one IMGUI used.</summary>
        public static Font Font => s_Font != null ? s_Font : s_Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        public static Sprite Disc => s_Disc != null ? s_Disc : s_Disc = ToSprite(Art.DiscTex);
        public static Sprite Heart => s_Heart != null ? s_Heart : s_Heart = ToSprite(Art.HeartTex);

        /// <summary>A solid arrowhead pointing right (the partner arrow, spike coop-camera).</summary>
        public static Sprite Arrow => s_Arrow != null ? s_Arrow : s_Arrow = ToSprite(ArrowTex());

        static Texture2D ArrowTex()
        {
            const int size = 16;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    px[y * size + x] = x >= 2 && Mathf.Abs(y - 7.5f) <= (15 - x) * 0.55f ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        static Sprite ToSprite(Texture2D tex) =>
            Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);

        /// <summary>A screen-space canvas whose height is always <see cref="Tuning.Hud.Height"/> units.</summary>
        public static RectTransform Canvas(Transform parent, string name, int order)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(Tuning.Hud.Width, Tuning.Hud.Height);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f; // the height sets the scale, as it did for the IMGUI HUD
            return (RectTransform)go.transform;
        }

        /// <summary>An empty rect whose <paramref name="pivot"/> sits <paramref name="offset"/> from the parent's <paramref name="anchor"/>.</summary>
        public static RectTransform Rect(RectTransform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 offset, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = offset;
            return rt;
        }

        /// <summary>Makes <paramref name="rt"/> fill its parent.</summary>
        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
        }

        /// <summary>A block of <paramref name="color"/> over the whole of <paramref name="parent"/>: a dim, a fade or a flash.</summary>
        public static Image Cover(RectTransform parent, string name, Color color)
        {
            Image image = Picture(parent, name, null, Middle, Middle, Vector2.zero, Vector2.zero, color);
            Stretch(image.rectTransform);
            return image;
        }

        /// <summary>Makes <paramref name="rt"/> take touches without being seen: a clear image for them to land on.</summary>
        public static Image Catcher(RectTransform rt)
        {
            var image = rt.gameObject.AddComponent<Image>();
            image.color = Color.clear;
            return image;
        }

        /// <summary>A tap on <paramref name="rt"/> calls <paramref name="onTap"/>: an unseen button, with no look or keyboard stop of its own.</summary>
        public static Button Tap(RectTransform rt, System.Action onTap) => Tap(Catcher(rt), onTap);

        /// <summary>A tap on <paramref name="look"/> (a cover or picture that's already there) calls <paramref name="onTap"/>, with no keyboard stop of its own.</summary>
        public static Button Tap(Graphic look, System.Action onTap)
        {
            look.raycastTarget = true;
            var b = look.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            b.onClick.AddListener(() => onTap());
            return b;
        }

        /// <summary>A rect that fills its parent.</summary>
        public static RectTransform Fill(RectTransform parent, string name)
        {
            RectTransform rt = Rect(parent, name, Vector2.zero, Middle, Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            return rt;
        }

        /// <summary>Bold, shadowed text, aligned to the point it's placed at; it never wraps or clips.</summary>
        public static Text Label(RectTransform parent, string name, Vector2 anchor, Vector2 offset, int size, TextAnchor align, Color color)
        {
            RectTransform rt = Rect(parent, name, anchor, PivotOf(align), offset, new Vector2(4f, 4f));
            var text = rt.gameObject.AddComponent<Text>();
            text.font = Font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = align;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            var shadow = rt.gameObject.AddComponent<Shadow>();
            shadow.effectColor = Shade;
            shadow.effectDistance = new Vector2(0.75f, -0.75f);
            return text;
        }

        /// <summary>A sprite (or a plain box, with no sprite) of a given size.</summary>
        public static Image Picture(RectTransform parent, string name, Sprite sprite, Vector2 anchor, Vector2 pivot, Vector2 offset, Vector2 size, Color color)
        {
            RectTransform rt = Rect(parent, name, anchor, pivot, offset, size);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static readonly Color Accent = new Color(1f, 0.62f, 0.2f);

        /// <summary>
        /// A button that answers clicks and taps. Keys are handled by the game, not by uGUI's navigation, so a key
        /// never presses a button and a shortcut too. <paramref name="hint"/> is the key shown under the label.
        /// </summary>
        public static Button Button(RectTransform parent, string label, Vector2 offset, Vector2 size, System.Action onClick, out Text hint)
        {
            RectTransform rt = Rect(parent, label, Middle, Middle, offset, size);
            var image = rt.gameObject.AddComponent<Image>();
            var outline = rt.gameObject.AddComponent<Outline>();
            outline.effectColor = Accent;
            outline.effectDistance = new Vector2(1f, -1f);
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.13f, 0.1f, 0.15f, 0.92f);
            colors.highlightedColor = new Color(0.3f, 0.2f, 0.24f, 0.96f);
            colors.selectedColor = colors.normalColor;
            colors.pressedColor = Accent;
            colors.fadeDuration = 0.05f;
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => onClick());
            Label(rt, "Label", Middle, Vector2.zero, 12, TextAnchor.MiddleCenter, Color.white).text = label;
            hint = Label(rt, "Key", new Vector2(0.5f, 0f), new Vector2(0f, -3f), 8, TextAnchor.UpperCenter, new Color(1f, 0.95f, 0.8f, 0.8f));
            return button;
        }

        /// <summary>Where a screen-space point lands on a canvas anchored at its bottom-left corner.</summary>
        public static Vector2 FromScreen(Vector3 screen) => new Vector2(screen.x, screen.y) * (Tuning.Hud.Height / Mathf.Max(1, Screen.height));

        public static void Show(Component c, bool on)
        {
            if (c.gameObject.activeSelf != on) c.gameObject.SetActive(on);
        }

        static Vector2 PivotOf(TextAnchor align)
        {
            int i = (int)align;
            return new Vector2((i % 3) * 0.5f, 1f - (i / 3) * 0.5f);
        }
    }
}
