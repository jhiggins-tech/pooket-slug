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

        public CutsceneLine(float at, Speaker who, string text)
        {
            At = at;
            Who = who;
            Text = text;
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

    /// <summary>One shot of a scene: how long it holds by itself, its bubbles and its cues.</summary>
    public sealed class CutsceneShot
    {
        public readonly string Name;
        public readonly float Seconds;
        public readonly CutsceneLine[] Lines;
        public readonly CutsceneCue[] Cues;

        public CutsceneShot(string name, float seconds, CutsceneLine[] lines, CutsceneCue[] cues = null)
        {
            Name = name;
            Seconds = seconds;
            Lines = lines ?? new CutsceneLine[0];
            Cues = cues ?? new CutsceneCue[0];
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
            new CutsceneShot("Outside the hangout", 5f,
                new[]
                {
                    new CutsceneLine(0.4f, Speaker.Narrator, "Friday night. tones2's back from the snack run."),
                    new CutsceneLine(3.2f, Speaker.Tones2, "SNACKS ARE HERE LADS"),
                }),
            new CutsceneShot("The door", 4.5f,
                new[]
                {
                    new CutsceneLine(0.9f, Speaker.Narrator, "But the door's been kicked in."),
                    new CutsceneLine(2.4f, Speaker.Tones2, "...LADS?"),
                },
                new[] { new CutsceneCue(0.5f, "door"), new CutsceneCue(1.2f, "static") }),
            new CutsceneShot("Inside, raided", 5f,
                new[] { new CutsceneLine(0.5f, Speaker.Narrator, "The hangout's trashed, and the crew are gone.") },
                new[] { new CutsceneCue(2.2f, "drop"), new CutsceneCue(3.4f, "static") }),
            new CutsceneShot("A straggler", 7f,
                new[]
                {
                    new CutsceneLine(0.8f, Speaker.Goon, "INTRUDER. INTRUD-"),
                    new CutsceneLine(1.4f, Speaker.Narrator, "One of the raiders is still here: a hologram."),
                    new CutsceneLine(4.6f, Speaker.Narrator, "It glitches out, and drops its keycard."),
                },
                new[] { new CutsceneCue(0.3f, "goon"), new CutsceneCue(3.2f, "jet"), new CutsceneCue(3.8f, "glitch"), new CutsceneCue(4.2f, "card-drop") }),
            new CutsceneShot("The keycard", 5f,
                new[]
                {
                    new CutsceneLine(0.5f, Speaker.Narrator, "A pass for the military base, up on the hill."),
                    new CutsceneLine(2.6f, Speaker.Tones2, "THAT'S WHERE THEY TOOK THEM."),
                },
                new[] { new CutsceneCue(0.1f, "card-up") }),
            new CutsceneShot("Heading out", 5f,
                new[]
                {
                    new CutsceneLine(0.4f, Speaker.Narrator, "So tones2 goes in alone."),
                    new CutsceneLine(1.2f, Speaker.Tones2, "RIGHT."),
                    new CutsceneLine(2.8f, Speaker.Caption, "MISSION 1 · THE MILITARY BASE\nFIND THE CREW"),
                },
                new[] { new CutsceneCue(1.6f, "crush"), new CutsceneCue(2.8f, "card"), new CutsceneCue(2.9f, "say:mission-start") }));
    }

    /// <summary>
    /// Plays a scene (<c>docs/design/story.md</c>, Cutscenes): each shot moves on by itself, or with a tap. A scene
    /// that's been seen before can be skipped. In co-op every player has to tap before a shot moves on early, so a
    /// partner who hasn't seen it gets to watch it; the timer still moves it on for both.
    /// </summary>
    public sealed class CutscenePlayer
    {
        /// <summary>A tap this soon into a shot is ignored, so mashing doesn't throw away two shots at once.</summary>
        public const float TapGuard = 0.3f;

        public readonly CutsceneDef Scene;
        public readonly bool CanSkip;
        readonly bool[] m_Tapped;

        public int Shot { get; private set; }
        /// <summary>Seconds into the current shot.</summary>
        public float T { get; private set; }
        public bool Done { get; private set; }
        public bool Skipped { get; private set; }

        public CutscenePlayer(CutsceneDef scene, bool seenBefore, int players = 1)
        {
            Scene = scene;
            CanSkip = seenBefore;
            m_Tapped = new bool[System.Math.Max(1, players)];
        }

        public int Players => m_Tapped.Length;
        public CutsceneShot Current => Done ? null : Scene.Shots[Shot];

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

        /// <summary><paramref name="player"/> tapped to move on. True if that moved the scene on.</summary>
        public bool Tap(int player)
        {
            if (Done || player < 0 || player >= m_Tapped.Length || T < TapGuard) return false;
            m_Tapped[player] = true;
            foreach (bool t in m_Tapped)
                if (!t) return false;
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
            float from = T;
            T += dt;
            foreach (CutsceneCue cue in Current.Cues)
                if (cue.At >= from && cue.At < T) due?.Add(cue.Id);
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
        public string Narration
        {
            get
            {
                if (Done) return null;
                string said = null;
                foreach (CutsceneLine line in Current.Lines)
                    if (line.Who == Speaker.Narrator && T >= line.At) said = line.Text;
                return said;
            }
        }

        void Next()
        {
            Shot++;
            T = 0f;
            for (int i = 0; i < m_Tapped.Length; i++) m_Tapped[i] = false;
            if (Shot >= Scene.Shots.Length)
            {
                Shot = Scene.Shots.Length - 1;
                Done = true;
            }
        }
    }
}
