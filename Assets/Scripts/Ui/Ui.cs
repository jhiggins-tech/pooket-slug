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
        static Sprite s_Disc, s_Heart;

        /// <summary>Unity's built-in font, the one IMGUI used.</summary>
        public static Font Font => s_Font != null ? s_Font : s_Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        public static Sprite Disc => s_Disc != null ? s_Disc : s_Disc = ToSprite(Art.DiscTex);
        public static Sprite Heart => s_Heart != null ? s_Heart : s_Heart = ToSprite(Art.HeartTex);

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
            scaler.referenceResolution = new Vector2(Tuning.Hud.Height * 16f / 9f, Tuning.Hud.Height);
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
