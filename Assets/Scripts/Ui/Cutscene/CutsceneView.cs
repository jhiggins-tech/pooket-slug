using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static PooketSlug.StageKit;

namespace PooketSlug
{
    /// <summary>
    /// The cutscenes (<c>docs/design/cutscenes.md</c>, How every cutscene plays): draws a <see cref="CutscenePlayer"/>'s scene in uGUI, over
    /// the game. It's the game's own sprites on painted backdrops, with speech bubbles, a caption card, a SKIP button once
    /// the scene's been seen, and in co-op a line saying who's still to tap.
    /// <para>
    /// What's on stage, where and when is the scene's own script (<c>scenes/&lt;name&gt;.txt</c>, <see cref="SceneScript"/>),
    /// worked out moment by moment by <see cref="ShotStage"/>; the parts here draw each thing in the vocabulary
    /// (<see cref="Staging.Things"/>) the way the script has it: <see cref="CutsceneBackdrops"/> the sets,
    /// <see cref="CutsceneCast"/> the soldiers and props, <see cref="CutsceneEffects"/> the named effects (snow, spilt
    /// chips, the shutter, the cage of light...), <see cref="CutsceneBubbles"/> and <see cref="CutsceneCaptions"/> the
    /// words, <see cref="CutsceneCamera"/> the camera and the character intros, <see cref="CueSounds"/> the cues' sounds.
    /// This view is the screen around them: taps, SKIP, the cut between shots, the Trollograms' static, and co-op's
    /// waiting line.
    /// </para>
    /// <para>
    /// The stage is <see cref="StageKit"/>'s, shared with the den and the debrief: 320 x 180 art pixels drawn at 2x, so
    /// the soldiers are the size they are in the game, and letterboxed on screens of other shapes.
    /// </para>
    /// </summary>
    public sealed class CutsceneView
    {
        const float FlickerFor = 0.3f; // the Trollograms' static pulse over the whole stage: one, fading

        readonly RectTransform m_Root, m_Stage, m_World;
        readonly Image m_Fade, m_Flicker, m_Back;
        readonly CutsceneCast m_Cast;
        readonly CutsceneEffects m_Effects;
        readonly CutsceneBubbles m_Bubbles;
        readonly CutsceneCaptions m_Captions;
        readonly CutsceneCamera m_Camera;
        readonly Text m_Hint, m_Waiting, m_Note;
        readonly Button m_Skip;
        float m_Clock, m_LastNow;
        string m_WaitingNote;
        readonly List<CutsceneLine> m_Lines = new List<CutsceneLine>();

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

            m_Cast = new CutsceneCast(m_World);
            m_Effects = new CutsceneEffects(m_World, m_Stage);
            m_Bubbles = new CutsceneBubbles(m_World, m_Cast);
            m_Captions = new CutsceneCaptions(m_Stage);
            m_Camera = new CutsceneCamera(m_World, m_Stage);

            // everything in the scene sits in the world, which the camera's crash zoom moves; what's over it (subtitles,
            // the caption card, buttons) sits on the stage. Things are made in drawing order, back to front, so the
            // parts' pieces are made here in turn.
            m_Back = BackdropImage(m_World);
            m_Cast.MakeKcajCorner();
            m_Cast.MakeColdStore();
            m_Cast.MakeHangout();
            m_Effects.MakeSearchlights();
            // the motor pool: lamps over the row, the row itself, the cold light the shutter lets out, and the shutter
            m_Effects.MakeWorkLights();
            m_Cast.MakeTones2Tabk();
            m_Effects.MakeRow();
            m_Cast.MakeKieAndTheDrone();
            m_Effects.MakeCageAndStars();
            m_Cast.MakeRemoteAndBeacon();
            m_Effects.MakeSleepAndShutter();
            m_Cast.Prop("spark", 6f, 6f);
            m_Effects.MakeChips();
            m_Cast.MakeFrontCast();
            m_Cast.MakeKcajTabk();
            m_Effects.MakeFrost();
            m_Effects.MakeWaterAndSnow();

            m_Camera.MakeIntro();
            m_Effects.MakeCloseup();
            m_Effects.MakeBoard();
            m_Captions.MakeCard();
            m_Effects.MakeAlarm();
            m_Flicker = Ui.Cover(m_Stage, "Flicker", new Color(0.4f, 1f, 1f, 0f));
            m_Fade = Ui.Cover(m_Stage, "Cut", Color.black);
            m_Captions.MakeSubtitles();

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

        public bool Showing => m_Root.gameObject.activeSelf;

        /// <summary>Plays <paramref name="player"/>'s scene; <paramref name="note"/> is shown over it (a draft's name and problems), or null.</summary>
        public void Begin(CutscenePlayer player, string note = null)
        {
            m_Player = player;
            m_StageOf = null;
            m_StageShot = -1;
            m_FlickerUntil = 0f;
            m_Tapped = m_SkipPressed = false;
            m_WaitingNote = null;
            m_Captions.Begin();
            m_Clock = 0f;
            m_LastNow = Time.unscaledTime;
            m_Note.text = note ?? "";
            Ui.Show(m_Note, note != null);
            m_Effects.Paint();
            m_Camera.Begin();
            Ui.Show(m_Root, true);
        }

        public void Hide()
        {
            Sfx.HushNarrator(); // the announcer's call as a scene ends into its mission plays out over the start
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
                CueSounds.Play(e.Sound, e.Volume);
                CueSounds.Play(e.Sound2, e.Volume2);
                if ((e.Staging & CueStaging.Flicker) != 0) m_FlickerUntil = m_Clock + FlickerFor;
            }
            else if (Cues.SaidLine(id) is string line) Sfx.Say(line);
            else Log.Game.Warn("Cutscene cue with nothing to do: " + id);
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

            // everything where the shot's script has it, then the camera on it
            CutsceneShot shot = p.Current;
            float ground = Staging.Sets.TryGetValue(shot.Set ?? "", out int g) ? g : 0f;
            m_Back.sprite = CutsceneBackdrops.For(shot.Set);
            m_Back.rectTransform.sizeDelta = m_Back.sprite.rect.size * Px; // the bay's wider than the stage: the camera pans along it
            m_Camera.Aim(stage, t);
            m_Cast.Draw(stage, t, now, ground);
            m_Effects.Draw(stage, t, now, ground);
            CutsceneIntro intro = shot.Intro;
            m_Camera.Draw(p, m_Cast.Figure(intro != null ? intro.Figure : "tones2"), m_Clock);

            // a quick cut between shots, and the Trollograms' static: one soft cyan pulse that fades, never a strobe
            // (it's timed on the scene's own clock; it was once timed against the real one, and strobed for the rest
            // of the scene)
            m_Fade.color = new Color(0f, 0f, 0f, Mathf.Clamp01(1f - t / 0.18f));
            float flicker = now < m_FlickerUntil ? 0.3f * (m_FlickerUntil - now) / FlickerFor : 0f;
            m_Flicker.color = new Color(0.4f, 1f, 1f, flicker);

            p.Showing(m_Lines);
            bool card = false;
            m_Bubbles.Begin();
            float goonGone = stage.Since("goon", "glitch-out", t);
            foreach (CutsceneLine line in m_Lines)
            {
                if (line.Who == Speaker.Caption)
                {
                    card = true;
                    m_Captions.Card(line, t);
                    continue;
                }
                if (line.Who == Speaker.Goon && goonGone > 0.3f) continue; // gone with the goon
                m_Bubbles.Show(line, now);
            }
            m_Bubbles.End();
            m_Captions.Show(card, p);

            Ui.Show(m_Skip, p.CanSkip);
            Ui.Show(m_Hint, !p.CanSkip && !p.Waiting && p.Shot == 0 && t > 1.2f);
            Ui.Show(m_Waiting, p.Waiting || m_WaitingNote != null);
            if (m_WaitingNote != null) m_Waiting.text = m_WaitingNote;
            else if (p.Waiting) m_Waiting.text = p.HasTapped(0) ? "WAITING FOR YOUR PARTNER" : "YOUR PARTNER'S READY: TAP TO GO ON";
        }

        /// <summary>In co-op: a line saying what the scene's waiting on, over the usual one, or null for that.</summary>
        public void Waiting(string note) => m_WaitingNote = note;
    }
}
