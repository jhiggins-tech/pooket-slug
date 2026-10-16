namespace PooketSlug
{
    /// <summary>
    /// Rooms: the shared world runs on the room's master client. A host whose phone goes to the background stops
    /// running, but Photon only notices when it times them out, and until then the partner's world stands still. This
    /// notices sooner, from the master's beat (<see cref="PartnerBeacon.BeatAt"/>, sent four times a second), and says
    /// when to ask Photon to make us the master instead. Photon only agrees once the master's connection has been idle
    /// for its own threshold (<c>MasterClientRequestReassignThreshold</c> in the project's Fusion config), so asking
    /// too early does no harm.
    /// </summary>
    public sealed class MasterWatch
    {
        /// <summary>Seconds the master's beat stays the same before we ask: Photon's threshold.</summary>
        public const float QuietAfter = 2f;
        /// <summary>Seconds between asks while the master stays quiet.</summary>
        public const float RetryEvery = 2f;
        /// <summary>The longest frame counted, so a frame of our own that stalls (loading) doesn't read as the master gone quiet.</summary>
        public const float LongestFrame = 0.5f;

        int m_Master = int.MinValue, m_Beat = int.MinValue;
        float m_Quiet, m_SinceAsked;

        /// <summary>Seconds the master's beat has stayed the same.</summary>
        public float Quiet => m_Quiet;

        /// <summary>
        /// Every frame: who the master is, their beat (-1 when we can't see it), and whether we may take over (a
        /// player in the session who isn't the master). True when it's time to ask.
        /// </summary>
        public bool Tick(float dt, int master, int beat, bool canTakeOver)
        {
            if (master != m_Master || beat != m_Beat)
            {
                m_Master = master;
                m_Beat = beat;
                m_Quiet = 0f;
                m_SinceAsked = RetryEvery;
                return false;
            }
            if (dt > LongestFrame) dt = LongestFrame;
            m_Quiet += dt;
            m_SinceAsked += dt;
            if (!canTakeOver || m_Quiet < QuietAfter || m_SinceAsked < RetryEvery) return false;
            m_SinceAsked = 0f;
            return true;
        }
    }
}
