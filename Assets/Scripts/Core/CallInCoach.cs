namespace PooketSlug
{
    /// <summary>
    /// The one-time hint that shows how to call a tank in (<c>docs/design/tanks.md</c>, Discovering tabks: "the game
    /// shows how once, at a calm moment"). It waits until the mission's been going a little while and no goon has been
    /// close for a moment (goons keep coming, so if no calm moment comes it shows anyway, a while later), shows for a
    /// few seconds, and goes once the player starts a call (or after its time). A tank no longer to call (called, or
    /// none this mission) ends it.
    /// </summary>
    public sealed class CallInCoach
    {
        public enum Phase { Waiting, Showing, Done }

        /// <summary>Seconds into the mission before it can show, so it doesn't land on the START banner.</summary>
        public float Delay = 4f;
        /// <summary>Seconds of calm it waits for: no goon close, the soldier on the ground and on foot.</summary>
        public float CalmFor = 1.5f;
        /// <summary>How close a goon can be to the soldier for a moment to be calm, in units.</summary>
        public float CalmRadius = 6f;
        /// <summary>Seconds into the mission when it shows anyway, calm or not.</summary>
        public float Latest = 20f;
        /// <summary>Seconds it stays up, unless the player calls first.</summary>
        public float ShowFor = 6f;

        float m_Elapsed, m_Calm, m_Shown;

        public CallInCoach(bool alreadySeen)
        {
            State = alreadySeen ? Phase.Done : Phase.Waiting;
        }

        public Phase State { get; private set; }
        public bool Showing => State == Phase.Showing;

        /// <summary>
        /// One frame. <paramref name="canCall"/>: a tank is left to call; <paramref name="calm"/>: a calm moment;
        /// <paramref name="calling"/>: the player's charging a call. True on the frame it first shows (to mark it seen).
        /// </summary>
        public bool Tick(float dt, bool canCall, bool calm, bool calling)
        {
            switch (State)
            {
                case Phase.Waiting:
                    if (!canCall) return false; // it may come back: a later mission's call-in
                    m_Elapsed += dt;
                    m_Calm = calm ? m_Calm + dt : 0f;
                    if (m_Elapsed < Delay || (m_Calm < CalmFor && m_Elapsed < Latest)) return false;
                    State = Phase.Showing;
                    m_Shown = 0f;
                    return true;
                case Phase.Showing:
                    m_Shown += dt;
                    if (!canCall || calling || m_Shown >= ShowFor) State = Phase.Done;
                    return false;
                default:
                    return false;
            }
        }
    }
}
