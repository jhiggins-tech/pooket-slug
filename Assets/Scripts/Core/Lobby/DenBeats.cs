using System;
using System.Collections.Generic;

namespace PooketSlug
{
    /// <summary>The den's beats (<c>docs/design/cutscenes.md</c>, The den): the first visit, and a crewmate moving in.</summary>
    public enum DenBeatKind { FirstVisit, MoveIn }

    /// <summary>
    /// One of the den's beats, played as the den opens, timed here (a few seconds, no narrator: only a speech bubble).
    /// <para>
    /// <b>The first visit</b> (4 s): the den still trashed from the raid and dark; kie heaves the couch back upright,
    /// tones2 plugs the TV back in, and the room warms up (the raid's mess fades into the den as it is) as kie says HOME
    /// SWEET HOME. <b>A crewmate moves in</b> (3 s): the roller door rattles up, the newcomer steps in under it and walks
    /// in, one of the crew says hi from by the couch, and their corner lights up with a clunk; the door rolls down.
    /// </para>
    /// </summary>
    public sealed class DenBeat
    {
        public const float FirstVisitSeconds = 4f, MoveInSeconds = 3f;
        /// <summary>The first visit: kie heaving the couch, tones2 at the TV, the room warming up, kie's line.</summary>
        public const float HeaveFrom = 0.2f, HeaveTo = 1.2f, PlugFrom = 0.8f, PlugTo = 1.6f, WarmFrom = 1.4f, WarmTo = 2.6f, HomeAt = 2.4f;
        /// <summary>Moving in: the door up, the newcomer through it and walking in, the hi, the corner lighting up, the door down.</summary>
        public const float UpTo = 0.6f, StepTo = 1.0f, WalkTo = 2.6f, HiFrom = 1.2f, HiTo = 2.8f, LitAt = 1.8f, LitTo = 2.2f, DownFrom = 2.4f;
        /// <summary>How far the newcomer walks in from the door.</summary>
        public const float WalkIn = 40f;

        public readonly DenBeatKind Kind;
        /// <summary>Who moves in (null for the first visit), and who says the line.</summary>
        public readonly SoldierDef Newcomer, Speaker;
        public readonly string Line;
        public float T { get; private set; }

        DenBeat(DenBeatKind kind, SoldierDef newcomer, SoldierDef speaker, string line)
        {
            Kind = kind;
            Newcomer = newcomer;
            Speaker = speaker;
            Line = line;
        }

        public static DenBeat FirstVisit() => new DenBeat(DenBeatKind.FirstVisit, null, Content.Kie, "HOME SWEET HOME.");

        /// <summary><paramref name="newcomer"/> moves in; tones2 says hi (kie, if it's tones2 moving in).</summary>
        public static DenBeat MoveIn(SoldierDef newcomer) =>
            new DenBeat(DenBeatKind.MoveIn, newcomer, newcomer == Content.Tones2 ? Content.Kie : Content.Tones2, "HEY. WELCOME HOME.");

        public float Length => Kind == DenBeatKind.FirstVisit ? FirstVisitSeconds : MoveInSeconds;
        public bool Done => T >= Length - 1e-4f;

        /// <summary>Moves it on.</summary>
        public void Tick(float dt) => T += dt;

        /// <summary>Tap to skip: straight to the end.</summary>
        public void Skip() => T = Length;

        /// <summary>Did <paramref name="at"/> pass in the last <paramref name="dt"/> (the tick just made)?</summary>
        public bool Passed(float at, float dt) => T >= at && T - dt < at;

        /// <summary>The line's showing.</summary>
        public bool Speaking => Kind == DenBeatKind.FirstVisit ? T >= HomeAt : T >= HiFrom && T < HiTo;

        // ---------------------------------------------------------------- the first visit

        /// <summary>How far the room's warmed up: 0 trashed and dark, 1 the den as it is.</summary>
        public float Warm => Kind == DenBeatKind.FirstVisit ? Ease(Between(T, WarmFrom, WarmTo)) : 1f;

        /// <summary>kie heaving the couch upright (0..1), tones2 plugging the TV in (0..1).</summary>
        public float Heave => Between(T, HeaveFrom, HeaveTo);
        public float Plug => Between(T, PlugFrom, PlugTo);

        // ---------------------------------------------------------------- moving in

        /// <summary>The roller door: up, held while they come in, then down.</summary>
        public float DoorLift => Kind != DenBeatKind.MoveIn ? 0f : T < UpTo ? Between(T, 0f, UpTo) : 1f - Between(T, DownFrom, MoveInSeconds);

        /// <summary>How far through the door the newcomer still is (1 behind it, 0 in the den).</summary>
        public float Through => 1f - Between(T, UpTo, StepTo);

        /// <summary>How far in from the door they've walked (0 at the door, 1 <see cref="WalkIn"/> in).</summary>
        public float Walked => Between(T, StepTo, WalkTo);

        /// <summary>Their corner lighting up: 0 dark, 1 lit.</summary>
        public float Lit => Kind != DenBeatKind.MoveIn ? 1f : Between(T, LitAt, LitTo);

        static float Between(float t, float from, float to) => Math.Max(0f, Math.Min(1f, (t - from) / (to - from)));
        static float Ease(float k) => k * k * (3f - 2f * k);
    }

    /// <summary>
    /// Which of the den's beats play as it opens (<c>docs/design/lobby.md</c>, When the den first appears; Filling the den
    /// as crew are freed): the first visit, once ever, then a move-in for each freed crewmate who hasn't had theirs (tones2
    /// and kie were home for the first visit). Kept as reveals in the profile, so they follow a sign-in. Never in a
    /// co-op room: the two phones would play them apart.
    /// </summary>
    public static class DenBeats
    {
        public const string FirstVisit = "den-first-visit";

        /// <summary>The reveal for <paramref name="s"/> having moved in.</summary>
        public static string MovedIn(SoldierDef s) => "moved-in-" + s.Name;

        /// <summary>Home for the first visit: no move-in of their own.</summary>
        public static bool Starter(SoldierDef s) => s == Content.Tones2 || s == Content.Kie;

        /// <summary>The beats due as the den opens in <paramref name="mode"/>, with <paramref name="crew"/> freed, in order.</summary>
        public static List<DenBeat> Due(Profile profile, IReadOnlyList<SoldierDef> crew, DenMode mode)
        {
            var due = new List<DenBeat>();
            if (mode == DenMode.Coop) return due;
            if (!profile.HasSeen(FirstVisit)) due.Add(DenBeat.FirstVisit());
            foreach (SoldierDef s in crew)
                if (!Starter(s) && !profile.HasSeen(MovedIn(s))) due.Add(DenBeat.MoveIn(s));
            return due;
        }

        /// <summary>Marks <paramref name="beat"/> played (skipped counts). True if that changed the profile.</summary>
        public static bool Played(Profile profile, DenBeat beat) =>
            profile.MarkSeen(beat.Kind == DenBeatKind.FirstVisit ? FirstVisit : MovedIn(beat.Newcomer));
    }
}
