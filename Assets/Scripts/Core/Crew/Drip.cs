namespace PooketSlug
{
    /// <summary>
    /// Spreads a steady flow over frames: so many drops a second, handed out a whole drop at a time, with what's left
    /// over carried to the next frame (the jet, the spew, their copies on a partner's screen and on tones2's tabk).
    /// </summary>
    public struct Drip
    {
        float m_Due;

        /// <summary>How many drops are due after <paramref name="dt"/> seconds at <paramref name="perSecond"/>.</summary>
        public int Take(float dt, float perSecond)
        {
            m_Due += dt * perSecond;
            int drops = (int)m_Due;
            m_Due -= drops;
            return drops;
        }

        /// <summary>The flow stopped: nothing carries over to the next time it starts.</summary>
        public void Reset() => m_Due = 0f;
    }
}
