using System;

namespace PooketSlug
{
    /// <summary>
    /// A house door that swings, like the kitchen's back door to the yard (<c>docs/design/lobby.md</c>, The outer
    /// rooms): reach it and it creaks open, you step into its dark, and the next room takes over; there the door swings
    /// shut behind you. While it's going either way the room doesn't take walking. A sibling of <see cref="RollerDoor"/>,
    /// which rolls: this one swings on its hinge.
    /// </summary>
    public sealed class SwingDoor
    {
        /// <summary>How long it takes to swing open, to step through its dark, and to swing shut behind you.</summary>
        public const float SwingSeconds = 0.4f, StepSeconds = 0.5f, ShutSeconds = 0.5f;

        enum Mode { Shut, Opening, Going, Shutting }

        Mode m_Mode;
        float m_T;
        bool m_Swung, m_Landed;

        /// <summary>How far it's swung open: 0 shut, 1 all the way.</summary>
        public float Swing { get; private set; }

        /// <summary>How far into its dark you are: 0 in the room, 1 through.</summary>
        public float Through { get; private set; }

        /// <summary>Swinging, stepping or shutting: the room doesn't take walking.</summary>
        public bool Busy => m_Mode != Mode.Shut;

        /// <summary>Shut, with you in the room (the room opening).</summary>
        public void Reset()
        {
            m_Mode = Mode.Shut;
            Swing = Through = 0f;
            m_Swung = m_Landed = false;
        }

        /// <summary>You've reached it: it creaks open and you step in. Only from shut.</summary>
        public bool GoIn()
        {
            if (m_Mode != Mode.Shut) return false;
            m_Mode = Mode.Opening;
            m_T = 0f;
            m_Swung = true;
            return true;
        }

        /// <summary>True once, as it starts to open: its creak.</summary>
        public bool TakeSwung()
        {
            bool s = m_Swung;
            m_Swung = false;
            return s;
        }

        /// <summary>True once, as it lands shut behind you: its thump.</summary>
        public bool TakeLanded()
        {
            bool l = m_Landed;
            m_Landed = false;
            return l;
        }

        /// <summary>Moves it on; true on the frame you're through into the next room.</summary>
        public bool Tick(float dt)
        {
            switch (m_Mode)
            {
                case Mode.Opening:
                    m_T += dt;
                    Swing = Clamp01(m_T / SwingSeconds);
                    if (m_T < SwingSeconds) return false;
                    m_Mode = Mode.Going;
                    return false;
                case Mode.Going:
                    m_T += dt;
                    Through = Clamp01((m_T - SwingSeconds) / StepSeconds);
                    if (m_T < SwingSeconds + StepSeconds) return false;
                    m_Mode = Mode.Shutting;
                    m_T = 0f;
                    return true;
                case Mode.Shutting:
                    m_T += dt;
                    Through = 0f;
                    Swing = 1f - Clamp01(m_T / ShutSeconds);
                    if (m_T < ShutSeconds) return false;
                    m_Mode = Mode.Shut;
                    Swing = 0f;
                    m_Landed = true;
                    return false;
                default:
                    return false;
            }
        }

        static float Clamp01(float v) => Math.Max(0f, Math.Min(1f, v));
    }
}
