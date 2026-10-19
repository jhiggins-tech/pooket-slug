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

    /// <summary>A sound due <see cref="At"/> seconds into a shot. The ids are <see cref="Cues"/>'.</summary>
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
    /// A character's intro, the first time they're on screen in the story (<c>docs/design/cutscenes.md</c>, Character
    /// intros): it builds up for <see cref="CutscenePlayer.BuildUp"/> seconds first (a riser, and the camera creeping in),
    /// then at <see cref="At"/> seconds into the shot it lands: the camera crash-zooms onto <see cref="Figure"/>, their name
    /// slams up big, time goes slow-mo, and a musical hit plays.
    /// </summary>
    public sealed class CutsceneIntro
    {
        public readonly float At;
        /// <summary>The soldier on the stage it's for (<see cref="Staging.Things"/>).</summary>
        public readonly string Figure;
        public readonly string Name;

        public CutsceneIntro(float at, string figure, string name)
        {
            At = at;
            Figure = figure;
            Name = name;
        }
    }

    /// <summary>
    /// One shot of a scene: how long it holds by itself, its set, its bubbles and its cues, any character's intro, and
    /// its staging (<see cref="ShotStage"/>).
    /// </summary>
    public sealed class CutsceneShot
    {
        public readonly string Name;
        public readonly float Seconds;
        public readonly CutsceneLine[] Lines;
        public readonly CutsceneCue[] Cues;
        public readonly CutsceneIntro Intro;
        /// <summary>Its set (<see cref="Staging.Sets"/>): the painted backdrop and the floor's height.</summary>
        public readonly string Set;
        public readonly StageStep[] Steps;

        public CutsceneShot(string name, float seconds, CutsceneLine[] lines, CutsceneCue[] cues = null, CutsceneIntro intro = null, string set = "street", StageStep[] steps = null)
        {
            Name = name;
            Seconds = seconds;
            Lines = lines ?? new CutsceneLine[0];
            Cues = cues ?? new CutsceneCue[0];
            Intro = intro;
            Set = set;
            Steps = steps ?? new StageStep[0];
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

    /// <summary>
    /// Plays a scene (<c>docs/design/cutscenes.md</c>, How every cutscene plays): each shot moves on by itself, or with a tap. A scene
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
        public const float TapGuard = Tuning.Hud.TapGuard;
        /// <summary>How long a character's intro holds, in real seconds, and how slow the shot runs meanwhile.</summary>
        public const float IntroSeconds = 1.8f, SlowMo = 0.15f;
        /// <summary>How long an intro builds up before it lands, in shot seconds (the shot runs at full speed till then).</summary>
        public const float BuildUp = 2.5f;
        /// <summary>The cues that come due as an intro's build-up starts (the riser) and as it lands (the musical hit).</summary>
        public const string BuildUpCue = Cues.IntroBuild, IntroCue = Cues.Intro;

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

        public CutsceneShot Current => Done ? null : Scene.Shots[Shot];

        /// <summary>How far into the build-up to the coming intro (0 to 1), or 0 if there's none building.</summary>
        public float BuildingUp
        {
            get
            {
                CutsceneIntro intro = Current?.Intro;
                if (intro == null || m_Intro != IntroState.Due) return 0f;
                return System.Math.Max(0f, System.Math.Min(1f, (T - (intro.At - BuildUp)) / BuildUp));
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
    /// A co-op scene's taps, kept in step over two phones (<c>docs/design/cutscenes.md</c>, How every cutscene plays: a shot moves on early
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
