using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static PooketSlug.StageKit;

namespace PooketSlug
{
    /// <summary>
    /// The cutscenes (<c>docs/design/story.md</c>, Cutscenes): draws a <see cref="CutscenePlayer"/>'s scene in uGUI, over
    /// the game. It's the game's own sprites on painted backdrops, with speech bubbles, a caption card, a SKIP button once
    /// the scene's been seen, and in co-op a line saying who's still to tap.
    /// <para>
    /// What's on stage, where and when is the scene's own script (<c>scenes/&lt;name&gt;.txt</c>, <see cref="SceneScript"/>),
    /// worked out moment by moment by <see cref="ShotStage"/>; this view draws each thing in the vocabulary
    /// (<see cref="Staging.Things"/>) the way the script has it: the soldiers, the props, and the named effects (snow,
    /// spilt chips, the shutter, the cage of light...), whose looks and little animations live here.
    /// </para>
    /// <para>
    /// The stage is <see cref="StageKit"/>'s, shared with the den and the debrief: 320 x 180 art pixels drawn at 2x, so
    /// the soldiers are the size they are in the game, and letterboxed on screens of other shapes.
    /// </para>
    /// </summary>
    public sealed class CutsceneView
    {
        const float FlickerFor = 0.3f; // the Trollograms' static pulse over the whole stage: one, fading
        // the motor pool: the shutter's opening, and where the row's tabks park (the first space is tones2's)
        const float MotorOpenX = 100f, MotorOpenW = 216f, MotorOpenH = 100f;
        static readonly float[] MotorRow = { 186f, 236f, 286f };
        // the other crew's paint on the parked tabks (tones2's pink is his own thing's colour)
        static readonly Color[] RowPaint = { new Color(1f, 0.62f, 0.8f), new Color(0.6f, 0.78f, 1f), new Color(1f, 0.9f, 0.5f) };

        readonly RectTransform m_Root, m_Stage, m_World;
        readonly Image m_Fade, m_Flicker, m_Back;
        readonly StageFigure m_Tones2, m_Goon, m_Kie;
        // the props, by name: placed, turned, sized and coloured as the script has them
        readonly Dictionary<string, Image> m_Props = new Dictionary<string, Image>();
        readonly Text m_Sign;
        // the named effects' own pieces
        readonly Image[] m_Snow = new Image[36], m_Chips = new Image[10], m_Jet = new Image[8], m_Beams = new Image[2];
        readonly RectTransform m_ShutterMask, m_KieTabk, m_KieTurret;
        readonly Image m_Shutter, m_ColdLight, m_KieHull, m_Rotor;
        readonly Image[] m_Row = new Image[2], m_Junk = new Image[9], m_Steam = new Image[4], m_Lamps = new Image[3];
        readonly Image[] m_Cage = new Image[7], m_Stars = new Image[3];
        readonly Image m_Alarm;
        readonly Text m_Zz;
        readonly Text m_Hint, m_Waiting, m_Subtitle, m_Note;
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
        readonly List<SpeechBubble> m_Bubbles = new List<SpeechBubble>();
        readonly List<Rect> m_Placed = new List<Rect>(); // this frame's bubbles so far, so the next doesn't sit on them
        readonly List<CutsceneLine> m_Lines = new List<CutsceneLine>();
        static Sprite s_Street, s_Room, s_MotorPool, s_Slats;
        // the camera between intros: how far in, and the world point (from its middle, art pixels) it centres on
        float m_Zoom = 1f;
        Vector2 m_Look;

        CutscenePlayer m_Player;
        ShotStage m_StageOf;
        int m_StageShot = -1;
        float m_FlickerUntil;
        bool m_Tapped, m_SkipPressed;

        public CutsceneView(Transform parent)
        {
            m_Root = Ui.Canvas(parent, "Cutscene", 20);
            m_Root.gameObject.AddComponent<GraphicRaycaster>(); // taps and SKIP (the menus' event system serves it)
            Image black = Build(m_Root, out m_Stage, out m_World);
            black.raycastTarget = true;
            var tap = black.gameObject.AddComponent<Button>();
            tap.transition = Selectable.Transition.None;
            tap.navigation = new Navigation { mode = Navigation.Mode.None };
            tap.onClick.AddListener(() => m_Tapped = true);

            // everything in the scene sits in the world, which the camera's crash zoom moves; what's over it (subtitles,
            // the caption card, buttons) sits on the stage. Things are made in drawing order, back to front.
            m_Back = BackdropImage(m_World);
            Prop("window", 28f, 20f);
            m_Sign = Ui.Label(m_World, "sign", Vector2.zero, Vector2.zero, 20, TextAnchor.MiddleCenter, Color.white);
            m_Sign.text = "POOKET";
            Prop("doorway", 20f, 40f);
            Prop("door", 20f, 40f).rectTransform.pivot = new Vector2(0f, 0f); // it swings on its hinge, on the left
            for (int i = 0; i < m_Beams.Length; i++)
            {
                m_Beams[i] = Picture("Searchlight", null, 267f + i * 10f, 111f, 3f, 36f, new Color(1f, 1f, 0.8f, 0.35f));
                m_Beams[i].rectTransform.pivot = new Vector2(0.5f, 0f);
            }

            // the motor pool: lamps over the row, the row itself, the cold light the shutter lets out, and the shutter
            for (int i = 0; i < m_Lamps.Length; i++)
                m_Lamps[i] = Picture("Work light", null, MotorRow[i] + 6f, 0f, 46f, 74f, new Color(1f, 0.95f, 0.75f, 0.12f));
            m_Props["tones2-tabk"] = Picture("tones2-tabk", Art.Tank, 0f, 0f, Art.Tank.rect.width, Art.Tank.rect.height, Color.white);
            for (int i = 0; i < m_Row.Length; i++) m_Row[i] = Picture("Tabk", Art.Tank, 0f, 0f, Art.Tank.rect.width, Art.Tank.rect.height, RowPaint[i + 1]);
            for (int i = 0; i < m_Junk.Length; i++) m_Junk[i] = Picture("Trollogram junk", null, 0f, 0f, 3f, 2f, Cyan);
            for (int i = 0; i < m_Steam.Length; i++) m_Steam[i] = Picture("Steam", null, 0f, 0f, 5f, 5f, new Color(0.85f, 0.88f, 0.95f, 0.5f));
            m_Kie = new StageFigure(m_World, "kie");
            m_KieTabk = Ui.Rect(m_World, "kie-tabk", Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
            m_KieTurret = Ui.Rect(m_KieTabk, "Turret", new Vector2(0.5f, 0f), new Vector2(Art.TankBarrel.pivot.x / Art.TankBarrel.rect.width, 0.5f),
                new Vector2(Art.TankBarrelPivot.x, Art.TankBarrelPivot.y) * Art.PPU * Px, new Vector2(Art.TankBarrel.rect.width, Art.TankBarrel.rect.height) * Px);
            Ui.Picture(m_KieTurret, "Barrel", Art.TankBarrel, Ui.Middle, Ui.Middle, Vector2.zero, new Vector2(Art.TankBarrel.rect.width, Art.TankBarrel.rect.height) * Px, Color.white);
            m_KieHull = Ui.Picture(m_KieTabk, "Hull", Art.TankHull, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(Art.TankHull.rect.width, Art.TankHull.rect.height) * Px, Color.white);
            Prop("clamp", 30f, 6f);
            Prop("cable", 1f, 10f);
            Prop("drone", 40f, 10f);
            m_Rotor = Picture("Rotors", null, 0f, 0f, 52f, 2f, new Color(0.75f, 0.8f, 0.85f, 0.7f));
            for (int i = 0; i < m_Cage.Length; i++) m_Cage[i] = Picture("Cage of light", null, 0f, 0f, 1f, 1f, Cyan);
            for (int i = 0; i < m_Stars.Length; i++) m_Stars[i] = Picture("Dazed", null, 0f, 0f, 2f, 2f, Gold);
            Prop("remote", 4f, 6f);
            Prop("beacon", 8f, 5f);
            m_Zz = Ui.Label(m_World, "Zz", Vector2.zero, Vector2.zero, 14, TextAnchor.MiddleCenter, Paper);
            m_Zz.text = "z Z";
            m_ColdLight = Picture("Cold light", null, MotorOpenX + MotorOpenW / 2f, 0f, MotorOpenW + 30f, 22f, new Color(0.7f, 0.95f, 1f, 0f));
            m_ShutterMask = Ui.Rect(m_World, "Shutter", Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(MotorOpenW, MotorOpenH) * Px);
            m_ShutterMask.pivot = Vector2.zero;
            m_ShutterMask.gameObject.AddComponent<RectMask2D>();
            m_Shutter = Ui.Picture(m_ShutterMask, "Slats", White, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(MotorOpenW, MotorOpenH) * Px, Color.white);
            m_Shutter.rectTransform.pivot = Vector2.zero;

            Prop("spark", 6f, 6f);
            for (int i = 0; i < m_Chips.Length; i++) m_Chips[i] = Picture("Chip", null, 0f, 0f, 3f, 2f, new Color(1f, 0.82f, 0.3f));
            m_Goon = new StageFigure(m_World, "goon");
            m_Tones2 = new StageFigure(m_World, "tones2");
            Prop("bag", 9f, 11f);
            Prop("cup", 6f, 12f);
            Prop("card", 8f, 5f);
            for (int i = 0; i < m_Jet.Length; i++) m_Jet[i] = Picture("Water", null, 0f, 0f, 4f, 3f, new Color(0.45f, 0.75f, 1f));
            for (int i = 0; i < m_Snow.Length; i++) m_Snow[i] = Picture("Snow", null, 0f, 0f, 1.5f, 1.5f, new Color(1f, 1f, 1f, 0.85f));

            // a character's intro: a white flash, then their name on a slanted band
            m_Flash = Ui.Picture(m_Stage, "Flash", null, Ui.Middle, Ui.Middle, Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0f));
            Stretch(m_Flash.rectTransform);
            m_IntroBand = Ui.Rect(m_Stage, "Intro", Ui.Middle, Ui.Middle, new Vector2(StageW * Px * 0.2f, 28f), new Vector2(StageW * Px * 0.62f, 74f));
            var introBack = m_IntroBand.gameObject.AddComponent<Image>();
            introBack.color = Band;
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
            band.color = Band;
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
            // a draft played with ?scene=: which one, and what's wrong with it
            m_Note = Ui.Label(m_Stage, "Note", Ui.TopLeft, new Vector2(8f, -8f), 9, TextAnchor.UpperLeft, Gold);
            m_Note.rectTransform.pivot = new Vector2(0f, 1f);
            m_Note.rectTransform.sizeDelta = new Vector2(StageW * Px - 100f, 60f);
            m_Note.horizontalOverflow = HorizontalWrapMode.Wrap;
            m_Skip = Ui.Button(m_Stage, "SKIP", new Vector2(StageW * Px / 2f - 42f, StageH * Px / 2f - 20f), new Vector2(64f, 22f), () => m_SkipPressed = true, out Text skipKey);
            skipKey.text = "ESC";

            Ui.Show(m_Root, false);
        }

        Image Picture(string name, Sprite sprite, float x, float y, float w, float h, Color color) => StageKit.Picture(m_World, name, sprite, x, y, w, h, color);

        /// <summary>A plain prop, made in its own colour; placed by the script each frame.</summary>
        Image Prop(string name, float w, float h)
        {
            Image img = Picture(name, null, 0f, 0f, w, h, Staging.Things[name].Colour);
            m_Props[name] = img;
            return img;
        }

        public bool Showing => m_Root.gameObject.activeSelf;

        /// <summary>Plays <paramref name="player"/>'s scene; <paramref name="note"/> is shown over it (a draft's name and problems), or null.</summary>
        public void Begin(CutscenePlayer player, string note = null)
        {
            m_Player = player;
            m_StageOf = null;
            m_StageShot = -1;
            m_FlickerUntil = 0f;
            m_Tapped = m_SkipPressed = false;
            m_LastSaid = m_WaitingNote = null;
            m_Clock = 0f;
            m_LastNow = Time.unscaledTime;
            m_Note.text = note ?? "";
            Ui.Show(m_Note, note != null);
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

        /// <summary>Plays the sounds, and the stage's static, for cues that just came due (<see cref="Cues"/>).</summary>
        public void Cue(string id)
        {
            if (Cues.TryEffect(id, out CueEffect e))
            {
                Play(e.Sound, e.Volume);
                Play(e.Sound2, e.Volume2);
                if ((e.Staging & CueStaging.Flicker) != 0) m_FlickerUntil = m_Clock + FlickerFor;
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
            if (m_StageShot != p.Shot)
            {
                m_StageShot = p.Shot;
                m_StageOf = new ShotStage(p.Current);
            }
            ShotStage stage = m_StageOf;

            Stage(p.Current, stage, t, now);
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
            float goonGone = stage.Since("goon", "glitch-out", t);
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
                if (line.Who == Speaker.Goon && goonGone > 0.3f) continue; // gone with the goon
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
            StageFigure who = intro != null ? Figure(intro.Figure) : m_Tones2;
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

        /// <summary>A soldier on the stage, by name.</summary>
        StageFigure Figure(string name)
        {
            switch (name)
            {
                case "kie": return m_Kie;
                case "goon": return m_Goon;
                default: return m_Tones2;
            }
        }

        void ShowBubble(int i, CutsceneLine line, float now)
        {
            while (m_Bubbles.Count <= i) m_Bubbles.Add(new SpeechBubble(m_World, Vector2.zero));
            SpeechBubble b = m_Bubbles[i];
            bool glitchy = line.Who == Speaker.Goon || line.Who == Speaker.Hologram;
            float w = glitchy ? b.Say(line.Text, Cyan, new Color(0.05f, 0.12f, 0.16f, 0.95f)) : b.Say(line.Text);

            StageFigure who = Figure(SceneScript.FigureOf(line.Who));
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
            b.Place(at, who.Head.x);
        }

        const float BubbleH = SpeechBubble.Height, BubbleGap = 4f;

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

        // ------------------------------------------------------------------ the staging, as the script has it

        /// <summary>Puts every thing where the shot's script has it at <paramref name="t"/> (<paramref name="now"/> is the scene's clock).</summary>
        void Stage(CutsceneShot shot, ShotStage stage, float t, float now)
        {
            float ground = Staging.Sets.TryGetValue(shot.Set ?? "", out int g) ? g : 0f;
            m_Back.sprite = shot.Set == "room" ? s_Room : shot.Set == "motorpool" ? s_MotorPool : s_Street;
            stage.Camera(t, out m_Zoom, out m_Look);

            foreach (KeyValuePair<string, Image> kv in m_Props) Place(kv.Value, kv.Key, stage, t, now, ground);
            Sign(stage, t, now, ground);
            Clamp(stage, t, now);
            Drone(stage, t, now, ground);
            Beacon(stage, t);

            Soldier(m_Tones2, "tones2", Art.SkinFor(Content.Tones2, 0), 10f, stage, t, now, ground);
            Soldier(m_Kie, "kie", Art.SkinFor(Content.Kie, 0), 8f, stage, t, now, ground);
            Goon(stage, t, now, ground);
            KieTabk(stage, t, now, ground);

            for (int i = 0; i < m_Snow.Length; i++) Snow(i, now, stage.Shown("snow", t));
            Searchlights(stage, t, now);
            Chips(stage, t, ground);
            Jet(stage, t, ground);
            Closeup(stage, t, now);
            Shutter(stage, t, now, ground);
            Row(stage, t, now, ground);
            Steam(stage, t, now, ground);
            Cage(stage, t, ground);
            Stars(stage, t, now, ground);
            Zz(stage, t, now, ground);
            Alarm(stage, t);
        }

        /// <summary>A plain prop: on stage (and lit, if it blinks), where it is, turned, sized and coloured.</summary>
        static void Place(Image img, string name, ShotStage stage, float t, float now, float ground)
        {
            bool on = stage.Shown(name, t) && stage.Lit(name, t, now);
            Ui.Show(img, on);
            if (!on) return;
            Vector2 at = stage.Where(name, t, now);
            RectTransform rt = img.rectTransform;
            rt.anchoredPosition = new Vector2(at.x, ground + at.y) * Px;
            rt.localRotation = Quaternion.Euler(0f, 0f, stage.Turn(name, t));
            Vector2 size = stage.Size(name, t);
            rt.localScale = new Vector3(size.x, size.y, 1f);
            img.color = stage.Colour(name, t, now);
        }

        /// <summary>The hangout's sign: words, not a picture.</summary>
        void Sign(ShotStage stage, float t, float now, float ground)
        {
            bool on = stage.Shown("sign", t);
            Ui.Show(m_Sign, on);
            if (!on) return;
            Vector2 at = stage.Where("sign", t, now);
            m_Sign.rectTransform.anchoredPosition = new Vector2(at.x, ground + at.y) * Px;
            m_Sign.color = stage.Colour("sign", t, now);
        }

        /// <summary>The Trollograms' clamp on tones2's tabk: flickering, until kie rips it off and flings it away.</summary>
        void Clamp(ShotStage stage, float t, float now)
        {
            Image clamp = m_Props["clamp"];
            if (!clamp.gameObject.activeSelf) return;
            float rip = Mathf.Max(0f, stage.Progress("clamp", "rip", t));
            if (rip >= 1f)
            {
                Ui.Show(clamp, false);
                return;
            }
            Color c = stage.Colour("clamp", t, now);
            c.a *= (int)(now * 6f) % 4 == 0 ? 0.5f : 0.9f;
            clamp.color = c;
            RectTransform rt = clamp.rectTransform;
            rt.anchoredPosition += new Vector2(-rip * 40f, Mathf.Sin(rip * Mathf.PI) * 24f) * Px;
            rt.localRotation = Quaternion.Euler(0f, 0f, rip * 220f);
        }

        /// <summary>The cargo drone's rotors, and its cable down to the top of tones2's tabk.</summary>
        void Drone(ShotStage stage, float t, float now, float ground)
        {
            bool on = stage.Shown("drone", t);
            Ui.Show(m_Rotor, on);
            if (on)
            {
                Vector2 at = stage.Where("drone", t, now);
                m_Rotor.rectTransform.anchoredPosition = new Vector2(at.x + (int)(now * 20f) % 2 * 2f, ground + at.y + 10f) * Px;
            }
            Image cable = m_Props["cable"];
            if (!cable.gameObject.activeSelf) return;
            float top = stage.Where("drone", t, now).y, bottom = stage.Where("tones2-tabk", t, now).y + 20f;
            Vector2 x = stage.Where("cable", t, now);
            cable.rectTransform.anchoredPosition = new Vector2(x.x, ground + bottom) * Px;
            cable.rectTransform.sizeDelta = new Vector2(1f, Mathf.Max(0f, top - bottom)) * Px;
        }

        /// <summary>The alarm's beacon: dark, and turning red once a second while the alarm's going.</summary>
        void Beacon(ShotStage stage, float t)
        {
            Image beacon = m_Props["beacon"];
            if (!beacon.gameObject.activeSelf || !stage.Shown("alarm", t)) return;
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f);
            beacon.color = Color.Lerp(Staging.Things["beacon"].Colour, Staging.Colours["red"], pulse);
        }

        /// <summary>
        /// A soldier, drawn from their legs and torso as in the game: walking, standing, crouched, spraying, or just their
        /// top half in a tabk's hatch; plain, or as Trollogram kie (cyan, flickering, grinning).
        /// </summary>
        void Soldier(StageFigure f, string name, PlayerSkin skin, float runRate, ShotStage stage, float t, float now, float ground)
        {
            bool on = stage.Shown(name, t);
            Ui.Show(f.Rt, on);
            if (!on) return;
            Vector2 at = stage.Where(name, t, now);
            string pose = stage.Pose(name, t);
            Ui.Show(f.Legs, pose != "hatch");
            f.Legs.sprite = pose == "run" ? skin.LegsRun[(int)(t * runRate) % skin.LegsRun.Length] : pose == "crouch" ? skin.LegsCrouch : skin.LegsIdle;
            f.Torso.sprite = skin.TorsoForward;
            f.Torso.rectTransform.anchoredPosition = pose == "spray" ? new Vector2(-1f, 0f) : Vector2.zero;
            Color tint = stage.Colour(name, t, now);
            if (stage.Look(name, t) == "trollo")
            {
                bool flick = (int)(now * 30f) % 9 == 0;
                tint = new Color(0.45f, 1f, 1f, flick ? 0.35f : 0.85f);
                if ((int)(now * 20f) % 7 == 0) at.x += 1f;
            }
            f.Legs.color = f.Torso.color = tint;
            f.Rt.localScale = new Vector3(stage.Facing(name, t), 1f, 1f);
            f.At(at.x, ground + at.y);
        }

        /// <summary>
        /// The opening's straggler: a Trollogram that flickers up (its <c>appear</c>), and dies the goons' own death (its
        /// <c>glitch-out</c>): a flash, tearing, then squashed to a line and gone.
        /// </summary>
        void Goon(ShotStage stage, float t, float now, float ground)
        {
            float died = stage.Since("goon", "glitch-out", t);
            bool on = stage.Shown("goon", t) && died <= 0.6f;
            Ui.Show(m_Goon.Rt, on);
            if (!on) return;
            m_Goon.Legs.sprite = Art.Holo(Art.SoldierLegsIdle);
            m_Goon.Torso.sprite = Art.Holo(Art.SoldierTorso);
            Ui.Show(m_Goon.Legs, true);
            Vector2 at = stage.Where("goon", t, now);
            float rise = stage.Progress("goon", "appear", t);
            if (rise < 0f) rise = 1f;
            float jitter = (int)(now * 20f) % 7 == 0 ? 1f : 0f;
            Color tint = new Color(0.45f, 1f, 1f, rise < 1f ? ((int)(now * 30f) % 2 == 0 ? rise : 0.2f) : 0.9f);
            float facing = stage.Facing("goon", t);
            Vector3 scale = new Vector3(facing, 1f, 1f);
            if (died >= 0f)
            {
                tint = died < 0.08f ? Color.white : Color.Lerp(Cyan, new Color(1f, 0.4f, 0.9f), (int)(now * 30f) % 2);
                jitter = Mathf.Sin(now * 90f) * died * 14f;
                scale = new Vector3(facing * (1f + died * 0.6f), Mathf.Max(0.04f, 1f - died * 1.6f), 1f);
                tint.a = 1f - Mathf.Clamp01((died - 0.4f) / 0.2f);
            }
            m_Goon.Legs.color = m_Goon.Torso.color = tint;
            m_Goon.Rt.localScale = scale;
            m_Goon.At(at.x + jitter, ground + at.y);
        }

        /// <summary>kie's tabk: its hull in its colour (white, or a wreck's), its gun turned as the script has the turret.</summary>
        void KieTabk(ShotStage stage, float t, float now, float ground)
        {
            bool on = stage.Shown("kie-tabk", t);
            Ui.Show(m_KieTabk, on);
            if (!on) return;
            Vector2 at = stage.Where("kie-tabk", t, now);
            m_KieTabk.anchoredPosition = new Vector2(at.x, ground + at.y) * Px;
            m_KieHull.color = stage.Colour("kie-tabk", t, now);
            m_KieTurret.localRotation = Quaternion.Euler(0f, 0f, stage.Turn("turret", t));
        }

        // ------------------------------------------------------------------ the named effects

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

        /// <summary>The base's searchlights sweeping past the window.</summary>
        void Searchlights(ShotStage stage, float t, float now)
        {
            bool on = stage.Shown("searchlights", t);
            for (int i = 0; i < m_Beams.Length; i++)
            {
                Ui.Show(m_Beams[i], on);
                if (on) m_Beams[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(now * 1.3f + i * 2f) * 20f);
            }
        }

        /// <summary>The chips: bursting out of the dropped bag (<c>burst</c>), or already all over the floor (<c>spill</c>).</summary>
        void Chips(ShotStage stage, float t, float ground)
        {
            bool on = stage.Shown("chips", t);
            float burst = stage.Since("chips", "burst", t);
            float[] where = stage.Numbers("chips", "burst", t) ?? stage.Numbers("chips", "spill", t);
            if (stage.Latest("chips", t, "burst", "spill")?.Verb == "spill") burst = 9f;
            on &= where != null && burst >= 0f;
            for (int i = 0; i < m_Chips.Length; i++)
            {
                Ui.Show(m_Chips[i], on);
                if (!on) continue;
                float dir = (i - m_Chips.Length / 2f) * 2.4f;
                float k = Mathf.Clamp01(burst / 0.35f);
                float hop = Mathf.Sin(k * Mathf.PI) * (4f + i % 3 * 3f);
                m_Chips[i].rectTransform.anchoredPosition = new Vector2(where[0] + dir * k * 1.6f, ground + hop) * Px;
            }
        }

        /// <summary>tones2's water jet, arcing from one place to another while it sprays.</summary>
        void Jet(ShotStage stage, float t, float ground)
        {
            StageStep s = stage.Latest("jet", t, "spray");
            bool on = s != null && t < s.To && stage.Shown("jet", t);
            for (int i = 0; i < m_Jet.Length; i++)
            {
                Ui.Show(m_Jet[i], on);
                if (!on) continue;
                float k = ((t - s.From) * 3f + i / (float)m_Jet.Length) % 1f;
                float x = Mathf.Lerp(s.N[0], s.N[1], k);
                float y = ground + 17f + Mathf.Sin(k * Mathf.PI) * 6f;
                m_Jet[i].rectTransform.anchoredPosition = new Vector2(x, y) * Px;
            }
        }

        /// <summary>The keycard, close up: the room dimmed, and the card sliding up into view, flickering like the Trollograms.</summary>
        void Closeup(ShotStage stage, float t, float now)
        {
            bool on = stage.Shown("closeup", t);
            Ui.Show(m_Dim, on);
            Ui.Show(m_BigCard, on);
            if (!on) return;
            float up = Mathf.Clamp01((t - stage.ShownSince("closeup", t)) / 0.35f);
            m_BigCard.anchoredPosition = new Vector2((int)(now * 10f) % 9 == 0 ? 2f : 0f, Mathf.Lerp(-140f, 26f, 1f - (1f - up) * (1f - up)));
            m_BigCard.localRotation = Quaternion.Euler(0f, 0f, (1f - up) * -8f);
        }

        /// <summary>
        /// The motor pool's blast shutter, down until it grinds up (<c>open</c>, juddering as it goes) or is already up;
        /// the cold light it lets out, and the work lights over the row, steady or stuttering.
        /// </summary>
        void Shutter(ShotStage stage, float t, float now, float ground)
        {
            bool on = stage.Shown("shutter", t);
            Ui.Show(m_ShutterMask, on);
            Ui.Show(m_ColdLight, on);
            StageStep s = stage.Latest("shutter", t, "open", "up");
            float up = s == null ? 0f : s.Verb == "up" ? 1f : s.Progress(t);
            if (on)
            {
                float grind = up > 0f && up < 1f ? (int)(now * 16f) % 2 : 0f;
                m_ShutterMask.anchoredPosition = new Vector2(MotorOpenX, ground) * Px;
                m_Shutter.rectTransform.anchoredPosition = new Vector2(0f, up * MotorOpenH + grind) * Px;
                m_ColdLight.rectTransform.anchoredPosition = new Vector2(MotorOpenX + MotorOpenW / 2f, 0f) * Px;
                m_ColdLight.rectTransform.sizeDelta = new Vector2(MotorOpenW + 30f, ground + 4f) * Px;
                m_ColdLight.color = new Color(0.7f, 0.95f, 1f, 0.22f * up);
            }
            bool lamps = stage.Shown("lamps", t);
            bool stutter = stage.Doing("lamps", "stutter", t);
            if (!on) up = 1f; // lamps without a shutter are just on
            for (int i = 0; i < m_Lamps.Length; i++)
            {
                Ui.Show(m_Lamps[i], lamps);
                if (!lamps) continue;
                bool flick = stutter && (int)(now * 11f + i * 5) % 17 == 0; // a work light stutters now and then
                m_Lamps[i].rectTransform.anchoredPosition = new Vector2(MotorRow[i] + 6f, ground + 6f) * Px;
                m_Lamps[i].color = new Color(1f, 0.95f, 0.75f, up * (flick ? 0.05f : 0.14f));
            }
        }

        /// <summary>The rest of the crew's tabks parked in a row under the work lights, and the Trollograms' junk all over the row.</summary>
        void Row(ShotStage stage, float t, float now, float ground)
        {
            bool on = stage.Shown("row", t);
            for (int i = 0; i < m_Row.Length; i++)
            {
                Ui.Show(m_Row[i], on);
                if (on) m_Row[i].rectTransform.anchoredPosition = new Vector2(MotorRow[i + 1], ground + 2f) * Px;
            }
            bool junk = stage.Shown("junk", t);
            for (int i = 0; i < m_Junk.Length; i++)
            {
                Ui.Show(m_Junk[i], junk);
                if (!junk) continue;
                float x = MotorRow[i % MotorRow.Length] - 14f + (i * 11 % 26);
                float y = ground + 8f + (i * 7 % 13);
                bool lit = (int)(now * 8f + i * 3) % 6 != 0;
                m_Junk[i].color = new Color(0.45f, 1f, 1f, lit ? 0.9f : 0.25f);
                m_Junk[i].rectTransform.anchoredPosition = new Vector2(x, y) * Px;
            }
        }

        /// <summary>Steam (or smoke, coloured so) rising off a parking space.</summary>
        void Steam(ShotStage stage, float t, float now, float ground)
        {
            bool on = stage.Shown("steam", t);
            Vector2 at = stage.Where("steam", t, now);
            Color c = stage.Colour("steam", t, now);
            for (int i = 0; i < m_Steam.Length; i++)
            {
                Ui.Show(m_Steam[i], on);
                if (!on) continue;
                float k = (now * 0.5f + i / (float)m_Steam.Length) % 1f;
                m_Steam[i].color = new Color(c.r, c.g, c.b, 0.45f * (1f - k));
                m_Steam[i].rectTransform.anchoredPosition = new Vector2(at.x - 12f + i * 8f + Mathf.Sin(now * 2f + i) * 2f, ground + at.y + k * 22f) * Px;
            }
        }

        /// <summary>The cage of light round the real kie: bars and two rails, lit until it flickers out (<c>flicker-out</c>).</summary>
        void Cage(ShotStage stage, float t, float ground)
        {
            const float W = 18f, H = 30f;
            StageStep s = stage.Latest("cage", t, "flicker-out");
            bool lit = stage.Shown("cage", t) && (s == null || PilotFreeingSpec.CageLitFor(t - s.From, s.To - s.From));
            float x = stage.Where("cage", t, 0f).x;
            for (int i = 0; i < m_Cage.Length; i++)
            {
                Ui.Show(m_Cage[i], lit);
                if (!lit) continue;
                bool rail = i >= 5;
                RectTransform rt = m_Cage[i].rectTransform;
                rt.sizeDelta = (rail ? new Vector2(W + 2f, 1f) : new Vector2(1f, H)) * Px;
                rt.anchoredPosition = new Vector2(rail ? x : x - W / 2f + i * (W / 4f), ground + (rail && i == 6 ? H : 0f)) * Px;
            }
        }

        /// <summary>Dazed stars, circling over whoever they're on.</summary>
        void Stars(ShotStage stage, float t, float now, float ground)
        {
            bool on = stage.Shown("stars", t);
            Vector2 at = stage.Where("stars", t, now);
            for (int i = 0; i < m_Stars.Length; i++)
            {
                Ui.Show(m_Stars[i], on);
                if (!on) continue;
                float a = now * 4f + i * Mathf.PI * 2f / m_Stars.Length;
                m_Stars[i].rectTransform.anchoredPosition = new Vector2(at.x + Mathf.Cos(a) * 7f, ground + at.y + Mathf.Sin(a) * 2f) * Px;
            }
        }

        /// <summary>z Z, drifting up off a sleeper, over and over.</summary>
        void Zz(ShotStage stage, float t, float now, float ground)
        {
            bool on = stage.Shown("zz", t);
            Ui.Show(m_Zz, on);
            if (!on) return;
            Vector2 at = stage.Where("zz", t, now);
            float k = Mathf.Repeat(t - stage.ShownSince("zz", t), 1.6f) / 1.6f;
            m_Zz.rectTransform.anchoredPosition = new Vector2(at.x + k * 8f, ground + at.y + k * 10f) * Px;
            m_Zz.color = new Color(Paper.r, Paper.g, Paper.b, 1f - k);
        }

        /// <summary>The alarm's slow red wash over everything: once a second, never a strobe.</summary>
        void Alarm(ShotStage stage, float t)
        {
            bool on = stage.Shown("alarm", t);
            Ui.Show(m_Alarm, on);
            if (!on) return;
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f);
            m_Alarm.color = new Color(1f, 0.1f, 0.05f, 0.1f * pulse);
        }

        // ------------------------------------------------------------------ the painted backdrops

        static void DrawStreet(PixelCanvas c)
        {
            int street = Staging.Sets["street"];
            Sky(c, 0x0b0f24, 0x1d2448);
            var rng = new System.Random(7);
            for (int i = 0; i < 60; i++) c.Set(rng.Next(StageW), 70 + rng.Next(StageH - 70), Art.Hex(0xc8d0ff, (byte)(120 + rng.Next(120))));
            // the town behind
            for (int x = 0, i = 0; x < 150; x += 22 + i % 3 * 6, i++) c.Rect(x, street, 18 + i % 2 * 8, 30 + (i * 13) % 34, Art.Hex(0x161a33));
            for (int i = 0; i < 18; i++) c.Rect(4 + i * 8 % 140, street + 8 + i * 11 % 40, 2, 3, Art.Hex(0x6a5a2a));
            // the hangout: a garage with a roll door and a side door
            c.Rect(150, street, 156, 96, Art.Hex(0x3a2a2e));
            for (int y = street + 4; y < street + 96; y += 6)
                for (int x = 150 + (y / 6 % 2) * 6; x < 306; x += 12) c.Rect(x, y, 1, 5, Art.Hex(0x2e2024));
            c.Rect(146, street + 96, 164, 6, Art.Hex(0x241820));
            c.Rect(160, street, 50, 54, Art.Hex(0x5b5f66));
            for (int y = street + 3; y < street + 54; y += 5) c.Rect(160, y, 50, 1, Art.Hex(0x45484e));
            c.Rect(221, street, 22, 42, Art.Hex(0x1c1214)); // the side door's frame
            c.Rect(246, street + 36, 32, 24, Art.Hex(0x1c1214)); // the window's frame
            // the street
            c.Rect(0, 0, StageW, street, Art.Hex(0x2a2c38));
            c.Rect(0, street - 3, StageW, 3, Art.Hex(0x4a4c5a));
            for (int x = 6; x < StageW; x += 28) c.Rect(x, 9, 12, 2, Art.Hex(0x55576a));
            // snow on the ground and roof
            c.Rect(0, street - 1, StageW, 1, Art.Hex(0xdfe6ff));
            c.Rect(146, street + 102, 164, 2, Art.Hex(0xdfe6ff));
            // a streetlight
            c.Rect(110, street, 2, 70, Art.Hex(0x22242e));
            c.Rect(106, street + 68, 10, 3, Art.Hex(0x22242e));
            c.Circle(111, street + 66, 2.5f, Art.Hex(0xffe6a0));
        }

        /// <summary>The motor pool, from outside: the base's wall at night, and the big opening the shutter covers, with the bay behind it.</summary>
        static void DrawMotorPool(PixelCanvas c)
        {
            int floor = Staging.Sets["motorpool"], ox = (int)MotorOpenX, ow = (int)MotorOpenW, oh = (int)MotorOpenH;
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
            int floor = Staging.Sets["room"];
            // walls, with cyan scorch marks
            c.Rect(0, 0, StageW, StageH, Art.Hex(0x2b2236));
            for (int x = 0; x < StageW; x += 40) c.Rect(x, floor, 1, StageH - floor, Art.Hex(0x241c2e));
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
            c.Rect(0, 0, StageW, floor, Art.Hex(0x4a3a30));
            for (int x = 0; x < StageW; x += 16) c.Rect(x, 0, 1, floor, Art.Hex(0x3c2e26));
            c.Rect(0, floor - 1, StageW, 1, Art.Hex(0x6a5444));
            // the gaming setup, wrecked: a cracked screen on a low table
            c.Rect(108, floor, 40, 14, Art.Hex(0x5a3e2e));
            c.Rect(112, floor + 14, 32, 22, Art.Hex(0x101014));
            c.Rect(114, floor + 16, 28, 18, Art.Hex(0x1e3a48));
            for (int i = 0; i < 12; i++) c.Set(118 + i * 2, floor + 30 - i, Art.Hex(0xd0f0ff));
            // controllers on the floor
            c.Rect(92, floor, 7, 3, Art.Hex(0x22222a));
            c.Rect(156, floor, 7, 3, Art.Hex(0xe8e8ee));
            // the couch, flipped on its back
            c.Rect(166, floor, 52, 10, Art.Hex(0x3c6a5a));
            c.Rect(170, floor + 10, 44, 12, Art.Hex(0x335a4c));
            c.Rect(166, floor + 10, 6, 6, Art.Hex(0x2a4a3e));
            c.Rect(212, floor + 10, 6, 6, Art.Hex(0x2a4a3e));
            c.Rect(174, floor + 22, 4, 3, Art.Hex(0x1c1214));
            c.Rect(206, floor + 22, 4, 3, Art.Hex(0x1c1214));
            // the doorway they came in by, on the left
            c.Rect(14, floor, 22, 44, Art.Hex(0x14101a));
        }
    }
}
