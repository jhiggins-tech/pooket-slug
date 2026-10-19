using System;

namespace PooketSlug
{
    /// <summary>
    /// The den's roller door into the tabk bay (<c>docs/design/lobby.md</c>, The tabk bay): reach it and it rolls up, you
    /// step in under it and the bay opens; leave the bay and you step back out under it, and it rolls down behind you.
    /// While it's going either way the den doesn't take walking.
    /// </summary>
    public sealed class RollerDoor
    {
        /// <summary>How long it takes to roll up, to step through and to roll down.</summary>
        public const float UpSeconds = 0.8f, StepSeconds = 0.5f, DownSeconds = 0.7f;

        enum Mode { Shut, GoingIn, Inside, ComingOut }

        Mode m_Mode;
        float m_T;
        bool m_Rolled;

        /// <summary>How far it's rolled up: 0 shut, 1 all the way.</summary>
        public float Lift { get; private set; }

        /// <summary>How far through it you are: 0 in the den, 1 in the bay.</summary>
        public float Through { get; private set; }

        /// <summary>Going in or coming out: the den doesn't take walking.</summary>
        public bool Busy => m_Mode == Mode.GoingIn || m_Mode == Mode.ComingOut;

        /// <summary>You're through, in the bay.</summary>
        public bool Inside => m_Mode == Mode.Inside;

        /// <summary>Shut, with you in the den (the den opening).</summary>
        public void Reset()
        {
            m_Mode = Mode.Shut;
            Lift = Through = 0f;
            m_Rolled = false;
        }

        /// <summary>You've reached it: it rolls up and you step in. Only from shut.</summary>
        public bool GoIn()
        {
            if (m_Mode != Mode.Shut) return false;
            m_Mode = Mode.GoingIn;
            m_T = 0f;
            m_Rolled = true;
            return true;
        }

        /// <summary>You've left the bay: you step out under it, and it rolls down. Only from inside.</summary>
        public bool ComeOut()
        {
            if (m_Mode != Mode.Inside) return false;
            m_Mode = Mode.ComingOut;
            m_T = 0f;
            return true;
        }

        /// <summary>True once, as the door starts rolling (up, or down behind you): its rattle.</summary>
        public bool TakeRolled()
        {
            bool r = m_Rolled;
            m_Rolled = false;
            return r;
        }

        /// <summary>Moves it on; true on the frame you're through into the bay.</summary>
        public bool Tick(float dt)
        {
            switch (m_Mode)
            {
                case Mode.GoingIn:
                    m_T += dt;
                    Lift = Clamp01(m_T / UpSeconds);
                    Through = Clamp01((m_T - UpSeconds) / StepSeconds);
                    if (m_T < UpSeconds + StepSeconds) return false;
                    m_Mode = Mode.Inside;
                    return true;
                case Mode.ComingOut:
                    float was = m_T;
                    m_T += dt;
                    if (was < StepSeconds && m_T >= StepSeconds) m_Rolled = true;
                    Through = 1f - Clamp01(m_T / StepSeconds);
                    Lift = 1f - Clamp01((m_T - StepSeconds) / DownSeconds);
                    if (m_T >= StepSeconds + DownSeconds) m_Mode = Mode.Shut;
                    return false;
                default:
                    return false;
            }
        }

        static float Clamp01(float v) => Math.Max(0f, Math.Min(1f, v));
    }
}
