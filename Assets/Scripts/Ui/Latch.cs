// A button press or a tap, held till the flow takes it: set by a click handler, read once and cleared (screens-tidy,
// Audit 3). Every screen's Take* methods go through these.
namespace PooketSlug
{
    /// <summary>Something pressed since the last <see cref="Take"/>.</summary>
    public struct Latch
    {
        bool m_On;

        public void Set() => m_On = true;

        /// <summary>Whether it was pressed, clearing it.</summary>
        public bool Take()
        {
            bool on = m_On;
            m_On = false;
            return on;
        }

        public void Clear() => m_On = false;
    }

    /// <summary>A tap that carries what was tapped (a crewmate, a place, where to walk), held till it's taken.</summary>
    public struct Latch<T>
    {
        bool m_On;
        T m_Value;

        public void Set(T value)
        {
            m_On = true;
            m_Value = value;
        }

        /// <summary>Whether it was tapped, and what, clearing it.</summary>
        public bool Take(out T value)
        {
            value = m_Value;
            bool on = m_On;
            Clear();
            return on;
        }

        public void Clear()
        {
            m_On = false;
            m_Value = default;
        }
    }
}
