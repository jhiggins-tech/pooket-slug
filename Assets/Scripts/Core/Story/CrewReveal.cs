namespace PooketSlug
{
    /// <summary>
    /// Two of the crew (<c>docs/design/story.md</c>): the third reveal, played once on the title screen after Mission
    /// one is beaten. The old title, kie walks in, START splits into the grown menu, then kie's line. A tap moves on;
    /// it can't be skipped.
    /// </summary>
    public sealed class CrewReveal
    {
        public enum Shot { OldTitle, KieWalksIn, MenuGrows, KiesLine, Done }

        /// <summary>Each shot's length, in seconds.</summary>
        public static readonly float[] Seconds = { 1f, 2f, 3f, 2f };
        /// <summary>In the menu shot: START cracks, then the new buttons drop in, one every <see cref="DropGap"/>.</summary>
        public const float CrackSeconds = 0.5f, DropGap = 0.8f, DropSeconds = 0.25f;
        /// <summary>A tap this soon after a shot starts is ignored, so a mashed button doesn't skip one unseen.</summary>
        public const float TapGuard = Tuning.Hud.TapGuard;
        /// <summary>The new buttons, in the order they drop.</summary>
        public static readonly string[] NewButtons = { "MISSIONS", "CO-OP", "CREW" };

        public Shot Now { get; private set; }
        /// <summary>Seconds into the current shot.</summary>
        public float T { get; private set; }
        public bool Done => Now == Shot.Done;

        /// <summary>Moves time on. True on the frame a new button lands, for its clunk.</summary>
        public bool Tick(float dt)
        {
            if (Done) return false;
            int before = Dropped;
            T += dt;
            bool landed = Dropped > before;
            if (T >= Seconds[(int)Now]) Next();
            return landed;
        }

        /// <summary>A tap: on to the next shot (after <see cref="TapGuard"/>).</summary>
        public void Tap()
        {
            if (!Done && T >= TapGuard) Next();
        }

        void Next()
        {
            Now++;
            T = 0f;
        }

        /// <summary>How many of the new buttons have landed (all of them once the menu's grown).</summary>
        public int Dropped
        {
            get
            {
                if (Now < Shot.MenuGrows) return 0;
                if (Now > Shot.MenuGrows) return NewButtons.Length;
                int n = 0;
                for (int i = 0; i < NewButtons.Length; i++)
                    if (T >= DropAt(i) + DropSeconds) n++;
                return n;
            }
        }

        /// <summary>When new button <paramref name="i"/> starts to drop, in the menu shot.</summary>
        public static float DropAt(int i) => CrackSeconds + i * DropGap;

        /// <summary>How far button <paramref name="i"/> has dropped, 0 (not yet showing) to 1 (in place).</summary>
        public float Drop(int i)
        {
            if (Now < Shot.MenuGrows) return 0f;
            if (Now > Shot.MenuGrows) return 1f;
            float k = (T - DropAt(i)) / DropSeconds;
            return k < 0f ? 0f : k > 1f ? 1f : k;
        }

        /// <summary>The title's old START button still shows (cracking, in the menu shot's first moments).</summary>
        public bool OldStart => Now < Shot.MenuGrows || Now == Shot.MenuGrows && T < CrackSeconds;

        /// <summary>
        /// The title has grown from START to MISSIONS, CO-OP and CREW: once the crew's open, and, while this reveal
        /// plays, once START has cracked (<paramref name="reveal"/> is the reveal playing, or null).
        /// </summary>
        public static bool MenuGrown(bool crewOpen, CrewReveal reveal) => crewOpen && (reveal == null || !reveal.OldStart);

        /// <summary>What kie says now, or null.</summary>
        public string Line => Now == Shot.KieWalksIn && T > 1.2f ? "MISSED ME?" : Now == Shot.KiesLine ? "TWO OF US NOW. BRING A MATE." : null;
    }
}
