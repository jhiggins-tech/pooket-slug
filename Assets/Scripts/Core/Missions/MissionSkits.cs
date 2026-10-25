// The skits that play in a mission or at its debrief (docs/design/cutscenes/skits.md), rather than in the den or the bay:
// Keep it down (kie's line in Mission one's coda, on a replay) and They're tracking us (kcaj on Mission two's first-clear
// debrief). When each is due, its once-only key, and its timing; the world and the debrief only play them.
using System.Collections.Generic;

namespace PooketSlug
{
    /// <summary>
    /// Keep it down (docs/design/cutscenes/skits.md): as Mission one's coda gets under way on a replay, the tabk's engine
    /// turns over beside kie asleep against the wall, and he says his line in his sleep, as a popup over him. Once ever,
    /// solo (Mission one always is). You keep control throughout: nothing stops or waits.
    /// </summary>
    public static class KeepItDown
    {
        /// <summary>Its once-only key, in players' saves. It never changes.</summary>
        public const string Key = "skit-keep-it-down";
        /// <summary>What kie says, asleep.</summary>
        public const string Line = "...KEEP IT DOWN.";
        /// <summary>Seconds after the coda gets under way that he says it: the engine turns over first.</summary>
        public const float LineAt = 0.6f;
        /// <summary>He stirs from the start until a little after his line.</summary>
        public const float StirFor = LineAt + 0.5f;
        /// <summary>He settles back, and his Zs start again, once the line's gone.</summary>
        public const float ZsBack = LineAt + Tuning.Hud.PopupLife;
        /// <summary>About how long it takes, start to end (the script's 3 s).</summary>
        public const float Length = 3f;

        /// <summary>
        /// Whether it plays as <paramref name="mission"/>'s coda gets under way: Mission one's, on a replay (the coda's been
        /// cleared before, the same rule that drops its prompts), never yet seen, solo and not a sandbox. <c>?test=coda</c>
        /// (<paramref name="testCoda"/>) shows it every time, and other test starts never do.
        /// </summary>
        public static bool Due(Profile profile, MissionDef mission, StoryRun run, bool testCoda)
        {
            if (mission != Missions.One || run.Coop || run.Sandbox) return false;
            if (testCoda) return true;
            if (run.Testing) return false;
            return profile.HasSeen(StoryRecord.Coda) && !profile.HasSeen(Key);
        }

        /// <summary>Playing it marks it seen, unless it's a test start's (<c>?test=coda</c> shows it without spending it).</summary>
        public static bool Spends(StoryRun run) => !run.Testing;
    }

    /// <summary>
    /// They're tracking us (docs/design/cutscenes/skits.md): on Mission two's first clear, kcaj, on the debrief for the
    /// first time, leans in to the accuracy as it counts, talks it over with kie, and slaps a sticky note over the number.
    /// The debrief's tally waits for it (a tap skips it, and fills the numbers); the note stays for the rest of the debrief.
    /// The one skit that plays in co-op: each phone has its own debrief, so it plays on each whose first clear it is.
    /// </summary>
    public static class TrackingUs
    {
        /// <summary>Its once-only key, in players' saves. It never changes.</summary>
        public const string Key = "skit-tracking-us";
        /// <summary>The whole skit, in seconds: the tally's length while it plays.</summary>
        public const float Length = 12f;
        /// <summary>Where its three shots start (the script's 4 s, 3.5 s and 4.5 s).</summary>
        public const float Shot2 = 4f, Shot3 = 7.5f;
        /// <summary>The accuracy row starts counting here (the rows above it at the usual pace), and finishes here.</summary>
        public const float AccuracyFrom = 1.6f, AccuracyTo = 10.6f;
        /// <summary>The rows under the accuracy have counted by here.</summary>
        public const float TallyTo = 11.4f;
        /// <summary>kcaj leans in towards the accuracy, slowly, over these seconds; he straightens up once the note's on.</summary>
        public const float LeanFrom = 1.6f, LeanTo = 3.2f, LeanBackFrom = 10.1f, LeanBackTo = 10.5f;
        /// <summary>The note goes on the accuracy number (with its sound).</summary>
        public const float NoteAt = 9.9f;
        /// <summary>tones2 looks at the note over these seconds, and then at the player.</summary>
        public const float LookFrom = 10.2f, LookTo = 11.1f;

        /// <summary>A line of the skit: who says it, and when.</summary>
        public readonly struct Line
        {
            public readonly SoldierDef Who;
            public readonly string Text;
            public readonly float From, To;

            public Line(SoldierDef who, string text, float from, float to)
            {
                Who = who;
                Text = text;
                From = from;
                To = to;
            }
        }

        /// <summary>The lines, in order, one shot each.</summary>
        public static readonly IReadOnlyList<Line> Lines = new[]
        {
            new Line(Content.Kcaj, "ACCURACY? THEY'RE TRACKING US.", 2.1f, Shot2),
            new Line(Content.Kie, "IT'S THE SCORE.", Shot2 + 0.3f, Shot3),
            new Line(Content.Kcaj, "THAT'S WHAT THEY WANT YOU TO THINK.", Shot3, NoteAt + 0.3f),
        };

        /// <summary>
        /// Whether it plays at the debrief of a won <paramref name="mission"/>: Mission two's first clear on this phone
        /// (solo or co-op), never yet seen. <c>?test=debrief</c> (<paramref name="testDebrief"/>) on Mission two shows it every time.
        /// </summary>
        public static bool Due(Profile profile, MissionDef mission, bool firstClear, bool testDebrief)
        {
            if (mission != Missions.Two) return false;
            if (testDebrief) return true;
            return firstClear && !profile.HasSeen(Key);
        }

        /// <summary>Playing it marks it seen, unless it's <c>?test=debrief</c>'s.</summary>
        public static bool Spends(bool testDebrief) => !testDebrief;

        /// <summary>The line being said at <paramref name="t"/> seconds in, if any.</summary>
        public static Line? Saying(float t)
        {
            foreach (Line l in Lines)
                if (t >= l.From && t < l.To) return l;
            return null;
        }

        /// <summary>How far kcaj leans in towards the numbers at <paramref name="t"/>, 0 (upright) to 1.</summary>
        public static float Lean(float t)
        {
            if (t < LeanFrom) return 0f;
            if (t < LeanTo) return Smooth((t - LeanFrom) / (LeanTo - LeanFrom));
            if (t < LeanBackFrom) return 1f;
            if (t < LeanBackTo) return 1f - Smooth((t - LeanBackFrom) / (LeanBackTo - LeanBackFrom));
            return 0f;
        }

        /// <summary>The note's on the accuracy number at <paramref name="t"/> (and stays on for the rest of the debrief).</summary>
        public static bool NoteOn(float t) => t >= NoteAt;

        /// <summary>tones2 is looking at the note at <paramref name="t"/> (before and after, at the player).</summary>
        public static bool TonesLooks(float t) => t >= LookFrom && t < LookTo;

        /// <summary>
        /// How far the tally has counted at <paramref name="t"/>, 0 to 1, over <paramref name="rows"/> rows that count in
        /// turn (the debrief's <c>tally × rows − row</c>): the rows above <paramref name="accuracyRow"/> at the usual pace,
        /// the accuracy slowly while kcaj stares (on under the note), then the rest.
        /// </summary>
        public static float Tally(float t, int rows, int accuracyRow)
        {
            float before = accuracyRow / (float)rows, after = (accuracyRow + 1) / (float)rows;
            if (t <= 0f) return 0f;
            if (t < AccuracyFrom) return before * t / AccuracyFrom;
            if (t < AccuracyTo) return before + (after - before) * (t - AccuracyFrom) / (AccuracyTo - AccuracyFrom);
            if (t < TallyTo) return after + (1f - after) * (t - AccuracyTo) / (TallyTo - AccuracyTo);
            return 1f;
        }

        static float Smooth(float k) => k * k * (3f - 2f * k);
    }
}
