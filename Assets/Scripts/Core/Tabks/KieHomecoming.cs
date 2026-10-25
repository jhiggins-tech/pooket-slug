using System;
using System.Collections.Generic;

namespace PooketSlug
{
    /// <summary>
    /// kie's tabk comes home (<c>docs/design/cutscenes/den.md</c>, kie's tabk comes home): a beat in the tabk bay the first
    /// time the player walks in once kie's tabk is won (after Mission one), once ever, kept in the profile so it follows a
    /// sign-in; a tap skips it. Three shots, timed here; <see cref="BayFlow"/> plays it and the bay draws it.
    /// <list type="number">
    /// <item>Sparks under the crane: kie's tabk on the repair stands, half built (its hull, no projector), kie's legs
    /// sticking out from under it. A weasel holds up a spanner, a hand takes it and hands back an empty ice cream cone.</item>
    /// <item>The crane lowers the projector onto the turret ring, frosted and steaming; kie slides out, and he and the
    /// player trade three lines.</item>
    /// <item>kie climbs in, the reels spin up, the lens glows gold, a little Trollogram of the tabk waves from the wall,
    /// the weasel hops in after him, the name card drops in, and kie's last line.</item>
    /// </list>
    /// The script's shots are 3 s each; the second gets 6 s so its three lines can be read, and in the third kie's line
    /// comes in with the name card, for the same reason: every line is up for its words at
    /// <see cref="SceneCheck.WordsPerSecond"/>, as a scene file's are (<c>BeatReadingTests</c>).
    /// </summary>
    public sealed class KieHomecoming
    {
        /// <summary>The reveal kept once it's played (or skipped).</summary>
        public const string Reveal = "kie-tabk-home";

        public const float Shot2 = 3f, Shot3 = 9f, Length = Shot3 + 3f;
        /// <summary>Shot 1: the weasel's spanner up, taken; the cone handed back.</summary>
        public const float SpannerFrom = 0.5f, SpannerTaken = 1.4f, ConeFrom = 1.9f;
        /// <summary>Shot 2: the projector lowered, kie sliding out.</summary>
        public const float LowerTo = 4.3f, SlideFrom = 4.3f, SlideTo = 4.9f;
        /// <summary>Shot 3: kie climbing in, the reels spinning up, the lens and the wall's Trollogram, the weasel hopping in, the name card.</summary>
        public const float ClimbTo = Shot3 + 0.6f, ReelsFrom = Shot3 + 0.6f, ReelsTo = Shot3 + 2.1f, GlowFrom = Shot3 + 0.9f,
            HoloFrom = Shot3 + 1.1f, HopFrom = Shot3 + 1.4f, HopTo = Shot3 + 1.8f, CardFrom = Shot3 + 1.2f;

        /// <summary>The lines: who says them (true kie, false the player), and when.</summary>
        public static readonly IReadOnlyList<(bool kie, string text, float from, float to)> Lines = new[]
        {
            (true, "IT USED TO PROJECT ME.", 4.5f, 6.2f),
            (false, "IS IT MEANT TO STEAM?", 6.2f, 7.9f),
            (true, "TROLLOGRAMS RUN HOT.", 7.9f, Shot3),
            (true, "GOOD AS NEW. BETTER, ACTUALLY.", CardFrom, Length),
        };

        /// <summary>The name card's two parts.</summary>
        public const string CardName = "KIE'S TABK", CardTitle = "THE PROJECTIONIST";

        public float T { get; private set; }
        public bool Done => T >= Length - 1e-4f;

        public void Tick(float dt) => T += dt;
        public void Skip() => T = Length;

        /// <summary>Did <paramref name="at"/> pass in the tick just made (<paramref name="dt"/> long)? For its sounds.</summary>
        public bool Passed(float at, float dt) => T >= at && T - dt < at;

        /// <summary>The shot playing: 1, 2 or 3.</summary>
        public int Shot => T < Shot2 ? 1 : T < Shot3 ? 2 : 3;

        /// <summary>Sparks under the crane, crackling on and off, through the first shot.</summary>
        public bool Sparking => T < Shot2 && Fx.Step(T, 5f, 3) != 2;

        /// <summary>The weasel's spanner held up (before the hand takes it), and the cone handed back.</summary>
        public bool Spanner => T >= SpannerFrom && T < SpannerTaken;
        public bool Cone => T >= ConeFrom && T < Shot2;

        /// <summary>How far the crane's lowered the projector: 0 high up, 1 on the turret ring (and it stays).</summary>
        public float Lowered => T < Shot2 ? 0f : Between(T, Shot2, LowerTo);

        /// <summary>Whether the projector's on the tabk yet (the hull's half a tabk till then).</summary>
        public bool Projector => T >= Shot2;

        /// <summary>kie: under the tabk (0) to out and standing beside it (1) in shot 2, then climbing in (1 to 2) in shot 3.</summary>
        public float Kie => T < SlideFrom ? 0f : T < Shot3 ? Between(T, SlideFrom, SlideTo) : 1f + Between(T, Shot3, ClimbTo);

        /// <summary>The reels spinning (they turn faster as they spin up), the lens glowing, the wall's Trollogram, the weasel's hop in.</summary>
        public float Reels => Between(T, ReelsFrom, ReelsTo);
        public bool Glow => T >= GlowFrom;
        public bool Holo => T >= HoloFrom;
        public float Hop => Between(T, HopFrom, HopTo);

        /// <summary>The name card dropping in: 0 above the screen, 1 in place.</summary>
        public float Card => Between(T, CardFrom, CardFrom + 0.35f);

        /// <summary>The line showing now, or null.</summary>
        public (bool kie, string text)? Line
        {
            get
            {
                foreach (var l in Lines)
                    if (T >= l.from && T < l.to) return (l.kie, l.text);
                return null;
            }
        }

        /// <summary>
        /// Whether it plays as the bay opens: kie's tabk is in <paramref name="won"/>, and it hasn't played yet (a
        /// <c>?test=</c> jump is checking something else).
        /// </summary>
        public static bool Due(Profile profile, IReadOnlyList<TabkCard> won, bool testing)
        {
            if (testing || profile.HasSeen(Reveal)) return false;
            foreach (TabkCard c in won)
                if (c == TabkCards.Kie) return true;
            return false;
        }

        static float Between(float t, float from, float to) => Math.Max(0f, Math.Min(1f, (t - from) / (to - from)));
    }
}
