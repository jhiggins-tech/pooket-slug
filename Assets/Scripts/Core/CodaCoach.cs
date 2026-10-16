namespace PooketSlug
{
    /// <summary>
    /// The coda's prompts, the first time (<c>docs/design/story.md</c>, The coda): they teach the tabk one thing at a
    /// time, DRIVE, then AIM at the gantry, FIRE at the watchtower and SP over the sandbag wall, each shown only until
    /// it's been done once. A prompt shows only while you're in the tabk, and from where its part of the road is in
    /// view; one you've driven well past without doing is dropped, so they never pile up.
    /// </summary>
    public sealed class CodaCoach
    {
        public enum Step { Drive, Aim, Fire, Special, Done }

        public const string DrivePrompt = "DRIVE!", AimPrompt = "AIM UP AT THE GANTRY!",
            FirePrompt = "FIRE! SHOOT THE TOWER DOWN. ROUNDS NEVER REFILL", SpecialPrompt = "HOLD UP-RIGHT AND SP: MUD JUMP THE WALL!";

        /// <summary>How far past a step's spot you can get before its prompt is dropped.</summary>
        public const float Overshoot = 10f;

        readonly float m_AimX, m_FireX, m_SpecialX;

        /// <param name="aimX">Where AIM shows from (the gantry in view).</param>
        /// <param name="fireX">Where FIRE shows from (the watchtower in view).</param>
        /// <param name="specialX">Where SP shows from (the sandbag wall in view).</param>
        public CodaCoach(float aimX, float fireX, float specialX)
        {
            m_AimX = aimX;
            m_FireX = fireX;
            m_SpecialX = specialX;
        }

        public Step At { get; private set; }

        /// <summary>The prompt to show now, or null.</summary>
        public string Prompt { get; private set; }

        /// <summary>
        /// One frame. <paramref name="driving"/>: in the tabk; <paramref name="x"/>: where it is; and what's been done
        /// with it this frame (or ever: these only need to be true once).
        /// </summary>
        public void Tick(bool driving, float x, bool moved, bool aimedUp, bool fired, bool special)
        {
            if (At == Step.Drive && driving && moved) At = Step.Aim;
            if (At == Step.Aim && (aimedUp && x >= m_AimX || x > m_AimX + Overshoot)) At = Step.Fire;
            if (At == Step.Fire && (fired && x >= m_FireX || x > m_FireX + Overshoot)) At = Step.Special;
            if (At == Step.Special && (special && x >= m_SpecialX || x > m_SpecialX + Overshoot)) At = Step.Done;
            Prompt = !driving ? null : At switch
            {
                Step.Drive => DrivePrompt,
                Step.Aim when x >= m_AimX => AimPrompt,
                Step.Fire when x >= m_FireX => FirePrompt,
                Step.Special when x >= m_SpecialX => SpecialPrompt,
                _ => null,
            };
        }
    }
}
