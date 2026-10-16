using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PooketSlug
{
    /// <summary>
    /// The cutscenes spike (<c>?spike=cutscenes</c>): draws a <see cref="CutscenePlayer"/>'s scene in uGUI, over the
    /// game. It's the game's own sprites on painted backdrops, with speech bubbles, a caption card, a SKIP button once
    /// the scene's been seen, and in co-op a line saying who's still to tap. The staging (where everyone stands and
    /// moves) is written per shot for the opening, its test case (<c>docs/design/story.md</c>, The opening scene).
    /// <para>
    /// The stage is 320 x 180 art pixels drawn at 2x, so the soldiers are the size they are in the game, and it's
    /// letterboxed on screens of other shapes.
    /// </para>
    /// </summary>
    public sealed class CutsceneView
    {
        const float Px = 2f; // HUD units per art pixel
        const int StageW = 320, StageH = 180, Street = 26, Floor = 22;

        static readonly Color Ink = new Color(0.1f, 0.07f, 0.1f), Paper = new Color(1f, 0.98f, 0.92f);
        static readonly Color Cyan = new Color(0.45f, 1f, 1f), Warm = new Color(1f, 0.78f, 0.38f), Gold = new Color(1f, 0.85f, 0.3f);
        static readonly Color Water = new Color(0.45f, 0.75f, 1f);

        readonly RectTransform m_Root, m_Stage, m_World;
        readonly Image m_Fade, m_Flicker, m_Back, m_Window, m_Door, m_DoorGap, m_Bag, m_Cup, m_Card, m_Spark;
        readonly Image[] m_Snow = new Image[36], m_Chips = new Image[10], m_Jet = new Image[8], m_Beams = new Image[2];
        readonly Figure m_Tones2, m_Goon;
        readonly Text m_Sign, m_Hint, m_Waiting, m_Subtitle;
        readonly RectTransform m_SubBand, m_BigCard;
        readonly Image m_Dim;
        readonly RectTransform m_CardBand;
        readonly Text m_CardText;
        readonly Button m_Skip;
        // a character's intro: the crash zoom moves m_World, and their name slams up over it
        readonly RectTransform m_IntroBand;
        readonly Text m_IntroName;
        readonly Image m_Flash;
        float m_Clock, m_LastNow;
        // ?spike=narrator: the candidate voices, and a button to switch between them
        readonly NarratorVoices m_Voices;
        readonly Button m_VoiceButton;
        readonly Text m_VoiceLabel;
        string m_LastSaid;
        bool m_VoicePressed;
        readonly List<Bubble> m_Bubbles = new List<Bubble>();
        readonly List<CutsceneLine> m_Lines = new List<CutsceneLine>();
        static Sprite s_Street, s_Room, s_White;

        CutscenePlayer m_Player;
        float m_FlickerUntil, m_GoonDiedAt = -1f;
        bool m_Tapped, m_SkipPressed;

        /// <summary>A soldier drawn from its legs and torso, as in the game.</summary>
        sealed class Figure
        {
            public readonly RectTransform Rt;
            public readonly Image Legs, Torso;

            public Figure(RectTransform stage, string name)
            {
                Rt = Ui.Rect(stage, name, Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(24f, 32f) * Px);
                Legs = Ui.Picture(Rt, "Legs", null, Ui.Middle, Ui.Middle, Vector2.zero, new Vector2(24f, 32f) * Px, Color.white);
                Torso = Ui.Picture(Rt, "Torso", null, Ui.Middle, Ui.Middle, Vector2.zero, new Vector2(24f, 32f) * Px, Color.white);
            }

            public void At(float x, float y) => Rt.anchoredPosition = new Vector2(x, y) * Px;
            public Vector2 Head => Rt.anchoredPosition + new Vector2(0f, 30f * Px);
        }

        sealed class Bubble
        {
            public RectTransform Rt;
            public Image Back, Tail;
            public Text Text;
        }

        public CutsceneView(Transform parent)
        {
            m_Root = Ui.Canvas(parent, "Cutscene", 20);
            m_Root.gameObject.AddComponent<GraphicRaycaster>(); // taps and SKIP (the menus' event system serves it)
            Image black = Ui.Picture(m_Root, "Letterbox", null, Ui.Middle, Ui.Middle, Vector2.zero, Vector2.zero, Color.black);
            Stretch(black.rectTransform);
            black.raycastTarget = true;
            var tap = black.gameObject.AddComponent<Button>();
            tap.transition = Selectable.Transition.None;
            tap.navigation = new Navigation { mode = Navigation.Mode.None };
            tap.onClick.AddListener(() => m_Tapped = true);

            m_Stage = Ui.Rect(m_Root, "Stage", Ui.Middle, Ui.Middle, Vector2.zero, new Vector2(StageW, StageH) * Px);
            m_Stage.gameObject.AddComponent<RectMask2D>();
            // everything in the scene sits in the world, which the camera's crash zoom moves; what's over it (subtitles,
            // the caption card, buttons) sits on the stage
            m_World = Ui.Rect(m_Stage, "World", Ui.Middle, Ui.Middle, Vector2.zero, new Vector2(StageW, StageH) * Px);
            m_Back = Picture("Backdrop", null, 0f, 0f, StageW, StageH, Color.white);
            m_Back.rectTransform.pivot = Vector2.zero;
            m_Window = Picture("Window glow", null, 262f, Street + 38f, 28f, 20f, Warm);
            m_Sign = Ui.Label(m_World, "Sign", Vector2.zero, new Vector2(232f, 110f) * Px, 20, TextAnchor.MiddleCenter, new Color(1f, 0.55f, 0.2f));
            m_Sign.text = "POOKET";
            m_DoorGap = Picture("Doorway", null, 232f, Street, 20f, 40f, Cyan);
            m_Door = Picture("Door", null, 222f, Street, 20f, 40f, new Color(0.42f, 0.27f, 0.2f));
            m_Door.rectTransform.pivot = new Vector2(0f, 0f);
            for (int i = 0; i < m_Beams.Length; i++)
            {
                m_Beams[i] = Picture("Searchlight", null, 267f + i * 10f, 111f, 3f, 36f, new Color(1f, 1f, 0.8f, 0.35f));
                m_Beams[i].rectTransform.pivot = new Vector2(0.5f, 0f);
            }

            m_Spark = Picture("Spark", null, 128f, 52f, 6f, 6f, Gold);
            for (int i = 0; i < m_Chips.Length; i++) m_Chips[i] = Picture("Chip", null, 0f, 0f, 3f, 2f, new Color(1f, 0.82f, 0.3f));
            m_Goon = new Figure(m_World, "Goon");
            m_Goon.Rt.localScale = new Vector3(-1f, 1f, 1f); // faces left, at tones2
            m_Tones2 = new Figure(m_World, "tones2");
            m_Bag = Picture("Snacks", null, 0f, 0f, 9f, 11f, new Color(0.95f, 0.3f, 0.35f));
            m_Cup = Picture("Slushie", null, 0f, 0f, 6f, 12f, new Color(0.55f, 0.35f, 0.95f));
            m_Card = Picture("Keycard", null, 0f, 0f, 8f, 5f, Cyan);
            for (int i = 0; i < m_Jet.Length; i++) m_Jet[i] = Picture("Water", null, 0f, 0f, 4f, 3f, Water);
            for (int i = 0; i < m_Snow.Length; i++) m_Snow[i] = Picture("Snow", null, 0f, 0f, 1.5f, 1.5f, new Color(1f, 1f, 1f, 0.85f));

            // a character's intro: a white flash, then their name on a slanted band
            m_Flash = Ui.Picture(m_Stage, "Flash", null, Ui.Middle, Ui.Middle, Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0f));
            Stretch(m_Flash.rectTransform);
            m_IntroBand = Ui.Rect(m_Stage, "Intro", Ui.Middle, Ui.Middle, new Vector2(StageW * Px * 0.2f, 28f), new Vector2(StageW * Px * 0.62f, 74f));
            var introBack = m_IntroBand.gameObject.AddComponent<Image>();
            introBack.color = new Color(0.06f, 0.04f, 0.08f, 0.9f);
            introBack.raycastTarget = false;
            m_IntroBand.localRotation = Quaternion.Euler(0f, 0f, 6f);
            Ui.Picture(m_IntroBand, "Edge", White, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(StageW * Px * 0.62f, 5f), Gold);
            Ui.Picture(m_IntroBand, "Edge", White, new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(StageW * Px * 0.62f, 5f), Gold);
            m_IntroName = Ui.Label(m_IntroBand, "Name", Ui.Middle, Vector2.zero, 52, TextAnchor.MiddleCenter, Gold);
            var nameOutline = m_IntroName.gameObject.AddComponent<Outline>();
            nameOutline.effectColor = Ink;
            nameOutline.effectDistance = new Vector2(2.5f, -2.5f);

            // the keycard's close-up: the room dimmed, and the card big enough to read
            m_Dim = Ui.Picture(m_Stage, "Dim", null, Ui.Middle, Ui.Middle, Vector2.zero, Vector2.zero, new Color(0.03f, 0.02f, 0.05f, 0.7f));
            Stretch(m_Dim.rectTransform);
            m_BigCard = Ui.Rect(m_Stage, "Keycard close-up", Ui.Middle, Ui.Middle, new Vector2(0f, 26f), new Vector2(124f, 78f) * Px);
            m_BigCard.gameObject.AddComponent<Image>().color = new Color(0.3f, 0.85f, 0.92f);
            m_BigCard.gameObject.AddComponent<Outline>().effectColor = Ink;
            Ui.Picture(m_BigCard, "Stripe", White, Ui.TopCenter, Ui.TopCenter, new Vector2(0f, -10f), new Vector2(124f, 18f) * Px, new Color(0.08f, 0.2f, 0.26f));
            Ui.Picture(m_BigCard, "Photo", White, Ui.BottomLeft, Ui.BottomLeft, new Vector2(10f, 12f) * Px, new Vector2(26f, 30f) * Px, new Color(0.12f, 0.3f, 0.36f));
            Ui.Picture(m_BigCard, "Chip", White, Ui.BottomRight, Ui.BottomRight, new Vector2(-10f, 14f) * Px, new Vector2(16f, 12f) * Px, Gold);
            Ui.Label(m_BigCard, "Title", Ui.TopCenter, new Vector2(0f, -18f), 22, TextAnchor.UpperCenter, Color.white).text = "MILITARY BASE";
            Ui.Label(m_BigCard, "Gate", Vector2.zero, new Vector2(84f, 66f), 18, TextAnchor.MiddleLeft, Ink).text = "NORTH GATE";
            Ui.Label(m_BigCard, "Access", Vector2.zero, new Vector2(84f, 40f), 11, TextAnchor.MiddleLeft, new Color(0.08f, 0.2f, 0.26f)).text = "ACCESS: ALL AREAS";
            foreach (Text t in m_BigCard.GetComponentsInChildren<Text>()) t.GetComponent<Shadow>().enabled = false;

            m_CardBand = Ui.Rect(m_Stage, "Caption card", Ui.Middle, Ui.Middle, new Vector2(0f, 10f), new Vector2(StageW * Px + 20f, 76f));
            var band = m_CardBand.gameObject.AddComponent<Image>();
            band.color = new Color(0.06f, 0.04f, 0.08f, 0.92f);
            band.raycastTarget = false; // a tap on the card still moves on
            m_CardText = Ui.Label(m_CardBand, "Text", Ui.Middle, Vector2.zero, 22, TextAnchor.MiddleCenter, Gold);
            m_CardText.lineSpacing = 1.1f;

            m_Flicker = Ui.Picture(m_Stage, "Flicker", null, Ui.Middle, Ui.Middle, Vector2.zero, Vector2.zero, new Color(0.4f, 1f, 1f, 0f));
            Stretch(m_Flicker.rectTransform);
            m_Fade = Ui.Picture(m_Stage, "Cut", null, Ui.Middle, Ui.Middle, Vector2.zero, Vector2.zero, Color.black);
            Stretch(m_Fade.rectTransform);

            // the narrator's subtitles, in a band along the bottom
            m_SubBand = Ui.Rect(m_Stage, "Subtitles", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(StageW * Px - 40f, 26f));
            var sub = m_SubBand.gameObject.AddComponent<Image>();
            sub.color = new Color(0f, 0f, 0f, 0.62f);
            sub.raycastTarget = false;
            m_Subtitle = Ui.Label(m_SubBand, "Narrator", Ui.Middle, Vector2.zero, 13, TextAnchor.MiddleCenter, Paper);
            m_Subtitle.fontStyle = FontStyle.BoldAndItalic;

            m_Hint = Ui.Label(m_Stage, "Tap", Ui.TopRight, new Vector2(-8f, -8f), 9, TextAnchor.UpperRight, new Color(1f, 1f, 1f, 0.55f));
            m_Hint.text = "TAP TO GO ON";
            m_Waiting = Ui.Label(m_Stage, "Waiting", new Vector2(0.5f, 0f), new Vector2(0f, 38f), 11, TextAnchor.LowerCenter, Cyan);
            m_Skip = Ui.Button(m_Stage, "SKIP", new Vector2(StageW * Px / 2f - 42f, StageH * Px / 2f - 20f), new Vector2(64f, 22f), () => m_SkipPressed = true, out Text skipKey);
            skipKey.text = "ESC";

            if (Spike.Narrator)
            {
                m_Voices = NarratorVoices.Load(Spike.Voice);
                m_VoiceButton = Ui.Button(m_Stage, "VOICE", new Vector2(-StageW * Px / 2f + 130f, StageH * Px / 2f - 20f), new Vector2(244f, 22f), () => m_VoicePressed = true, out Text voiceKey);
                voiceKey.text = m_Voices != null ? "TAP TO TRY THE NEXT VOICE" : "NO VOICES IN THIS BUILD: SUBTITLES ONLY";
                m_VoiceLabel = m_VoiceButton.GetComponentInChildren<Text>();
                m_VoiceLabel.fontSize = 10;
            }

            Ui.Show(m_Root, false);
        }

        Image Picture(string name, Sprite sprite, float x, float y, float w, float h, Color color) =>
            Ui.Picture(m_World, name, sprite != null ? sprite : White, Vector2.zero, new Vector2(0.5f, 0f), new Vector2(x, y) * Px, new Vector2(w, h) * Px, color);

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
        }

        static Sprite White => s_White != null ? s_White : s_White = Sprite.Create(Art.WhiteTex, new Rect(0f, 0f, Art.WhiteTex.width, Art.WhiteTex.height), new Vector2(0.5f, 0.5f), 100f);

        public bool Showing => m_Root.gameObject.activeSelf;

        public void Begin(CutscenePlayer player)
        {
            m_Player = player;
            m_GoonDiedAt = -1f;
            m_FlickerUntil = 0f;
            m_Tapped = m_SkipPressed = m_VoicePressed = false;
            m_LastSaid = null;
            m_Clock = 0f;
            m_LastNow = Time.unscaledTime;
            if (s_Street == null)
            {
                s_Street = Backdrop(DrawStreet);
                s_Room = Backdrop(DrawRoom);
            }
            Ui.Show(m_Root, true);
        }

        public void Hide()
        {
            if (m_Voices != null) Sfx.Hush();
            m_Player = null;
            Ui.Show(m_Root, false);
        }

        /// <summary>The screen was tapped or clicked since the last call.</summary>
        public bool TakeTap()
        {
            bool t = m_Tapped;
            m_Tapped = false;
            return t;
        }

        /// <summary>SKIP was pressed since the last call.</summary>
        public bool TakeSkip()
        {
            bool s = m_SkipPressed;
            m_SkipPressed = false;
            return s;
        }

        /// <summary>Plays the sounds and the one-off staging for cues that just came due.</summary>
        public void Cue(string id)
        {
            switch (id)
            {
                case "door": Sfx.Play(Sfx.Thump, 0.7f); break;
                case "static": Sfx.Play(Sfx.Glitch, 0.4f); m_FlickerUntil = Time.unscaledTime + 0.25f; break;
                case "drop": Sfx.Play(Sfx.Splat, 0.6f); break;
                case "goon": Sfx.Play(Sfx.Glitch, 0.5f); break;
                case "jet": Sfx.Play(Sfx.Splat, 0.8f); break;
                case "glitch": Sfx.Play(Sfx.Glitch, 0.7f); m_GoonDiedAt = m_Player.T; break;
                case "card-drop": Sfx.Play(Sfx.Pickup, 0.6f); break;
                case "card-up": Sfx.Play(Sfx.Pickup, 0.9f); break;
                case "crush": Sfx.Play(Sfx.Hit, 0.7f); break;
                case "card": Sfx.Play(Sfx.Thump, 1f); break;
                case CutscenePlayer.IntroCue: Sfx.Play(Sfx.Intro, 1f); break;
                default:
                    if (id.StartsWith("say:")) Sfx.Say(id.Substring(4));
                    break;
            }
        }

        /// <summary>Draws the scene where the player has it now.</summary>
        public void Refresh()
        {
            CutscenePlayer p = m_Player;
            if (p == null || p.Done) return;
            // the scene's own clock, for snow, flickers and sparks, so they go slow-mo in an intro too
            float real = Time.unscaledTime;
            m_Clock += (real - m_LastNow) * (p.Introducing ? CutscenePlayer.SlowMo : 1f);
            m_LastNow = real;
            float t = p.T, now = m_Clock;

            Stage(p.Shot, t, now);
            Intro(p);

            // a quick cut between shots, and the cyan flicker of the holograms' static
            m_Fade.color = new Color(0f, 0f, 0f, Mathf.Clamp01(1f - t / 0.18f));
            float flicker = now < m_FlickerUntil ? ((int)(now * 30f) % 2 == 0 ? 0.35f : 0.1f) : 0f;
            m_Flicker.color = new Color(0.4f, 1f, 1f, flicker);

            p.Showing(m_Lines);
            int bubbles = 0;
            bool card = false;
            foreach (CutsceneLine line in m_Lines)
            {
                if (line.Who == Speaker.Caption)
                {
                    card = true;
                    m_CardText.text = line.Text;
                    float since = t - line.At;
                    float scale = since < 0.12f ? Mathf.Lerp(1.6f, 1f, since / 0.12f) : 1f;
                    m_CardBand.localScale = new Vector3(1f, scale, 1f);
                    m_CardText.rectTransform.localScale = new Vector3(scale, 1f, 1f);
                    continue;
                }
                if (line.Who == Speaker.Goon && m_GoonDiedAt >= 0f && t - m_GoonDiedAt > 0.3f) continue; // gone with the goon
                ShowBubble(bubbles++, line, now);
            }
            for (int i = bubbles; i < m_Bubbles.Count; i++) Ui.Show(m_Bubbles[i].Rt, false);
            bool narrating = p.Narrating(out CutsceneLine narration);
            Ui.Show(m_SubBand, narrating);
            if (narrating) m_Subtitle.text = narration.Text;
            Voice(narrating ? narration.Voice : null);
            Ui.Show(m_CardBand, card);

            Ui.Show(m_Skip, p.CanSkip);
            Ui.Show(m_Hint, !p.CanSkip && !p.Waiting && p.Shot == 0 && t > 1.2f);
            Ui.Show(m_Waiting, p.Waiting);
            if (p.Waiting) m_Waiting.text = p.HasTapped(0) ? "WAITING FOR YOUR PARTNER" : "YOUR PARTNER'S READY: TAP TO GO ON";
        }

        /// <summary>
        /// A character's intro (<c>docs/design/story.md</c>, Cutscenes): the camera crash-zooms onto them, there's a
        /// white flash, their name slams up on a band, and it all eases back out as the slow-mo ends.
        /// </summary>
        void Intro(CutscenePlayer p)
        {
            CutsceneIntro intro = p.Current.Intro;
            bool on = p.Introducing && intro != null;
            Ui.Show(m_IntroBand, on);
            if (!on)
            {
                m_World.localScale = Vector3.one;
                m_World.anchoredPosition = Vector2.zero;
                m_Flash.color = new Color(1f, 1f, 1f, 0f);
                return;
            }
            float k = p.IntroT, end = CutscenePlayer.IntroSeconds;
            float zoomIn = Mathf.Clamp01(k / 0.12f), zoomOut = Mathf.Clamp01((end - k) / 0.3f);
            float e = Mathf.Min(1f - (1f - zoomIn) * (1f - zoomIn), zoomOut * zoomOut * (3f - 2f * zoomOut));

            Figure who = intro.Who == Speaker.Goon || intro.Who == Speaker.Hologram ? m_Goon : m_Tones2;
            Vector2 half = new Vector2(StageW, StageH) * Px / 2f;
            Vector2 focus = who.Rt.anchoredPosition + new Vector2(0f, 16f * Px) - half; // their middle, from the world's centre
            Vector2 target = new Vector2(-StageW * Px * 0.22f, -12f); // where they end up: left of centre, the name to their right
            float scale = Mathf.Lerp(1f, 2.6f, e);
            m_World.localScale = new Vector3(scale, scale, 1f);
            m_World.anchoredPosition = Vector2.Lerp(focus, target, e) - focus * scale;

            m_Flash.color = new Color(1f, 1f, 1f, Mathf.Clamp01(0.85f - k / 0.18f));
            m_IntroName.text = intro.Name;
            float slam = Mathf.Clamp01((k - 0.08f) / 0.1f);
            float size = k < 0.08f ? 0f : Mathf.Lerp(2.4f, 1f, slam);
            m_IntroBand.localScale = new Vector3(size * zoomOut, size, 1f);
        }

        /// <summary>?spike=narrator: reads each narrator line as it comes up, and the voice button tries the next voice.</summary>
        void Voice(string line)
        {
            if (m_VoiceButton == null) return;
            Ui.Show(m_VoiceButton, true);
            string label = m_Voices != null ? m_Voices.Label : "NARRATOR: NO VOICE";
            if (m_VoiceLabel.text != label)
            {
                m_VoiceLabel.text = label;
                m_VoiceLabel.fontSize = 10;
                while (m_VoiceLabel.fontSize > 7 && m_VoiceLabel.preferredWidth > 236f) m_VoiceLabel.fontSize--; // a long name shrinks to fit
            }
            if (m_Voices == null) return;
            if (m_VoicePressed)
            {
                m_VoicePressed = false;
                m_Voices.Next();
                m_LastSaid = null; // say the line up now again, in the new voice
            }
            if (line == m_LastSaid) return;
            m_LastSaid = line;
            m_Voices.Say(line);
        }

        void ShowBubble(int i, CutsceneLine line, float now)
        {
            while (m_Bubbles.Count <= i) m_Bubbles.Add(NewBubble());
            Bubble b = m_Bubbles[i];
            Ui.Show(b.Rt, true);
            bool glitchy = line.Who == Speaker.Goon || line.Who == Speaker.Hologram;
            b.Text.text = line.Text;
            b.Text.color = glitchy ? Cyan : Ink;
            b.Back.color = b.Tail.color = glitchy ? new Color(0.05f, 0.12f, 0.16f, 0.95f) : Paper;
            float w = b.Text.preferredWidth + 16f;
            b.Rt.sizeDelta = new Vector2(w, 22f);

            Figure who = line.Who == Speaker.Goon ? m_Goon : m_Tones2;
            Vector2 at = who.Head + new Vector2(0f, 16f);
            float half = w / 2f + 4f;
            at.x = Mathf.Clamp(at.x, half, StageW * Px - half);
            if (glitchy && (int)(now * 12f) % 5 == 0) at.x += 2f; // the holograms' bubbles glitch too
            b.Rt.anchoredPosition = at;
            b.Tail.rectTransform.anchoredPosition = new Vector2(Mathf.Clamp(who.Head.x - at.x, -w / 2f + 8f, w / 2f - 8f), 0f);
        }

        Bubble NewBubble()
        {
            var b = new Bubble();
            b.Rt = Ui.Rect(m_World, "Bubble", Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(40f, 22f));
            b.Tail = Ui.Picture(b.Rt, "Tail", White, new Vector2(0.5f, 0f), Ui.Middle, Vector2.zero, new Vector2(8f, 8f), Paper);
            b.Tail.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            b.Back = b.Rt.gameObject.AddComponent<Image>();
            b.Back.sprite = White;
            b.Back.raycastTarget = false;
            var outline = b.Rt.gameObject.AddComponent<Outline>();
            outline.effectColor = Ink;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            b.Text = Ui.Label(b.Rt, "Line", Ui.Middle, Vector2.zero, 11, TextAnchor.MiddleCenter, Ink);
            b.Text.GetComponent<Shadow>().enabled = false;
            b.Tail.transform.SetAsFirstSibling();
            b.Rt.SetAsLastSibling();
            return b;
        }

        // ------------------------------------------------------------------ the opening's staging

        void Stage(int shot, float t, float now)
        {
            bool street = shot <= 1;
            m_Back.sprite = street ? s_Street : s_Room;
            Ui.Show(m_Sign, street);
            Ui.Show(m_Window, street);
            Ui.Show(m_Door, street);
            Ui.Show(m_DoorGap, street && shot == 1);
            for (int i = 0; i < m_Snow.Length; i++) Snow(i, now, street);
            Ui.Show(m_Goon.Rt, false);
            Ui.Show(m_Card, false);
            Ui.Show(m_Dim, shot == 4);
            Ui.Show(m_BigCard, shot == 4);
            Ui.Show(m_Spark, !street && shot >= 2 && (int)(now * 9f) % 3 == 0);
            foreach (Image j in m_Jet) Ui.Show(j, false);
            foreach (Image b in m_Beams) Ui.Show(b, false);
            foreach (Image c in m_Chips) Ui.Show(c, false);
            Ui.Show(m_Tones2.Rt, shot != 4);
            Ui.Show(m_Bag, shot <= 2);
            Ui.Show(m_Cup, shot <= 5);
            m_Window.color = Warm;
            m_Door.rectTransform.localRotation = Quaternion.identity;
            m_Door.rectTransform.localScale = Vector3.one;
            m_Door.rectTransform.anchoredPosition = new Vector2(222f, Street) * Px;

            switch (shot)
            {
                case 0: // outside the hangout: tones2 strolls in from the left with the snacks
                {
                    float x = Mathf.Lerp(-16f, 196f, Mathf.Clamp01(t / 3.6f));
                    Soldier(x, Street, t < 3.6f, t);
                    break;
                }
                case 1: // the door swings in off a hinge, and the warm light turns cold
                {
                    Soldier(206f, Street, false, t);
                    float swing = Mathf.Clamp01((t - 0.5f) / 0.3f);
                    // it swings in on its hinge and sags, hanging off it
                    m_Door.rectTransform.localScale = new Vector3(1f - swing * 0.65f, 1f, 1f);
                    m_Door.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -swing * 9f);
                    bool cold = t > 1.2f;
                    Color glow = cold ? ((int)(now * 14f) % 4 == 0 ? Warm : Cyan) : Warm;
                    if (t > 1.8f) glow = Cyan;
                    m_Window.color = glow;
                    m_DoorGap.color = glow;
                    break;
                }
                case 2: // inside, raided: a long look, then the bag drops and the chips go everywhere
                {
                    Soldier(56f, Floor, false, t);
                    if (t >= 2.2f)
                    {
                        Ui.Show(m_Bag, true);
                        float fall = Mathf.Clamp01((t - 2.2f) / 0.2f);
                        m_Bag.rectTransform.anchoredPosition = new Vector2(66f, Mathf.Lerp(Floor + 12f, Floor, fall)) * Px;
                        if (fall >= 1f) Chips(t - 2.4f, 66f);
                    }
                    break;
                }
                case 3: // a straggler: it flickers up and shouts, gets sprayed, glitches out and drops its keycard, which tones2 picks up
                {
                    Ui.Show(m_Bag, false);
                    Chips(9f, 66f);
                    bool walk = t > 5.4f && t < 6.2f;
                    float x = t <= 5.4f ? 56f : Mathf.Lerp(56f, 164f, Mathf.Clamp01((t - 5.4f) / 0.8f));
                    bool spraying = t >= 3.2f && t < 3.8f;
                    Soldier(x, Floor, walk, t, spraying);
                    Goon(t, now);
                    if (spraying) Jet(t - 3.2f, x + 10f, 168f);
                    if (t >= 4.2f && t < 6.4f) Keycard(Mathf.Clamp01((t - 4.2f) / 0.35f), 176f);
                    break;
                }
                case 4: // the keycard, close up: it slides up into view, and flickers like the holograms do
                {
                    Soldier(40f, Floor, false, t);
                    float up = Mathf.Clamp01(t / 0.35f);
                    m_BigCard.anchoredPosition = new Vector2((int)(now * 10f) % 9 == 0 ? 2f : 0f, Mathf.Lerp(-140f, 26f, 1f - (1f - up) * (1f - up)));
                    m_BigCard.localRotation = Quaternion.Euler(0f, 0f, (1f - up) * -8f);
                    break;
                }
                case 5: // heading out: searchlights through the window, the slushie crushed, and off he goes
                {
                    Chips(9f, 66f);
                    for (int i = 0; i < m_Beams.Length; i++)
                    {
                        Ui.Show(m_Beams[i], true);
                        m_Beams[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(now * 1.3f + i * 2f) * 20f);
                    }
                    float x = t < 1.8f ? 164f : Mathf.Lerp(164f, 340f, (t - 1.8f) / 3f);
                    Soldier(x, Floor, t >= 1.8f, t);
                    if (t >= 1.6f) Ui.Show(m_Cup, false);
                    else m_Cup.rectTransform.localScale = new Vector3(1f, t > 1.4f ? 0.5f : 1f, 1f);
                    break;
                }
            }
        }

        void Soldier(float x, float y, bool walking, float t, bool spraying = false)
        {
            PlayerSkin skin = Art.Tones2Skins[0];
            m_Tones2.Legs.sprite = walking ? skin.LegsRun[(int)(t * 10f) % skin.LegsRun.Length] : skin.LegsIdle;
            m_Tones2.Torso.sprite = skin.TorsoForward;
            m_Tones2.At(x, y);
            m_Tones2.Torso.rectTransform.anchoredPosition = spraying ? new Vector2(-1f, 0f) : Vector2.zero;
            m_Bag.rectTransform.anchoredPosition = new Vector2(x - 6f, y + 9f) * Px;
            m_Cup.rectTransform.anchoredPosition = new Vector2(x + 9f, y + 13f) * Px;
            m_Cup.rectTransform.localScale = Vector3.one;
        }

        void Goon(float t, float now)
        {
            const float x = 178f;
            if (m_GoonDiedAt >= 0f && t - m_GoonDiedAt > 0.6f) return;
            Ui.Show(m_Goon.Rt, true);
            m_Goon.Legs.sprite = Art.Holo(Art.SoldierLegsIdle);
            m_Goon.Torso.sprite = Art.Holo(Art.SoldierTorso);
            // it flickers up from behind the couch
            float rise = Mathf.Clamp01((t - 0.2f) / 0.3f);
            float y = Floor + 6f;
            float jitter = (int)(now * 20f) % 7 == 0 ? 1f : 0f;
            Color tint = new Color(0.45f, 1f, 1f, rise < 1f ? ((int)(now * 30f) % 2 == 0 ? rise : 0.2f) : 0.9f);
            Vector3 scale = new Vector3(-1f, 1f, 1f);
            if (m_GoonDiedAt >= 0f)
            {
                // the goons' own death: a flash, tearing, then squashed to a line and gone
                float d = t - m_GoonDiedAt;
                tint = d < 0.08f ? Color.white : Color.Lerp(Cyan, new Color(1f, 0.4f, 0.9f), (int)(now * 30f) % 2);
                jitter = Mathf.Sin(now * 90f) * d * 14f;
                scale = new Vector3(-1f - d * 0.6f, Mathf.Max(0.04f, 1f - d * 1.6f), 1f);
                tint.a = 1f - Mathf.Clamp01((d - 0.4f) / 0.2f);
            }
            m_Goon.Legs.color = m_Goon.Torso.color = tint;
            m_Goon.Rt.localScale = scale;
            m_Goon.At(x + jitter, y);
        }

        void Jet(float t, float from, float to)
        {
            for (int i = 0; i < m_Jet.Length; i++)
            {
                float k = (t * 3f + i / (float)m_Jet.Length) % 1f;
                float x = Mathf.Lerp(from, to, k);
                float y = Floor + 17f + Mathf.Sin(k * Mathf.PI) * 6f;
                Ui.Show(m_Jet[i], true);
                m_Jet[i].rectTransform.anchoredPosition = new Vector2(x, y) * Px;
            }
        }

        void Keycard(float fall, float x)
        {
            Ui.Show(m_Card, true);
            m_Card.rectTransform.anchoredPosition = new Vector2(x, Mathf.Lerp(Floor + 14f, Floor, fall * fall)) * Px;
            m_Card.rectTransform.localRotation = Quaternion.Euler(0f, 0f, (1f - fall) * 200f);
        }

        void Chips(float t, float x)
        {
            for (int i = 0; i < m_Chips.Length; i++)
            {
                float dir = (i - m_Chips.Length / 2f) * 2.4f;
                float k = Mathf.Clamp01(t / 0.35f);
                float hop = Mathf.Sin(k * Mathf.PI) * (4f + i % 3 * 3f);
                Ui.Show(m_Chips[i], true);
                m_Chips[i].rectTransform.anchoredPosition = new Vector2(x + dir * k * 1.6f, Floor + hop) * Px;
            }
        }

        void Snow(int i, float now, bool on)
        {
            Image s = m_Snow[i];
            Ui.Show(s, on);
            if (!on) return;
            float speed = 10f + (i * 7 % 9);
            float y = StageH - ((now * speed + i * 53f) % (StageH + 4f));
            float x = (i * 97f % StageW) + Mathf.Sin(now * 0.9f + i) * 4f;
            s.rectTransform.anchoredPosition = new Vector2(x, y) * Px;
        }

        // ------------------------------------------------------------------ the painted backdrops

        static Sprite Backdrop(System.Action<PixelCanvas> draw)
        {
            var c = new PixelCanvas(StageW, StageH);
            draw(c);
            return Sprite.Create(Art.ToTexture(c), new Rect(0, 0, StageW, StageH), Vector2.zero, 1f);
        }

        static void Sky(PixelCanvas c, uint top, uint bottom)
        {
            Color32 a = Art.Hex(top), b = Art.Hex(bottom);
            for (int y = 0; y < StageH; y++)
                c.Rect(0, y, StageW, 1, Color32.Lerp(b, a, y / (float)StageH));
        }

        static void DrawStreet(PixelCanvas c)
        {
            Sky(c, 0x0b0f24, 0x1d2448);
            var rng = new System.Random(7);
            for (int i = 0; i < 60; i++) c.Set(rng.Next(StageW), 70 + rng.Next(StageH - 70), Art.Hex(0xc8d0ff, (byte)(120 + rng.Next(120))));
            // the town behind
            for (int x = 0, i = 0; x < 150; x += 22 + i % 3 * 6, i++) c.Rect(x, Street, 18 + i % 2 * 8, 30 + (i * 13) % 34, Art.Hex(0x161a33));
            for (int i = 0; i < 18; i++) c.Rect(4 + i * 8 % 140, Street + 8 + i * 11 % 40, 2, 3, Art.Hex(0x6a5a2a));
            // the hangout: a garage with a roll door and a side door
            c.Rect(150, Street, 156, 96, Art.Hex(0x3a2a2e));
            for (int y = Street + 4; y < Street + 96; y += 6)
                for (int x = 150 + (y / 6 % 2) * 6; x < 306; x += 12) c.Rect(x, y, 1, 5, Art.Hex(0x2e2024));
            c.Rect(146, Street + 96, 164, 6, Art.Hex(0x241820));
            c.Rect(160, Street, 50, 54, Art.Hex(0x5b5f66));
            for (int y = Street + 3; y < Street + 54; y += 5) c.Rect(160, y, 50, 1, Art.Hex(0x45484e));
            c.Rect(221, Street, 22, 42, Art.Hex(0x1c1214)); // the side door's frame
            c.Rect(246, Street + 36, 32, 24, Art.Hex(0x1c1214)); // the window's frame
            // the street
            c.Rect(0, 0, StageW, Street, Art.Hex(0x2a2c38));
            c.Rect(0, Street - 3, StageW, 3, Art.Hex(0x4a4c5a));
            for (int x = 6; x < StageW; x += 28) c.Rect(x, 9, 12, 2, Art.Hex(0x55576a));
            // snow on the ground and roof
            c.Rect(0, Street - 1, StageW, 1, Art.Hex(0xdfe6ff));
            c.Rect(146, Street + 102, 164, 2, Art.Hex(0xdfe6ff));
            // a streetlight
            c.Rect(110, Street, 2, 70, Art.Hex(0x22242e));
            c.Rect(106, Street + 68, 10, 3, Art.Hex(0x22242e));
            c.Circle(111, Street + 66, 2.5f, Art.Hex(0xffe6a0));
        }

        static void DrawRoom(PixelCanvas c)
        {
            // walls, with cyan scorch marks
            c.Rect(0, 0, StageW, StageH, Art.Hex(0x2b2236));
            for (int x = 0; x < StageW; x += 40) c.Rect(x, Floor, 1, StageH - Floor, Art.Hex(0x241c2e));
            var rng = new System.Random(3);
            for (int i = 0; i < 7; i++)
            {
                int sx = 20 + rng.Next(StageW - 40), sy = 60 + rng.Next(90);
                for (int k = 0; k < 14; k++) c.Set(sx + rng.Next(-6, 7), sy + rng.Next(-8, 9), Art.Hex(0x3fd4e0, (byte)(90 + rng.Next(120))));
            }
            // the window, on the night and the base on its hill
            c.Rect(236, 92, 70, 60, Art.Hex(0x14101a));
            for (int y = 95; y < 149; y++) c.Rect(239, y, 64, 1, Color32.Lerp(Art.Hex(0x232a52), Art.Hex(0x0b0f24), (y - 95) / 54f));
            for (int x = 239; x < 303; x++)
            {
                int h = (int)(14 - System.Math.Abs(x - 272) * 0.45f);
                if (h > 0) c.Rect(x, 95, 1, h, Art.Hex(0x101425));
            }
            c.Rect(262, 107, 20, 4, Art.Hex(0x2a2e40));
            c.Rect(266, 111, 3, 3, Art.Hex(0xffd860));
            c.Rect(276, 111, 3, 3, Art.Hex(0xffd860));
            c.Rect(270, 92, 2, 60, Art.Hex(0x14101a));
            c.Rect(236, 121, 70, 2, Art.Hex(0x14101a));
            // a crew poster, torn
            c.Rect(30, 100, 26, 34, Art.Hex(0xb24a5a));
            c.Rect(36, 108, 14, 16, Art.Hex(0xe8c088));
            c.Rect(48, 100, 8, 12, Art.Hex(0x2b2236));
            // the floor
            c.Rect(0, 0, StageW, Floor, Art.Hex(0x4a3a30));
            for (int x = 0; x < StageW; x += 16) c.Rect(x, 0, 1, Floor, Art.Hex(0x3c2e26));
            c.Rect(0, Floor - 1, StageW, 1, Art.Hex(0x6a5444));
            // the gaming setup, wrecked: a cracked screen on a low table
            c.Rect(108, Floor, 40, 14, Art.Hex(0x5a3e2e));
            c.Rect(112, Floor + 14, 32, 22, Art.Hex(0x101014));
            c.Rect(114, Floor + 16, 28, 18, Art.Hex(0x1e3a48));
            for (int i = 0; i < 12; i++) c.Set(118 + i * 2, Floor + 30 - i, Art.Hex(0xd0f0ff));
            // controllers on the floor
            c.Rect(92, Floor, 7, 3, Art.Hex(0x22222a));
            c.Rect(156, Floor, 7, 3, Art.Hex(0xe8e8ee));
            // the couch, flipped on its back
            c.Rect(166, Floor, 52, 10, Art.Hex(0x3c6a5a));
            c.Rect(170, Floor + 10, 44, 12, Art.Hex(0x335a4c));
            c.Rect(166, Floor + 10, 6, 6, Art.Hex(0x2a4a3e));
            c.Rect(212, Floor + 10, 6, 6, Art.Hex(0x2a4a3e));
            c.Rect(174, Floor + 22, 4, 3, Art.Hex(0x1c1214));
            c.Rect(206, Floor + 22, 4, 3, Art.Hex(0x1c1214));
            // the doorway they came in by, on the left
            c.Rect(14, Floor, 22, 44, Art.Hex(0x14101a));
        }
    }
}
