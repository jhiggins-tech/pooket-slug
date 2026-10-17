using System.Collections.Generic;

namespace PooketSlug
{
    /// <summary>
    /// Career XP and rank (<c>docs/design/player.md</c>, What the player keeps). Placeholder amounts on Tabks' scale,
    /// to tune after playtesting.
    /// </summary>
    public static class Progression
    {
        public const int ClearXp = 200, FreedXp = 300, KillXp = 1;
        /// <summary>
        /// Stages beaten up the ladder (the record's <c>rank</c>): a rung is <see cref="Rung"/> stages. Until missions
        /// are split into stages, a clear counts as one, and the first clear of a mission as a whole rung.
        /// </summary>
        public const int Rung = 4, FirstClearClimb = Rung, ReplayClimb = 1;
        /// <summary>Every this much career XP earns a looks token (<c>docs/design/player.md</c>).</summary>
        public const int LooksTokenXp = 600;

        /// <summary>How far career XP <paramref name="xp"/> is towards the next looks token, 0 to 1 (the debrief's XP bar).</summary>
        public static float TokenProgress(int xp) => (xp % LooksTokenXp) / (float)LooksTokenXp;

        /// <summary>The rank <paramref name="climb"/> stages up the ladder is (the lowest at 0).</summary>
        public static int RankIndex(int climb) => System.Math.Max(0, System.Math.Min(Ranks.All.Length - 1, climb / Rung));

        public static Rank RankAt(int climb) => Ranks.All[RankIndex(climb)];

        /// <summary>How far through the current rank, 0 to 1 (1 at the top of the ladder).</summary>
        public static float RungProgress(int climb) =>
            RankIndex(climb) >= Ranks.All.Length - 1 ? 1f : (climb - RankIndex(climb) * Rung) / (float)Rung;

        /// <summary>A won run's XP, part by part: the clear, the crewmate it freed (if any), and the kills.</summary>
        public static List<XpPart> XpFor(bool won, string freed, int kills)
        {
            var parts = new List<XpPart>();
            if (won) parts.Add(new XpPart("MISSION CLEARED", ClearXp));
            if (won && freed != null) parts.Add(new XpPart(freed.ToUpperInvariant() + " FREED", FreedXp));
            if (kills > 0) parts.Add(new XpPart(kills + (kills == 1 ? " KILL" : " KILLS"), kills * KillXp));
            return parts;
        }

        /// <summary>How far a won run moves you up the ladder: a whole rung the first time a mission's beaten.</summary>
        public static int ClimbFor(bool won, bool firstClear) => !won ? 0 : firstClear ? FirstClearClimb : ReplayClimb;
    }

    public readonly struct XpPart
    {
        public readonly string Label;
        public readonly int Amount;

        public XpPart(string label, int amount)
        {
            Label = label;
            Amount = amount;
        }
    }

    /// <summary>What a won run's debrief shows.</summary>
    public sealed class DebriefResult
    {
        public int Score, Best, Seconds, Kills, Lives, Continues;
        public bool NewBest;
        /// <summary>Shots that hit, as a share of shots fired (-1 when nothing was fired).</summary>
        public float Accuracy = -1f;
        public List<XpPart> Xp = new List<XpPart>();
        /// <summary>Career XP before this run, and the stages beaten before and after.</summary>
        public int XpBefore, ClimbBefore, ClimbAfter;
        /// <summary>The first debrief: XP and rank are being shown for the first time, with kie's lines.</summary>
        public bool First;
        /// <summary>The player isn't signed in, so the debrief offers it (the first time, and on every later one).</summary>
        public bool OfferSignIn;

        public int XpGained
        {
            get
            {
                int sum = 0;
                foreach (XpPart p in Xp) sum += p.Amount;
                return sum;
            }
        }

        public bool RankedUp => Progression.RankIndex(ClimbAfter) > Progression.RankIndex(ClimbBefore);
    }

    /// <summary>
    /// Mission one's debrief, and every won mission's after it (<c>docs/design/story.md</c>, The first debrief): the
    /// tally, then the XP, then the rank, then (for players who aren't signed in) the sign-in offer, then CONTINUE.
    /// Each part counts up fast and a tap fills it at once; another tap moves on.
    /// </summary>
    public sealed class DebriefPlayer
    {
        public enum Part { Tally, Xp, Rank, SignIn, Continue }

        /// <summary>How long each part takes to count up, in seconds.</summary>
        public const float TallySeconds = 2.4f, XpSeconds = 2f, RankSeconds = 1.6f;
        /// <summary>A tap this soon after a part starts is ignored, so a mashed button doesn't skip a part unseen.</summary>
        public const float TapGuard = 0.25f;

        readonly List<Part> m_Parts = new List<Part>();
        int m_At;

        public readonly DebriefResult Result;

        public DebriefPlayer(DebriefResult result)
        {
            Result = result;
            m_Parts.Add(Part.Tally);
            m_Parts.Add(Part.Xp);
            m_Parts.Add(Part.Rank);
            if (result.OfferSignIn) m_Parts.Add(Part.SignIn);
            m_Parts.Add(Part.Continue);
        }

        public Part Now => m_Parts[m_At];
        public IReadOnlyList<Part> Parts => m_Parts;
        /// <summary>Seconds into the current part.</summary>
        public float T { get; private set; }
        /// <summary>Has the current part (and so every one before it) finished counting up?</summary>
        public bool Counted => T >= Length(Now);
        /// <summary>How far the current part has counted up, 0 to 1.</summary>
        public float Progress => Length(Now) <= 0f ? 1f : System.Math.Min(1f, T / Length(Now));

        /// <summary>Has <paramref name="part"/> been reached (it shows from then on)?</summary>
        public bool Reached(Part part)
        {
            int i = m_Parts.IndexOf(part);
            return i >= 0 && i <= m_At;
        }

        /// <summary>
        /// What kie says now, or null: the first debrief explains the XP as it counts up, and the sign-in offer asks
        /// (<c>docs/design/story.md</c>, The first debrief).
        /// </summary>
        public string KieSays =>
            Now == Part.SignIn ? "SIGN IN SO THIS DOESN'T VANISH." : Result.First && Now == Part.Xp ? "YOU GET XP FOR THAT." : null;

        /// <summary>The sign-in button shows: in its own part, and at CONTINUE for a player who isn't signed in.</summary>
        public bool ShowsSignIn => Now == Part.SignIn || Now == Part.Continue && Result.OfferSignIn;

        static float Length(Part p) => p == Part.Tally ? TallySeconds : p == Part.Xp ? XpSeconds : p == Part.Rank ? RankSeconds : 0f;

        /// <summary>Moves time on. True on the frame the current part finishes counting.</summary>
        public bool Tick(float dt)
        {
            bool was = Counted;
            T += dt;
            return !was && Counted;
        }

        /// <summary>
        /// A tap: fills the part counting up at once, or moves on to the next part. Returns false at CONTINUE, where
        /// a tap means leave (the caller's to do).
        /// </summary>
        public bool Tap()
        {
            if (Now == Part.Continue) return false;
            if (T < TapGuard) return true;
            if (!Counted)
            {
                T = Length(Now);
                return true;
            }
            m_At++;
            T = 0f;
            return true;
        }
    }
}
