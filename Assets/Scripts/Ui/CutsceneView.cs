using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PooketSlug
{
    /// <summary>
    /// The cutscenes (<c>docs/design/story.md</c>, Cutscenes): draws a <see cref="CutscenePlayer"/>'s scene in uGUI, over
    /// the game. It's the game's own sprites on painted backdrops, with speech bubbles, a caption card, a SKIP button once
    /// the scene's been seen, and in co-op a line saying who's still to tap. The staging (where everyone stands and
    /// moves) is written per shot for each scene: the opening (<c>docs/design/story.md</c>, The opening scene) and the
    /// motor pool reveal (The motor pool reveal).
    /// <para>
    /// The stage is 320 x 180 art pixels drawn at 2x, so the soldiers are the size they are in the game, and it's
    /// letterboxed on screens of other shapes.
    /// </para>
    /// </summary>
    public sealed class CutsceneView
    {
        const float Px = 2f; // HUD units per art pixel
        const float FlickerFor = 0.3f; // the Trollograms' static pulse over the whole stage: one, fading
        const int StageW = 320, StageH = 180, Street = 26, Floor = 22;
        // the motor pool: its floor, the shutter's opening, and where the row's tabks park (the first space is kie's)
        const float MotorFloor = 18f, MotorOpenX = 100f, MotorOpenW = 216f, MotorOpenH = 100f;
        static readonly float[] MotorRow = { 186f, 236f, 286f };
        const float KieSpace = 136f;
        // the crew's paint on the parked tabks: tones2's pink, then two others
        static readonly Color[] RowPaint = { new Color(1f, 0.62f, 0.8f), new Color(0.6f, 0.78f, 1f), new Color(1f, 0.9f, 0.5f) };

        static readonly Color Ink = new Color(0.1f, 0.07f, 0.1f), Paper = new Color(1f, 0.98f, 0.92f);
        static readonly Color Cyan = new Color(0.45f, 1f, 1f), Warm = new Color(1f, 0.78f, 0.38f), Gold = new Color(1f, 0.85f, 0.3f);
        static readonly Color Water = new Color(0.45f, 0.75f, 1f);

        readonly RectTransform m_Root, m_Stage, m_World;
        readonly Image m_Fade, m_Flicker, m_Back, m_Window, m_Door, m_DoorGap, m_Bag, m_Cup, m_Card, m_Spark;
        readonly Image[] m_Snow = new Image[36], m_Chips = new Image[10], m_Jet = new Image[8], m_Beams = new Image[2];
        readonly Figure m_Tones2, m_Goon, m_Kie;
        // the motor pool: its shutter, the crew's tabks in a row, and kie's rolling in with Trollogram kie in the hatch
        readonly RectTransform m_ShutterMask, m_KieTabk, m_KieTurret;
        readonly Image m_Shutter, m_ColdLight, m_KieHull;
        readonly Image[] m_Row = new Image[3], m_Junk = new Image[9], m_Steam = new Image[4], m_Lamps = new Image[3];
        // freeing kie: the cage of light, his dazed stars, the clamp on tones2's tabk, the cargo drone and its cable,
        // the call-in remote, and the alarm's beacon and red wash
        readonly Image[] m_Cage = new Image[7], m_Stars = new Image[3];
        readonly Image m_Clamp, m_Drone, m_Rotor, m_Cable, m_Remote, m_Beacon, m_Alarm;
        readonly Text m_Zz;
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
        string m_LastSaid, m_WaitingNote;
        readonly List<Bubble> m_Bubbles = new List<Bubble>();
        readonly List<Rect> m_Placed = new List<Rect>(); // this frame's bubbles so far, so the next doesn't sit on them
        readonly List<CutsceneLine> m_Lines = new List<CutsceneLine>();
        static Sprite s_Street, s_Room, s_MotorPool, s_Slats, s_White;
        // the camera between intros: how far in, and the world point (from its middle, art pixels) it centres on
        float m_Zoom = 1f;
        Vector2 m_Look;

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

            // the motor pool: lamps over the row, the row itself, the cold light the shutter lets out, and the shutter
            for (int i = 0; i < m_Lamps.Length; i++)
            {
                m_Lamps[i] = Picture("Work light", null, MotorRow[i] + 6f, MotorFloor + 6f, 46f, 74f, new Color(1f, 0.95f, 0.75f, 0.12f));
            }
            for (int i = 0; i < m_Row.Length; i++) m_Row[i] = Picture("Tabk", Art.Tank, 0f, 0f, Art.Tank.rect.width, Art.Tank.rect.height, Color.white);
            for (int i = 0; i < m_Junk.Length; i++) m_Junk[i] = Picture("Trollogram junk", null, 0f, 0f, 3f, 2f, Cyan);
            for (int i = 0; i < m_Steam.Length; i++) m_Steam[i] = Picture("Steam", null, 0f, 0f, 5f, 5f, new Color(0.85f, 0.88f, 0.95f, 0.5f));
            m_Kie = new Figure(m_World, "Trollogram kie");
            m_KieTabk = Ui.Rect(m_World, "kie's tabk", Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
            m_KieTurret = Ui.Rect(m_KieTabk, "Turret", new Vector2(0.5f, 0f), new Vector2(Art.TankBarrel.pivot.x / Art.TankBarrel.rect.width, 0.5f),
                new Vector2(Art.TankBarrelPivot.x, Art.TankBarrelPivot.y) * Art.PPU * Px, new Vector2(Art.TankBarrel.rect.width, Art.TankBarrel.rect.height) * Px);
            Ui.Picture(m_KieTurret, "Barrel", Art.TankBarrel, Ui.Middle, Ui.Middle, Vector2.zero, new Vector2(Art.TankBarrel.rect.width, Art.TankBarrel.rect.height) * Px, Color.white);
            m_KieHull = Ui.Picture(m_KieTabk, "Hull", Art.TankHull, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(Art.TankHull.rect.width, Art.TankHull.rect.height) * Px, Color.white);
            m_Clamp = Picture("Trollogram clamp", null, 0f, 0f, 30f, 6f, Cyan);
            m_Cable = Picture("Drone cable", null, 0f, 0f, 1f, 10f, new Color(0.1f, 0.1f, 0.12f));
            m_Drone = Picture("Cargo drone", null, 0f, 0f, 40f, 10f, new Color(0.32f, 0.35f, 0.4f));
            m_Rotor = Picture("Rotors", null, 0f, 0f, 52f, 2f, new Color(0.75f, 0.8f, 0.85f, 0.7f));
            for (int i = 0; i < m_Cage.Length; i++) m_Cage[i] = Picture("Cage of light", null, 0f, 0f, 1f, 1f, Cyan);
            for (int i = 0; i < m_Stars.Length; i++) m_Stars[i] = Picture("Dazed", null, 0f, 0f, 2f, 2f, Gold);
            m_Remote = Picture("Remote", null, 0f, 0f, 4f, 6f, new Color(0.9f, 0.3f, 0.3f));
            m_Beacon = Picture("Alarm beacon", null, 94f, MotorFloor + 108f, 8f, 5f, new Color(1f, 0.2f, 0.15f));
            m_Zz = Ui.Label(m_World, "Zz", Vector2.zero, Vector2.zero, 14, TextAnchor.MiddleCenter, Paper);
            m_Zz.text = "z Z";
            m_ColdLight = Picture("Cold light", null, MotorOpenX + MotorOpenW / 2f, 0f, MotorOpenW + 30f, MotorFloor + 4f, new Color(0.7f, 0.95f, 1f, 0f));
            m_ShutterMask = Ui.Rect(m_World, "Shutter", Vector2.zero, Vector2.zero, new Vector2(MotorOpenX, MotorFloor) * Px, new Vector2(MotorOpenW, MotorOpenH) * Px);
            m_ShutterMask.pivot = Vector2.zero;
            m_ShutterMask.anchoredPosition = new Vector2(MotorOpenX, MotorFloor) * Px;
            m_ShutterMask.gameObject.AddComponent<RectMask2D>();
            m_Shutter = Ui.Picture(m_ShutterMask, "Slats", White, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(MotorOpenW, MotorOpenH) * Px, Color.white);
            m_Shutter.rectTransform.pivot = Vector2.zero;

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

            m_Alarm = Ui.Picture(m_Stage, "Alarm", null, Ui.Middle, Ui.Middle, Vector2.zero, Vector2.zero, new Color(1f, 0.1f, 0.05f, 0f));
            Stretch(m_Alarm.rectTransform);
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
            m_Tapped = m_SkipPressed = false;
            m_LastSaid = m_WaitingNote = null;
            m_Clock = 0f;
            m_LastNow = Time.unscaledTime;
            if (s_Street == null)
            {
                s_Street = Backdrop(DrawStreet);
                s_Room = Backdrop(DrawRoom);
                s_MotorPool = Backdrop(DrawMotorPool);
                var slats = new PixelCanvas((int)MotorOpenW, (int)MotorOpenH);
                DrawShutter(slats);
                s_Slats = Sprite.Create(Art.ToTexture(slats), new Rect(0, 0, MotorOpenW, MotorOpenH), Vector2.zero, 1f);
                m_Shutter.sprite = s_Slats;
            }
            m_Zoom = 1f;
            m_Look = Vector2.zero;
            Ui.Show(m_Root, true);
        }

        public void Hide()
        {
            Sfx.Hush();
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

        /// <summary>Plays the sounds and the one-off staging for cues that just came due (<see cref="Cues"/>).</summary>
        public void Cue(string id)
        {
            if (Cues.TryEffect(id, out CueEffect e))
            {
                Play(e.Sound, e.Volume);
                Play(e.Sound2, e.Volume2);
                if ((e.Staging & CueStaging.Flicker) != 0) m_FlickerUntil = m_Clock + FlickerFor;
                if ((e.Staging & CueStaging.GoonGlitches) != 0) m_GoonDiedAt = m_Player.T;
            }
            else if (Cues.SaidLine(id) is string line) Sfx.Say(line);
            else Log.Game.Warn("Cutscene cue with nothing to do: " + id);
        }

        static void Play(CueSound sound, float volume)
        {
            AudioClip clip = ClipOf(sound);
            if (clip != null) Sfx.Play(clip, volume);
        }

        static AudioClip ClipOf(CueSound sound)
        {
            switch (sound)
            {
                case CueSound.Thump: return Sfx.Thump;
                case CueSound.Glitch: return Sfx.Glitch;
                case CueSound.Splat: return Sfx.Splat;
                case CueSound.Pickup: return Sfx.Pickup;
                case CueSound.Hit: return Sfx.Hit;
                case CueSound.Servo: return Sfx.Servo;
                case CueSound.RocketRumble: return Sfx.RocketRumble;
                case CueSound.Charge: return Sfx.Charge;
                case CueSound.Siren: return Sfx.Siren;
                case CueSound.IntroRise: return Sfx.IntroRise;
                case CueSound.Intro: return Sfx.Intro;
                default: return null;
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

            // a quick cut between shots, and the Trollograms' static: one soft cyan pulse that fades, never a strobe
            // (it's timed on the scene's own clock; it was once timed against the real one, and strobed for the rest
            // of the scene)
            m_Fade.color = new Color(0f, 0f, 0f, Mathf.Clamp01(1f - t / 0.18f));
            float flicker = now < m_FlickerUntil ? 0.3f * (m_FlickerUntil - now) / FlickerFor : 0f;
            m_Flicker.color = new Color(0.4f, 1f, 1f, flicker);

            p.Showing(m_Lines);
            int bubbles = 0;
            bool card = false;
            m_Placed.Clear();
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
            Ui.Show(m_Waiting, p.Waiting || m_WaitingNote != null);
            if (m_WaitingNote != null) m_Waiting.text = m_WaitingNote;
            else if (p.Waiting) m_Waiting.text = p.HasTapped(0) ? "WAITING FOR YOUR PARTNER" : "YOUR PARTNER'S READY: TAP TO GO ON";
        }

        /// <summary>
        /// A character's intro (<c>docs/design/story.md</c>, Character intros). First the build-up: the camera creeps in
        /// on them and the scene darkens a little as the riser climbs, with a shake as it peaks. Then it lands: the camera
        /// crash-zooms onto them, there's a white flash, their name slams up on a band, and it all eases back out as the
        /// slow-mo ends.
        /// </summary>
        void Intro(CutscenePlayer p)
        {
            const float Creep = 1.18f;
            CutsceneIntro intro = p.Current.Intro;
            bool on = p.Introducing && intro != null;
            float build = p.BuildingUp;
            Ui.Show(m_IntroBand, on);
            Figure who = intro != null ? FigureFor(intro.Who) : m_Tones2;
            Vector2 half = new Vector2(StageW, StageH) * Px / 2f;
            Vector2 focus = who.Rt.anchoredPosition + new Vector2(0f, 16f * Px) - half; // their middle, from the world's centre
            if (!on)
            {
                // the build-up: a slow creep in on them (they stay put on screen), a darkening, and a shake at the peak
                float creep = Mathf.Lerp(1f, Creep, build * build);
                Vector2 shake = build > 0.8f ? new Vector2(Mathf.Sin(m_Clock * 90f), Mathf.Cos(m_Clock * 70f)) * (build - 0.8f) * 10f : Vector2.zero;
                if (build <= 0f)
                {
                    // the shot's own camera: centred on m_Look, m_Zoom in
                    m_World.localScale = new Vector3(m_Zoom, m_Zoom, 1f);
                    m_World.anchoredPosition = -m_Look * Px * m_Zoom;
                    m_Flash.color = new Color(0f, 0f, 0.05f, 0f);
                    return;
                }
                m_World.localScale = new Vector3(creep, creep, 1f);
                m_World.anchoredPosition = focus - focus * creep + shake;
                m_Flash.color = new Color(0f, 0f, 0.05f, 0.3f * build * build);
                return;
            }
            float k = p.IntroT, end = CutscenePlayer.IntroSeconds;
            float zoomIn = Mathf.Clamp01(k / 0.12f), zoomOut = Mathf.Clamp01((end - k) / 0.3f);
            float e = Mathf.Min(1f - (1f - zoomIn) * (1f - zoomIn), zoomOut * zoomOut * (3f - 2f * zoomOut));

            Vector2 target = new Vector2(-StageW * Px * 0.22f, -12f); // where they end up: left of centre, the name to their right
            float scale = Mathf.Lerp(k < end / 2f ? Creep : 1f, 2.6f, e); // on from the creep, back out to where it started
            m_World.localScale = new Vector3(scale, scale, 1f);
            m_World.anchoredPosition = Vector2.Lerp(focus, target, e) - focus * scale; // they stay where the creep had them

            m_Flash.color = new Color(1f, 1f, 1f, Mathf.Clamp01(0.85f - k / 0.18f));
            m_IntroName.text = intro.Name;
            float slam = Mathf.Clamp01((k - 0.08f) / 0.1f);
            float size = k < 0.08f ? 0f : Mathf.Lerp(2.4f, 1f, slam);
            m_IntroBand.localScale = new Vector3(size * zoomOut, size, 1f);
        }

        /// <summary>The narrator reads each of their lines as it comes up, with its subtitle.</summary>
        void Voice(string line)
        {
            if (line == m_LastSaid) return;
            m_LastSaid = line;
            if (line != null) Sfx.Narrate(line);
        }

        /// <summary>In co-op: a line saying what the scene's waiting on, over the usual one, or null for that.</summary>
        public void Waiting(string note) => m_WaitingNote = note;

        /// <summary>Who a line or an intro belongs to on this scene's stage.</summary>
        Figure FigureFor(Speaker who)
        {
            bool motorPool = m_Player != null && m_Player.Scene == Cutscenes.MotorPool;
            switch (who)
            {
                case Speaker.Goon: return m_Goon;
                case Speaker.Hologram: return motorPool ? m_Kie : m_Goon;
                case Speaker.Kie: return m_Kie;
                default: return m_Tones2;
            }
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

            Figure who = FigureFor(line.Who);
            Vector2 at = who.Head + new Vector2(0f, 16f);
            // kept on screen, where the shot's camera has it (its bubble shrinks with the zoom, so it's in world units)
            float zoom = m_World.localScale.x, half = w / 2f + 4f, view = StageW * Px / 2f / zoom;
            float middle = StageW * Px / 2f - m_World.anchoredPosition.x / zoom;
            at.x = Mathf.Clamp(at.x, middle - view + half, middle + view - half);
            // never on top of another bubble: comic-book style, a later one stacks above the ones already up, or below
            // them if there's no room left above in the shot
            float top = StageH * Px / 2f - m_World.anchoredPosition.y / zoom + StageH * Px / 2f / zoom;
            at.y = Stack(at.x, at.y, w, top);
            m_Placed.Add(new Rect(at.x - w / 2f, at.y, w, BubbleH));
            if (glitchy && (int)(now * 12f) % 5 == 0) at.x += 2f; // the holograms' bubbles glitch too
            b.Rt.anchoredPosition = at;
            b.Tail.rectTransform.anchoredPosition = new Vector2(Mathf.Clamp(who.Head.x - at.x, -w / 2f + 8f, w / 2f - 8f), 0f);
        }

        const float BubbleH = 22f, BubbleGap = 4f;

        /// <summary>The height for a bubble at <paramref name="x"/> that clears every bubble already placed this frame.</summary>
        float Stack(float x, float y, float w, float top)
        {
            for (int step = 0; step < 8; step++)
            {
                float up = y + step * (BubbleH + BubbleGap);
                if (up + BubbleH <= top - 2f && Clear(x, up, w)) return up;
            }
            for (int step = 1; step < 8; step++)
            {
                float down = y - step * (BubbleH + BubbleGap);
                if (Clear(x, down, w)) return down;
            }
            return y;
        }

        bool Clear(float x, float y, float w)
        {
            foreach (Rect r in m_Placed)
                if (Mathf.Abs(x - r.center.x) < (w + r.width) / 2f + BubbleGap && Mathf.Abs(y - r.y) < BubbleH + BubbleGap)
                    return false;
            return true;
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
            m_Zoom = 1f;
            m_Look = Vector2.zero;
            bool motorPool = m_Player.Scene == Cutscenes.MotorPool, freeing = m_Player.Scene == Cutscenes.FreeingKie;
            ShowMotorPool(motorPool || freeing);
            ShowFreeing(freeing);
            if (motorPool) StageMotorPool(shot, t, now);
            else if (freeing) StageFreeing(shot, t, now);
            else StageOpening(shot, t, now);
        }

        void StageOpening(int shot, float t, float now)
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
                    // he walks on for a few seconds before his intro lands (at 4 s), and on to the door after it
                    float x = Mathf.Lerp(-16f, 196f, Mathf.Clamp01(t / 5.6f));
                    Soldier(x, Street, t < 5.6f, t);
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

        // ------------------------------------------------------------------ the motor pool reveal's staging

        /// <summary>Shows the motor pool's own things, or hides them (and the opening's) for the other scene.</summary>
        void ShowMotorPool(bool on)
        {
            Ui.Show(m_ShutterMask, on);
            Ui.Show(m_ColdLight, on);
            Ui.Show(m_KieTabk, on);
            Ui.Show(m_Kie.Rt, false);
            foreach (Image i in m_Row) Ui.Show(i, on);
            foreach (Image i in m_Lamps) Ui.Show(i, on);
            foreach (Image i in m_Junk) Ui.Show(i, false);
            foreach (Image i in m_Steam) Ui.Show(i, false);
            if (!on) return;
            m_Back.sprite = s_MotorPool;
            Ui.Show(m_Sign, false);
            Ui.Show(m_Window, false);
            Ui.Show(m_Door, false);
            Ui.Show(m_DoorGap, false);
            for (int i = 0; i < m_Snow.Length; i++) Snow(i, m_Clock, false);
            Ui.Show(m_Goon.Rt, false);
            Ui.Show(m_Card, false);
            Ui.Show(m_Dim, false);
            Ui.Show(m_BigCard, false);
            Ui.Show(m_Spark, false);
            foreach (Image j in m_Jet) Ui.Show(j, false);
            foreach (Image b in m_Beams) Ui.Show(b, false);
            foreach (Image c in m_Chips) Ui.Show(c, false);
            Ui.Show(m_Bag, false);
            Ui.Show(m_Cup, false);
            Ui.Show(m_Tones2.Rt, true);
        }

        void StageMotorPool(int shot, float t, float now)
        {
            // the shutter: down at first, grinding up through shot 1, and up from then on
            float up = shot == 0 ? Mathf.Clamp01((t - 0.4f) / 2.6f) : 1f;
            float grind = shot == 0 && up > 0f && up < 1f ? ((int)(now * 16f) % 2) * 1f : 0f; // it judders as it goes
            m_Shutter.rectTransform.anchoredPosition = new Vector2(0f, (up * MotorOpenH + grind)) * Px;
            m_ColdLight.color = new Color(0.7f, 0.95f, 1f, 0.22f * up);
            for (int i = 0; i < m_Lamps.Length; i++)
            {
                bool flick = (int)(now * 11f + i * 5) % 17 == 0; // a work light stutters now and then
                m_Lamps[i].color = new Color(1f, 0.95f, 0.75f, up * (flick ? 0.05f : 0.14f));
            }
            Row(now);

            // kie's space at the front: steaming paint scuffs until his tabk rolls back into it in shot 3
            if (shot <= 1 || shot == 2 && t < 1.8f) Steam(now);

            // kie's tabk: out of sight, then rolling in from the right into its space in shot 3, the hatch popping
            Ui.Show(m_KieTabk, shot >= 2);
            float tabkX = KieSpace;
            if (shot == 2)
            {
                float roll = Mathf.Clamp01((t - 0.2f) / 1.8f);
                tabkX = Mathf.Lerp(352f, KieSpace, 1f - (1f - roll) * (1f - roll));
                if (roll < 1f) tabkX += (int)(now * 20f) % 2 * 0.5f; // its engine shakes it
            }
            m_KieTabk.anchoredPosition = new Vector2(tabkX, MotorFloor) * Px;
            m_KieHull.color = Color.white;
            // the turret points up and away, then swings round on to tones2 once the hatch has shut
            float swing = shot == 3 ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 1.0f) / 0.7f)) : 0f;
            m_KieTurret.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-150f, 6f, swing));

            // Trollogram kie: up out of the hatch in shot 3, flickering cyan; back down and the hatch slams in shot 4
            float rise = shot == 2 ? Mathf.Clamp01((t - 2.1f) / 0.3f) : shot == 3 ? 1f - Mathf.Clamp01((t - 0.2f) / 0.25f) : 0f;
            if (rise > 0f) Trollo(tabkX, rise, now);

            // tones2: in from the left in shot 1, then stood watching
            float x = shot == 0 ? Mathf.Lerp(-14f, 58f, Mathf.Clamp01(t / 1.8f)) : 58f;
            Soldier(x, MotorFloor, shot == 0 && t < 1.8f, t);
            m_Tones2.Rt.localScale = Vector3.one;

            // shot 2's camera eases right and in, on the row
            if (shot == 1)
            {
                float ease = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 2.2f));
                m_Zoom = Mathf.Lerp(1f, 1.15f, ease);
                m_Look = new Vector2(Mathf.Lerp(0f, 20f, ease), Mathf.Lerp(0f, -12f, ease));
            }
        }

        // ------------------------------------------------------------------ freeing kie's staging

        void ShowFreeing(bool on)
        {
            foreach (Image i in m_Cage) Ui.Show(i, false);
            foreach (Image i in m_Stars) Ui.Show(i, false);
            Ui.Show(m_Clamp, false);
            Ui.Show(m_Drone, false);
            Ui.Show(m_Rotor, false);
            Ui.Show(m_Cable, false);
            Ui.Show(m_Remote, false);
            Ui.Show(m_Beacon, on);
            Ui.Show(m_Zz, false);
            Ui.Show(m_Alarm, false);
            m_Beacon.color = new Color(0.35f, 0.08f, 0.06f);
        }

        /// <summary>
        /// The motor pool after the fight: the shutter up, kie's tabk a smoking wreck in its space, the row behind. The
        /// real kie drops out of a cage of light, takes the snacks, frees tones2's tabk (the cargo drone hauls it out
        /// through the roof) and hands over its remote, then sits down against his wreck and nods off as the alarms go.
        /// </summary>
        void StageFreeing(int shot, float t, float now)
        {
            const float KieAt = 100f, Tones2At = 40f;
            m_Shutter.rectTransform.anchoredPosition = new Vector2(0f, MotorOpenH) * Px;
            m_ColdLight.color = new Color(0.7f, 0.95f, 1f, 0.22f);
            for (int i = 0; i < m_Lamps.Length; i++) m_Lamps[i].color = new Color(1f, 0.95f, 0.75f, 0.14f);
            Row(now);
            foreach (Image j in m_Junk) Ui.Show(j, false); // the Trollograms' junk went with him
            // kie's tabk, wrecked: dark, smoking, its gun drooping
            m_KieTabk.anchoredPosition = new Vector2(KieSpace, MotorFloor) * Px;
            m_KieHull.color = new Color(0.4f, 0.37f, 0.36f);
            m_KieTurret.localRotation = Quaternion.Euler(0f, 0f, 14f);
            Steam(now);
            for (int i = 0; i < m_Steam.Length; i++) m_Steam[i].color = new Color(0.3f, 0.3f, 0.32f, m_Steam[i].color.a);

            // tones2's tabk (first in the row) wears the Trollograms' clamp until kie rips it off; then the drone takes it
            float lift = shot == 2 ? Mathf.Clamp01((t - 2.7f) / 1.2f) : shot > 2 ? 1f : 0f;
            Ui.Show(m_Row[0], lift < 1f);
            m_Row[0].rectTransform.anchoredPosition = new Vector2(MotorRow[0], MotorFloor + 2f + lift * lift * 150f) * Px;
            float rip = shot == 2 ? Mathf.Clamp01((t - 1.2f) / 0.5f) : shot > 2 ? 1f : 0f;
            if (rip < 1f)
            {
                Ui.Show(m_Clamp, true);
                m_Clamp.color = new Color(0.45f, 1f, 1f, (int)(now * 6f) % 4 == 0 ? 0.5f : 0.9f);
                m_Clamp.rectTransform.anchoredPosition = new Vector2(MotorRow[0] - rip * 40f, MotorFloor + 12f + Mathf.Sin(rip * Mathf.PI) * 24f) * Px;
                m_Clamp.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rip * 220f);
            }
            if (shot == 2 && t >= 1.6f && lift < 1f)
            {
                // the cargo drone drops through the roof hatch, hooks it, and hauls it up and out
                float down = Mathf.Clamp01((t - 1.6f) / 0.8f);
                float y = Mathf.Lerp(StageH + 10f, MotorFloor + 44f, 1f - (1f - down) * (1f - down)) + lift * lift * 150f;
                Ui.Show(m_Drone, true);
                Ui.Show(m_Rotor, true);
                Ui.Show(m_Cable, down >= 1f);
                m_Drone.rectTransform.anchoredPosition = new Vector2(MotorRow[0], y) * Px;
                m_Rotor.rectTransform.anchoredPosition = new Vector2(MotorRow[0] + ((int)(now * 20f) % 2) * 2f, y + 10f) * Px;
                m_Cable.rectTransform.anchoredPosition = new Vector2(MotorRow[0], MotorFloor + 22f + lift * lift * 150f) * Px;
                m_Cable.rectTransform.sizeDelta = new Vector2(1f, y - (MotorFloor + 22f + lift * lift * 150f)) * Px;
            }

            // tones2: stood back at first, then over with the snacks
            float tx = shot == 0 ? Tones2At : shot == 1 ? Mathf.Lerp(Tones2At, 74f, Mathf.Clamp01(t / 0.7f)) : 74f;
            Soldier(tx, MotorFloor, shot == 1 && t < 0.7f, t);
            m_Tones2.Rt.localScale = Vector3.one;
            Ui.Show(m_Bag, shot == 0 || shot == 1 && t < 2.2f);
            Ui.Show(m_Cup, false);

            // kie
            Ui.Show(m_Kie.Rt, true);
            PlayerSkin skin = Art.KieSkins[0];
            Ui.Show(m_Kie.Legs, true);
            m_Kie.Legs.color = m_Kie.Torso.color = Color.white;
            m_Kie.Torso.sprite = skin.TorsoForward;
            m_Kie.Torso.rectTransform.anchoredPosition = Vector2.zero;
            float kx = KieAt, ky = MotorFloor;
            bool down2 = false, walking = false;
            int facing = -1; // at tones2
            switch (shot)
            {
                case 0: // held in the cage of light, which flickers out, and he drops in a heap
                {
                    PilotFreeingSpec spec = KieTuning.Freeing;
                    bool lit = spec.CageLit(t);
                    float fall = Mathf.Clamp01((t - spec.Cage) / 0.3f);
                    ky = MotorFloor + 10f * (1f - fall * fall);
                    down2 = t >= spec.Cage;
                    Cage(KieAt, lit);
                    if (t >= spec.Cage + 0.3f) Stars(kx, ky + 22f, now);
                    break;
                }
                case 1: // up off the floor, swaying, and he takes the snacks
                    down2 = t < 1.4f;
                    if (!down2) Stars(kx, ky + 30f, now);
                    if (t >= 2.2f)
                    {
                        Ui.Show(m_Bag, true);
                        m_Bag.rectTransform.anchoredPosition = new Vector2(kx - 7f, ky + 9f) * Px;
                    }
                    break;
                case 2: // over to tones2's tabk to rip off the clamp, then back with the remote
                    walking = t < 1.0f || t > 2.6f && t < 3.4f;
                    kx = t < 1.0f ? Mathf.Lerp(KieAt, MotorRow[0] - 20f, t) : t < 2.6f ? MotorRow[0] - 20f : Mathf.Lerp(MotorRow[0] - 20f, 96f, Mathf.Clamp01((t - 2.6f) / 0.8f));
                    facing = t < 2.6f ? 1 : -1;
                    if (t >= 3.4f && t < 4.6f)
                    {
                        // the remote, pressed into tones2's hand
                        float give = Mathf.Clamp01((t - 4.0f) / 0.3f);
                        Ui.Show(m_Remote, true);
                        m_Remote.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(kx - 8f, 84f, give), MotorFloor + 12f) * Px;
                    }
                    break;
                case 3: // sits down hard against his wreck, the snacks in his lap, and nods off
                {
                    float sit = Mathf.Clamp01(t / 0.6f);
                    walking = sit < 1f;
                    kx = Mathf.Lerp(96f, KieSpace - 30f, sit);
                    down2 = sit >= 1f;
                    facing = 1;
                    Ui.Show(m_Bag, down2);
                    m_Bag.rectTransform.anchoredPosition = new Vector2(kx + 6f, ky + 5f) * Px;
                    if (t > 2.4f)
                    {
                        Ui.Show(m_Zz, true);
                        float k = (t - 2.4f) % 1.6f / 1.6f;
                        m_Zz.rectTransform.anchoredPosition = new Vector2(kx + 6f + k * 8f, ky + 26f + k * 10f) * Px;
                        m_Zz.color = new Color(Paper.r, Paper.g, Paper.b, 1f - k);
                    }
                    // the alarm: a beacon turning over the shutter, and a slow red wash (once a second: never a strobe)
                    float pulse = 0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f);
                    m_Beacon.color = Color.Lerp(new Color(0.35f, 0.08f, 0.06f), new Color(1f, 0.25f, 0.15f), pulse);
                    Ui.Show(m_Alarm, true);
                    m_Alarm.color = new Color(1f, 0.1f, 0.05f, 0.1f * pulse);
                    break;
                }
            }
            m_Kie.Legs.sprite = down2 ? skin.LegsCrouch : walking ? skin.LegsRun[(int)(t * 8f) % skin.LegsRun.Length] : skin.LegsIdle;
            if (shot == 1 && !down2 || shot == 2) kx += Mathf.Round(Mathf.Sin(t * 3f) * 1.4f); // swaying, still dazed
            m_Kie.Rt.localScale = new Vector3(facing, 1f, 1f);
            m_Kie.At(kx, ky);
        }

        /// <summary>The cage of light round the real kie: bars and two rails, lit or out.</summary>
        void Cage(float x, bool lit)
        {
            const float W = 18f, H = 30f;
            for (int i = 0; i < m_Cage.Length; i++)
            {
                Ui.Show(m_Cage[i], lit);
                if (!lit) continue;
                bool rail = i >= 5;
                RectTransform rt = m_Cage[i].rectTransform;
                rt.sizeDelta = (rail ? new Vector2(W + 2f, 1f) : new Vector2(1f, H)) * Px;
                rt.anchoredPosition = new Vector2(rail ? x : x - W / 2f + i * (W / 4f), MotorFloor + (rail && i == 6 ? H : 0f)) * Px;
            }
        }

        void Stars(float x, float y, float now)
        {
            for (int i = 0; i < m_Stars.Length; i++)
            {
                float a = now * 4f + i * Mathf.PI * 2f / m_Stars.Length;
                Ui.Show(m_Stars[i], true);
                m_Stars[i].rectTransform.anchoredPosition = new Vector2(x + Mathf.Cos(a) * 7f, y + Mathf.Sin(a) * 2f) * Px;
            }
        }

        /// <summary>The crew's tabks parked in a row under the work lights, in their paint, with Trollogram junk all over them.</summary>
        void Row(float now)
        {
            for (int i = 0; i < m_Row.Length; i++)
            {
                m_Row[i].color = RowPaint[i];
                m_Row[i].rectTransform.anchoredPosition = new Vector2(MotorRow[i], MotorFloor + 2f) * Px;
            }
            for (int i = 0; i < m_Junk.Length; i++)
            {
                float x = MotorRow[i % m_Row.Length] - 14f + (i * 11 % 26);
                float y = MotorFloor + 8f + (i * 7 % 13);
                bool lit = (int)(now * 8f + i * 3) % 6 != 0;
                Ui.Show(m_Junk[i], true);
                m_Junk[i].color = new Color(0.45f, 1f, 1f, lit ? 0.9f : 0.25f);
                m_Junk[i].rectTransform.anchoredPosition = new Vector2(x, y) * Px;
            }
        }

        void Steam(float now)
        {
            for (int i = 0; i < m_Steam.Length; i++)
            {
                float k = (now * 0.5f + i / (float)m_Steam.Length) % 1f;
                Ui.Show(m_Steam[i], true);
                m_Steam[i].color = new Color(0.85f, 0.88f, 0.95f, 0.45f * (1f - k));
                m_Steam[i].rectTransform.anchoredPosition = new Vector2(KieSpace - 12f + i * 8f + Mathf.Sin(now * 2f + i) * 2f, MotorFloor + k * 22f) * Px;
            }
        }

        /// <summary>Trollogram kie in his tabk's hatch: kie, but cyan, flickering and grinning; <paramref name="rise"/> 0..1 is how far up he is.</summary>
        void Trollo(float tabkX, float rise, float now)
        {
            Ui.Show(m_Kie.Rt, true);
            PlayerSkin skin = Art.KieSkins[0];
            m_Kie.Legs.sprite = skin.LegsIdle;
            m_Kie.Torso.sprite = skin.TorsoForward;
            Ui.Show(m_Kie.Legs, false); // he's stood in the hatch: only his top half shows
            bool flick = (int)(now * 30f) % 9 == 0;
            m_Kie.Torso.color = new Color(0.45f, 1f, 1f, flick ? 0.35f : 0.85f);
            float jitter = (int)(now * 20f) % 7 == 0 ? 1f : 0f;
            m_Kie.Rt.localScale = new Vector3(-1f, 1f, 1f); // faces left, at tones2
            m_Kie.At(tabkX + 4f + jitter, MotorFloor + Mathf.Lerp(-2f, 12f, rise));
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

        /// <summary>The motor pool, from outside: the base's wall at night, and the big opening the shutter covers, with the bay behind it.</summary>
        static void DrawMotorPool(PixelCanvas c)
        {
            int floor = (int)MotorFloor, ox = (int)MotorOpenX, ow = (int)MotorOpenW, oh = (int)MotorOpenH;
            Sky(c, 0x0b0f24, 0x1d2448);
            var rng = new System.Random(11);
            for (int i = 0; i < 50; i++) c.Set(rng.Next(StageW), 140 + rng.Next(StageH - 140), Art.Hex(0xc8d0ff, (byte)(120 + rng.Next(120))));
            // the base's wall: steel panels, rivets
            c.Rect(0, floor, StageW, 128, Art.Hex(0x2c3442));
            for (int x = 0; x < StageW; x += 32) c.Rect(x, floor, 1, 128, Art.Hex(0x232a36));
            for (int y = floor + 20; y < floor + 128; y += 26)
                for (int x = 6; x < StageW; x += 16) c.Set(x, y, Art.Hex(0x4a5466));
            c.Rect(0, floor + 126, StageW, 4, Art.Hex(0x1c222c));
            // the bay behind the shutter: dark, with a back wall, a gantry and the lamps' fittings
            c.Rect(ox - 6, floor, ow + 12, oh + 8, Art.Hex(0x1a1e26)); // the frame
            c.Rect(ox, floor, ow, oh, Art.Hex(0x101318));
            for (int x = ox + 4; x < ox + ow; x += 24) c.Rect(x, floor + 10, 1, oh - 10, Art.Hex(0x161a22));
            c.Rect(ox, floor + oh - 14, ow, 3, Art.Hex(0x242a34)); // the gantry
            for (int x = ox + 6; x < ox + ow; x += 10) c.Rect(x, floor + oh - 11, 1, 6, Art.Hex(0x242a34));
            foreach (float rx in MotorRow) c.Rect((int)rx + 2, floor + oh - 14, 8, 3, Art.Hex(0x5a5a50));
            // cables snaking over the bay floor, and the paint of the parking spaces
            for (int x = ox; x < ox + ow; x++) c.Set(x, floor + 3 + (int)(System.Math.Sin(x * 0.11) * 1.5), Art.Hex(0x0a0c10));
            for (int x = ox + 12; x < ox + ow; x += 50) c.Rect(x, floor, 2, 2, Art.Hex(0xc8b040));
            // hazard stripes over the opening
            for (int x = ox - 6; x < ox + ow + 6; x++)
                c.Rect(x, floor + oh + 8, 1, 6, ((x - ox) / 5) % 2 == 0 ? Art.Hex(0xe0b020) : Art.Hex(0x1a1a1a));
            c.Rect(14, floor + 54, 40, 8, Art.Hex(0x1a1e26)); // a sign plate by the door
            c.Rect(16, floor + 56, 36, 4, Art.Hex(0xc0a030));
            // the apron outside
            c.Rect(0, 0, StageW, floor, Art.Hex(0x30343e));
            c.Rect(0, floor - 1, StageW, 1, Art.Hex(0x4c5262));
            for (int x = 4; x < StageW; x += 26) c.Rect(x, 6, 14, 2, Art.Hex(0x4a4a3c));
        }

        /// <summary>The motor pool's blast shutter: steel slats with a hazard edge along the bottom.</summary>
        static void DrawShutter(PixelCanvas c)
        {
            c.Rect(0, 0, c.W, c.H, Art.Hex(0x5b5f66));
            for (int y = 3; y < c.H; y += 6) c.Rect(0, y, c.W, 1, Art.Hex(0x45484e));
            for (int y = 0; y < c.H; y += 6) c.Rect(0, y + 1, c.W, 1, Art.Hex(0x6c717a));
            for (int x = 0; x < c.W; x++) c.Rect(x, 0, 1, 4, (x / 6) % 2 == 0 ? Art.Hex(0xe0b020) : Art.Hex(0x1a1a1a));
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
