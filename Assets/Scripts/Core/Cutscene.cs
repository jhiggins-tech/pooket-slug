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

    /// <summary>
    /// How long each of the narrator's recorded lines runs, in seconds (measured from the vendor repository's
    /// narration.zip). A narrator line gets at least its voice plus <see cref="Breath"/> on screen before their next
    /// line or the end of its shot, so the voice is never cut off; a test holds every scene to it. A line re-recorded
    /// longer still isn't clipped: the scene waits for the voice (<see cref="CutscenePlayer.Tick"/>).
    /// </summary>
    public static class NarratorClips
    {
        public const float Breath = 0.4f;

        public static readonly Dictionary<string, float> Seconds = new Dictionary<string, float>
        {
            { "opening-1", 3.54f }, { "opening-2", 1.42f }, { "opening-3", 2.9f }, { "opening-4a", 2.96f },
            { "opening-4b", 2.5f }, { "opening-5", 2.56f }, { "opening-6", 1.76f },
            { "motor-pool-1", 2.67f }, { "motor-pool-2", 3.26f }, { "motor-pool-3", 3.84f }, { "motor-pool-4", 2.13f },
            { "freeing-1", 3.08f }, { "freeing-2", 2.53f }, { "freeing-3", 3.35f },
        };
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

    /// <summary>
    /// A scene: its shots, the id it's remembered by once seen (<see cref="Profile.seen"/>), and a small number that
    /// names it to a co-op partner's phone (<see cref="CutsceneSync"/>).
    /// </summary>
    public sealed class CutsceneDef
    {
        public readonly string Id;
        public readonly int NetId;
        public readonly CutsceneShot[] Shots;

        public CutsceneDef(string id, int netId, params CutsceneShot[] shots)
        {
            Id = id;
            NetId = netId;
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
        public static readonly CutsceneDef Opening = new CutsceneDef("scene-opening", 1,
            new CutsceneShot("Outside the hangout", 8.5f,
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
                    new CutsceneLine(1.2f, Speaker.Narrator, "One of the raiders is still here: a Trollogram.", "opening-4a"),
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

        /// <summary>
        /// Freeing kie, after Trollogram kie is beaten on foot. His glitch-out (the design's shot 1) plays in the game
        /// itself (<c>Pilot</c>); the scene picks up as the cage of light flickers out and the real kie drops,
        /// with his intro. Then the snacks, the release of tones2's tabk (the cargo drone, and its remote handed over),
        /// and kie nodding off as the alarms start. It can be skipped once seen.
        /// </summary>
        public static readonly CutsceneDef FreeingKie = new CutsceneDef("scene-freeing-kie", 3,
            new CutsceneShot("The real kie", 7.5f,
                new[]
                {
                    new CutsceneLine(3.4f, Speaker.Narrator, "And out drops the real kie. Bit peaky.", "freeing-1"), // after his intro
                    new CutsceneLine(4.4f, Speaker.Kie, "...HOW LONG WAS I IN THERE?"),
                },
                new[] { new CutsceneCue(0.1f, "cage"), new CutsceneCue(1.5f, "kie-drop") },
                intro: new CutsceneIntro(3.0f, Speaker.Kie, "KIE")),
            new CutsceneShot("The bit", 4.5f,
                new[]
                {
                    new CutsceneLine(0.8f, Speaker.Tones2, "BROUGHT SNACKS."),
                    new CutsceneLine(2.4f, Speaker.Kie, "LEGEND."),
                },
                new[] { new CutsceneCue(2.2f, "snacks") }),
            new CutsceneShot("The release", 6f,
                new[]
                {
                    new CutsceneLine(0.3f, Speaker.Narrator, "Then he sets tones2's tabk free.", "freeing-2"),
                    new CutsceneLine(3.6f, Speaker.Kie, "YOURS. CALL IT WHEN YOU NEED IT."),
                },
                new[] { new CutsceneCue(1.2f, "rip"), new CutsceneCue(1.6f, "drone"), new CutsceneCue(2.7f, "lift"), new CutsceneCue(4.2f, "remote") }),
            new CutsceneShot("Lights out", 7f,
                new[]
                {
                    new CutsceneLine(0.9f, Speaker.Kie, "I'M GONNA... SIT THIS ONE OUT."),
                    new CutsceneLine(3.0f, Speaker.Narrator, "kie's done for the night. tones2 isn't.", "freeing-3"),
                },
                new[] { new CutsceneCue(0.1f, "alarm"), new CutsceneCue(2.6f, "alarm") }));

        /// <summary>
        /// A stand-in for later missions' co-op scenes, for the two-phone check (<c>?test=scene</c>, CI's co-op test):
        /// Mission one is single player only, so its own scenes never play in a co-op room. Six plain shots on the
        /// opening's stage, each held long enough that a phone tapping alone is plainly waiting.
        /// </summary>
        public static readonly CutsceneDef CoopTest = new CutsceneDef("scene-coop-test", 63,
            new CutsceneShot("Test shot 1", 6f, new[] { new CutsceneLine(0.3f, Speaker.Tones2, "TEST SHOT 1 OF 6: TAP TOGETHER") }),
            new CutsceneShot("Test shot 2", 6f, new[] { new CutsceneLine(0.3f, Speaker.Tones2, "TEST SHOT 2 OF 6: TAP TOGETHER") }),
            new CutsceneShot("Test shot 3", 6f, new[] { new CutsceneLine(0.3f, Speaker.Tones2, "TEST SHOT 3 OF 6: TAP TOGETHER") }),
            new CutsceneShot("Test shot 4", 6f, new[] { new CutsceneLine(0.3f, Speaker.Tones2, "TEST SHOT 4 OF 6: TAP TOGETHER") }),
            new CutsceneShot("Test shot 5", 6f, new[] { new CutsceneLine(0.3f, Speaker.Tones2, "TEST SHOT 5 OF 6: TAP TOGETHER") }),
            new CutsceneShot("Test shot 6", 6f, new[] { new CutsceneLine(0.3f, Speaker.Tones2, "TEST SHOT 6 OF 6: TAP TOGETHER") }));

        /// <summary>
        /// The motor pool reveal: the first-ever reveal, that tabks exist. It plays once, ever, the first time tones2
        /// reaches the motor pool, and it's kie's tabk's entrance: the boss intro after it skips the roll-out and goes
        /// straight to the banner and the name card. Trollogram kie gets his intro as he pops up in the hatch.
        /// </summary>
        public static readonly CutsceneDef MotorPool = new CutsceneDef("scene-motor-pool", 2,
            new CutsceneShot("The shutter", 4.5f,
                new[] { new CutsceneLine(0.8f, Speaker.Narrator, "Behind the shutter: the base's motor pool.", "motor-pool-1") },
                new[] { new CutsceneCue(0.2f, "shutter"), new CutsceneCue(2.6f, "hum") }),
            new CutsceneShot("The tabks", 5.5f,
                new[]
                {
                    new CutsceneLine(0.4f, Speaker.Narrator, "And parked inside, all in a row: the crew's tabks.", "motor-pool-2"),
                    new CutsceneLine(3.2f, Speaker.Tones2, "...ARE THOSE OURS?"),
                }),
            new CutsceneShot("kie's tabk", 9.5f,
                new[]
                {
                    new CutsceneLine(4.8f, Speaker.Narrator, "That's kie. Or something wearing his face.", "motor-pool-3"), // after his intro
                    new CutsceneLine(5.9f, Speaker.Hologram, "HEY TONES. WANNA RACE?"),
                },
                new[] { new CutsceneCue(0.2f, "engine"), new CutsceneCue(2.1f, "hatch"), new CutsceneCue(2.4f, "static") },
                intro: new CutsceneIntro(4.5f, Speaker.Hologram, "TROLLO-KIE")),
            new CutsceneShot("Hatch shut", 4.5f,
                new[]
                {
                    new CutsceneLine(0.3f, Speaker.Narrator, "kie has never once offered to race.", "motor-pool-4"),
                    new CutsceneLine(2.6f, Speaker.Tones2, "THAT'S NOT KIE."),
                },
                new[] { new CutsceneCue(0.5f, "slam"), new CutsceneCue(1.0f, "turret") }));
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

        /// <summary>
        /// Where the scene is, as a number that only goes up: three beats a shot (before its intro's build-up, in the
        /// build-up or the intro, and after it; a shot with no intro is always on its last). A tap ends the beat it's
        /// made in, so a co-op partner's phone can tell which beat they've tapped through (<see cref="CutsceneSync"/>).
        /// </summary>
        public int Beat
        {
            get
            {
                if (Done) return Scene.Shots.Length * 3;
                CutsceneIntro intro = Current.Intro;
                int stage = intro == null || m_Intro == IntroState.Over ? 2 : m_Intro == IntroState.Playing || T >= intro.At - BuildUp ? 1 : 0;
                return Shot * 3 + stage;
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
        public int Tick(float dt, List<string> due = null, bool narratorTalking = false)
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
            if (narratorTalking && m_Held < MaxHold)
            {
                // the narrator's still talking: their next line, and the end of the shot, wait for them to finish
                float until = NextNarration(from);
                if (T >= until)
                {
                    m_Held += T - from;
                    T = System.Math.Max(from, until - 0.001f);
                }
            }
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

        /// <summary>Most a shot waits on the narrator, in case a voice never reports it's done.</summary>
        public const float MaxHold = 3f;
        float m_Held;

        /// <summary>When the narrator's next line in this shot is due after <paramref name="from"/>, or the shot's end.</summary>
        float NextNarration(float from)
        {
            float until = Current.Seconds;
            foreach (CutsceneLine l in Current.Lines)
                if (l.Who == Speaker.Narrator && l.At > from && l.At < until) until = l.At;
            return until;
        }

        void Next()
        {
            Shot++;
            T = 0f;
            m_Held = 0f;
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

    /// <summary>
    /// A co-op scene's taps, kept in step over two phones (<c>docs/design/story.md</c>, Cutscenes: a shot moves on early
    /// once both have tapped, and a scene seen before is skipped once both want to). Each phone sends one number: the
    /// round and the scene it's in, the last <see cref="CutscenePlayer.Beat"/> it tapped through, and whether it's asked
    /// to skip. Each plays the scene by its own clock, so the timer still moves it on for both.
    /// </summary>
    public static class CutsceneSync
    {
        const int BeatBits = 10, SceneBits = 8, RoundBits = 12, SkipBeat = (1 << BeatBits) - 1;

        /// <summary>The number to send. <paramref name="tapped"/> is the last beat tapped through, -1 for none yet.</summary>
        public static int Pack(int round, int scene, int tapped, bool skip)
        {
            int beat = skip ? SkipBeat : System.Math.Max(0, System.Math.Min(SkipBeat - 1, tapped + 1));
            return ((round & ((1 << RoundBits) - 1)) << (BeatBits + SceneBits)) | ((scene & ((1 << SceneBits) - 1)) << BeatBits) | beat;
        }

        /// <summary>
        /// Reads a partner's number, if it's about this <paramref name="round"/>'s <paramref name="scene"/> (0, sent
        /// before they started it, never is): the last beat they tapped through (-1 for none), and whether they want to skip.
        /// </summary>
        public static bool Read(int value, int round, int scene, out int tapped, out bool skip)
        {
            tapped = -1;
            skip = false;
            if (value == 0 || value != (Pack(round, scene, -1, false) | (value & ((1 << BeatBits) - 1)))) return false;
            int beat = value & SkipBeat;
            skip = beat == SkipBeat;
            tapped = skip ? int.MaxValue : beat - 1;
            return true;
        }

        /// <summary>
        /// Plays a partner's number into <paramref name="player"/> (player 1): they've tapped the beat it's on, or are
        /// past it. With no partner there (they left), nobody's waited for. True if they've asked to skip.
        /// </summary>
        public static bool Apply(CutscenePlayer player, bool partnerHere, int value, int round)
        {
            if (player.Done) return false;
            if (!partnerHere)
            {
                player.Tap(1);
                return true;
            }
            if (!Read(value, round, player.Scene.NetId, out int tapped, out bool skip)) return false;
            if (tapped >= player.Beat) player.Tap(1);
            return skip;
        }
    }
}
