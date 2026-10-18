namespace PooketSlug
{
    /// <summary>
    /// Frozen solid for a moment (Mission two's nitrogen: its vents and its tanks), soldiers and goons alike: while
    /// frozen they can't move or fire of their own accord, though gravity and a belt still carry them. Once they thaw,
    /// a short grace keeps them from freezing again straight away, so a soldier can always walk out of a vent.
    /// </summary>
    public struct Frost
    {
        float m_Left, m_Grace, m_GraceAfter;

        public bool Frozen => m_Left > 0f;
        /// <summary>Seconds left frozen.</summary>
        public float Left => m_Left;
        /// <summary>Just thawed: nothing freezes them for a moment.</summary>
        public bool Thawing => !Frozen && m_Grace > 0f;

        /// <summary>Freezes for <paramref name="seconds"/>, then <paramref name="grace"/> seconds safe; false if already frozen or thawing.</summary>
        public bool Freeze(float seconds, float grace)
        {
            if (Frozen || m_Grace > 0f || seconds <= 0f) return false;
            m_Left = seconds;
            m_GraceAfter = grace;
            return true;
        }

        public void Tick(float dt)
        {
            if (m_Left > 0f)
            {
                m_Left -= dt;
                if (m_Left <= 0f)
                {
                    m_Left = 0f;
                    m_Grace = m_GraceAfter;
                }
                return;
            }
            if (m_Grace > 0f) m_Grace -= dt;
        }

        /// <summary>Thawed at once (down, respawned, or a new life).</summary>
        public void Clear()
        {
            m_Left = 0f;
            m_Grace = 0f;
        }
    }
}
