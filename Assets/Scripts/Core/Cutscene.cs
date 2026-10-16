using System.Collections.Generic;

namespace PooketSlug
{
    /// <summary>Who says a line in a cutscene, which sets how its bubble looks.</summary>
    public enum Speaker { Tones2, Kie, Goon, Hologram, Caption, Narrator }

    /// <summary>
    /// One speech bubble, the caption card, or the narrator's subtitle, shown from <see cref="At"/> seconds into its
    /// shot until the shot ends (the narrator's until their next line).
    /// </summary>
    public readonly struct CutsceneLine
    {
        public readonly float At;
        public readonly Speaker Who;
        public readonly string Text;
        /// <summary>The recorded line's id, for a voice to read it (the narrator's), or null.</summary>
        public readonly string Voice;

        public CutsceneLine(float at, Speaker who, string text, string voice = null)
        {
            At = at;
            Who = who;
            Text = text;
            Voice = voice;
        }
    }

    /// <summary>A sound or a bit of staging due <see cref="At"/> seconds into a shot. The ids are the view's to read.</summary>
    public readonly struct CutsceneCue
    {
        public readonly float At;
        public readonly string Id;

        public CutsceneCue(float at, string id)
        {
            At = at;
            Id = id;
        }
    }

    /// <summary>
    /// A character's intro, the first time they're on screen in the story (<c>docs/design/story.md</c>, Character
    /// intros): it builds up for <see cref="CutscenePlayer.BuildUp"/> seconds first (a riser, and the camera creeping in),
    /// then at <see cref="At"/> seconds into the shot it lands: the camera crash-zooms onto <see cref="Who"/>, their name
    /// slams up big, time goes slow-mo, and a musical hit plays.
    /// </summary>
    public sealed class CutsceneIntro
    {
        public readonly float At;
        public readonly Speaker Who;
        public readonly string Name;

        public CutsceneIntro(float at, Speaker who, string name)
        {
            At = at;
            Who = who;
            Name = name;
        }
    }

    /// <summary>One shot of a scene: how long it holds by itself, its bubbles and its cues, and any character's intro.</summary>
    public sealed class CutsceneShot
    {
        public readonly string Name;
        public readonly float Seconds;
        public readonly CutsceneLine[] Lines;
        public readonly CutsceneCue[] Cues;
        public readonly CutsceneIntro Intro;

        public CutsceneShot(string name, float seconds, CutsceneLine[] lines, CutsceneCue[] cues = null, CutsceneIntro intro = null)
        {
            Name = name;
            Seconds = seconds;
            Lines = lines ?? new CutsceneLine[0];
            Cues = cues ?? new CutsceneCue[0];
            Intro = intro;
        }
    }

    /// <summary>A scene: its shots, and the id it's remembered by once seen (<see cref="Profile.seen"/>).</summary>
    public sealed class CutsceneDef
    {
        public readonly string Id;
        public readonly CutsceneShot[] Shots;

        public CutsceneDef(string id, params CutsceneShot[] shots)
        {
            Id = id;
            Shots = shots;
        }

        public float Seconds
        {
            get
            {
                float s = 0f;
                foreach (CutsceneShot shot in Shots) s += shot.Seconds;
                return s;
            }
        }
    }

    /// <summary>The scenes as written in <c>docs/design/story.md</c>. The lines are placeholders for the owner's own.</summary>
    public static class Cutscenes
    {
        /// <summary>
        /// The opening: the snack run and the raided hangout, which doubles as Mission one's briefing. The narrator
        /// tells it in subtitles (a voice comes later), and the keycard gets a close-up so it's clear where it sends you.
        /// </summary>
        public static readonly CutsceneDef Opening = new CutsceneDef("scene-opening",
            new CutsceneShot("Outside the hangout", 8f,
                new[]
                {
                    new CutsceneLine(4.3f, Speaker.Narrator, "Friday night. tones2's back from the snack run.", "opening-1"), // after his intro
                    new CutsceneLine(6.4f, Speaker.Tones2, "SNACKS ARE HERE LADS"),
                },
                // he walks on for a few seconds, the build-up starts at 1.5 s, and it lands at 4 s
                intro: new CutsceneIntro(4.0f, Speaker.Tones2, "TONES2")),
            new CutsceneShot("The door", 4.5f,
                new[]
                {
                    new CutsceneLine(0.9f, Speaker.Narrator, "But the door's been kicked in.", "opening-2"),
                    new CutsceneLine(2.4f, Speaker.Tones2, "...LADS?"),
                },
                new[] { new CutsceneCue(0.5f, "door"), new CutsceneCue(1.2f, "static") }),
            new CutsceneShot("Inside, raided", 5f,
                new[] { new CutsceneLine(0.5f, Speaker.Narrator, "The hangout's trashed, and the crew are gone.", "opening-3") },
                new[] { new CutsceneCue(2.2f, "drop"), new CutsceneCue(3.4f, "static") }),
            new CutsceneShot("A straggler", 8f,
                new[]
                {
                    new CutsceneLine(0.8f, Speaker.Goon, "INTRUDER. INTRUD-"),
                    new CutsceneLine(1.2f, Speaker.Narrator, "One of the raiders is still here: a hologram.", "opening-4a"),
                    new CutsceneLine(5.0f, Speaker.Narrator, "It glitches out, and drops its keycard.", "opening-4b"),
                },
                new[] { new CutsceneCue(0.3f, "goon"), new CutsceneCue(3.2f, "jet"), new CutsceneCue(3.8f, "glitch"), new CutsceneCue(4.2f, "card-drop") }),
            new CutsceneShot("The keycard", 5f,
                new[]
                {
                    new CutsceneLine(0.5f, Speaker.Narrator, "A pass for the military base, up on the hill.", "opening-5"),
                    new CutsceneLine(2.6f, Speaker.Tones2, "THAT'S WHERE THEY TOOK THEM."),
                },
                new[] { new CutsceneCue(0.1f, "card-up") }),
            new CutsceneShot("Heading out", 6f,
                new[]
                {
                    new CutsceneLine(0.4f, Speaker.Narrator, "So tones2 goes in alone.", "opening-6"),
                    new CutsceneLine(1.2f, Speaker.Tones2, "RIGHT."),
                    new CutsceneLine(3.9f, Speaker.Caption, "MISSION 1 · THE MILITARY BASE\nFIND THE CREW"),
                },
                new[] { new CutsceneCue(1.6f, "crush"), new CutsceneCue(3.9f, "card"), new CutsceneCue(4.1f, "say:mission-start") }));
    }

    /// <summary>
    /// Plays a scene (<c>docs/design/story.md</c>, Cutscenes): each shot moves on by itself, or with a tap. A scene
    /// that's been seen before can be skipped. In co-op every player has to tap before a shot moves on early, so a
    /// partner who hasn't seen it gets to watch it; the timer still moves it on for both.
    /// <para>
    /// A shot with a character's <see cref="CutsceneIntro"/> holds it for <see cref="IntroSeconds"/>, during which the
    /// shot's own clock runs at <see cref="SlowMo"/>, and a tap can't cut it short.
    /// </para>
    /// </summary>
    public sealed class CutscenePlayer
    {
        /// <summary>A tap this soon into a shot is ignored, so mashing doesn't throw away two shots at once.</summary>
        public const float TapGuard = 0.3f;
        /// <summary>How long a character's intro holds, in real seconds, and how slow the shot runs meanwhile.</summary>
        public const float IntroSeconds = 1.8f, SlowMo = 0.15f;
        /// <summary>How long an intro builds up before it lands, in shot seconds (the shot runs at full speed till then).</summary>
        public const float BuildUp = 2.5f;
        /// <summary>The cues that come due as an intro's build-up starts (the riser) and as it lands (the musical hit).</summary>
        public const string BuildUpCue = "intro-build", IntroCue = "intro";

        public readonly CutsceneDef Scene;
        public readonly bool CanSkip;
        readonly bool[] m_Tapped;

        public int Shot { get; private set; }
        /// <summary>Seconds into the current shot.</summary>
        public float T { get; private set; }
        public bool Done { get; private set; }
        public bool Skipped { get; private set; }
        /// <summary>Real seconds into the intro playing now, if <see cref="Introducing"/>.</summary>
        public float IntroT { get; private set; }

        enum IntroState { Due, Playing, Over }
        IntroState m_Intro;

        public CutscenePlayer(CutsceneDef scene, bool seenBefore, int players = 1)
        {
            Scene = scene;
            CanSkip = seenBefore;
            m_Tapped = new bool[System.Math.Max(1, players)];
        }

        public int Players => m_Tapped.Length;
        public CutsceneShot Current => Done ? null : Scene.Shots[Shot];

        /// <summary>How far into the build-up to the coming intro (0 to 1), or 0 if there's none building.</summary>
        public float BuildingUp
        {
            get
            {
                CutsceneIntro intro = Current?.Intro;
                if (intro == null || m_Intro != IntroState.Due) return 0f;
                return UnityEngine.Mathf.Clamp01((T - (intro.At - BuildUp)) / BuildUp);
            }
        }

        /// <summary>A character's intro is playing now: the crash zoom, their name, and slow-mo.</summary>
        public bool Introducing => !Done && m_Intro == IntroState.Playing;

        public bool HasTapped(int player) => player >= 0 && player < m_Tapped.Length && m_Tapped[player];

        /// <summary>Someone has tapped this shot and someone else hasn't yet: show who's being waited for.</summary>
        public bool Waiting
        {
            get
            {
                if (Done || m_Tapped.Length < 2) return false;
                bool any = false, all = true;
                foreach (bool t in m_Tapped)
                {
                    any |= t;
                    all &= t;
                }
                return any && !all;
            }
        }

        /// <summary>
        /// <paramref name="player"/> tapped to move on. True if that moved the scene on: to the next shot, or to the
        /// build-up of the character intro still to come in this one.
        /// </summary>
        public bool Tap(int player)
        {
            if (Done || player < 0 || player >= m_Tapped.Length || T < TapGuard || Introducing) return false;
            m_Tapped[player] = true;
            foreach (bool t in m_Tapped)
                if (!t) return false;
            CutsceneIntro intro = Current.Intro;
            if (intro != null && m_Intro == IntroState.Due)
            {
                // a character's intro is never tapped past: the tap jumps to its build-up instead, and does nothing in it
                for (int i = 0; i < m_Tapped.Length; i++) m_Tapped[i] = false;
                if (T >= intro.At - BuildUp) return false;
                T = intro.At - BuildUp;
                return true;
            }
            Next();
            return true;
        }

        /// <summary>Ends the scene at once, if it's been seen before.</summary>
        public bool Skip()
        {
            if (!CanSkip || Done) return false;
            Done = Skipped = true;
            return true;
        }

        /// <summary>
        /// Moves time on by <paramref name="dt"/>, and fills <paramref name="due"/> with the ids of the cues that came due
        /// in the current shot. Returns how many. A shot's leftover cues are dropped when a tap moves past them.
        /// </summary>
        public int Tick(float dt, List<string> due = null)
        {
            due?.Clear();
            if (Done) return 0;
            float from = T, rate = 1f;
            if (m_Intro == IntroState.Playing)
            {
                IntroT += dt;
                if (IntroT >= IntroSeconds) m_Intro = IntroState.Over;
                else rate = SlowMo;
            }
            T += dt * rate;
            foreach (CutsceneCue cue in Current.Cues)
                if (cue.At >= from && cue.At < T) due?.Add(cue.Id);
            CutsceneIntro intro = Current.Intro;
            if (intro != null && m_Intro == IntroState.Due && from < intro.At - BuildUp && T >= intro.At - BuildUp) due?.Add(BuildUpCue);
            if (intro != null && m_Intro == IntroState.Due && T >= intro.At)
            {
                m_Intro = IntroState.Playing;
                IntroT = 0f;
                due?.Add(IntroCue);
            }
            if (T >= Current.Seconds) Next();
            return due?.Count ?? 0;
        }

        /// <summary>The bubbles and the caption card on screen now, in the order they're written (not the narrator).</summary>
        public int Showing(List<CutsceneLine> into)
        {
            into.Clear();
            if (Done) return 0;
            foreach (CutsceneLine line in Current.Lines)
                if (T >= line.At && line.Who != Speaker.Narrator) into.Add(line);
            return into.Count;
        }

        /// <summary>The narrator's subtitle now: their latest line in this shot, or null.</summary>
        public string Narration => Narrating(out CutsceneLine line) ? line.Text : null;

        /// <summary>The narrator's line up now (their latest in this shot), if there is one.</summary>
        public bool Narrating(out CutsceneLine line)
        {
            line = default;
            if (Done) return false;
            bool any = false;
            foreach (CutsceneLine l in Current.Lines)
                if (l.Who == Speaker.Narrator && T >= l.At)
                {
                    line = l;
                    any = true;
                }
            return any;
        }

        void Next()
        {
            Shot++;
            T = 0f;
            m_Intro = IntroState.Due;
            IntroT = 0f;
            for (int i = 0; i < m_Tapped.Length; i++) m_Tapped[i] = false;
            if (Shot >= Scene.Shots.Length)
            {
                Shot = Scene.Shots.Length - 1;
                Done = true;
            }
        }
    }
}
