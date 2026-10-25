namespace PooketSlug
{
    /// <summary>
    /// The prompt that shows how to call a tabk in, the first time (<c>docs/design/story.md</c>, The coda: "a prompt
    /// blinks: HOLD DOWN + SP TO CALL YOUR TABK, shown until you call it"). It waits a moment, so it doesn't land on a
    /// banner, then shows until the player starts a call, or until there's no tabk left to call.
    /// </summary>
    public sealed class CallInCoach
    {
        public enum Phase { Waiting, Showing, Done }

        /// <summary>Seconds before it shows, so it doesn't land on the START banner.</summary>
        public float Delay = 2.5f;

        float m_Elapsed;

        public CallInCoach(bool alreadySeen)
        {
            State = alreadySeen ? Phase.Done : Phase.Waiting;
        }

        public Phase State { get; private set; }
        public bool Showing => State == Phase.Showing;

        /// <summary>
        /// One frame. <paramref name="canCall"/>: a tank is left to call; <paramref name="calling"/>: the player's
        /// charging a call. True on the frame it first shows (to mark it seen).
        /// </summary>
        public bool Tick(float dt, bool canCall, bool calling)
        {
            switch (State)
            {
                case Phase.Waiting:
                    if (!canCall) return false;
                    m_Elapsed += dt;
                    if (m_Elapsed < Delay) return false;
                    State = Phase.Showing;
                    return true;
                case Phase.Showing:
                    if (!canCall || calling) State = Phase.Done;
                    return false;
                default:
                    return false;
            }
        }
    }
}
