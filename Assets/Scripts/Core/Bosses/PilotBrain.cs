using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// What a boss tank's pilot can see this frame (<c>docs/design/tanks.md</c>, The pilot round): itself, its target,
    /// the shots coming at it and its meters. Only what's on screen: the pilot never sees off it, and never reads a
    /// player's input.
    /// </summary>
    public struct PilotView
    {
        public Vector2 Pos, Vel;
        public bool Grounded, OnBridge;
        public int Facing;
        /// <summary>Its health, 0 to 1.</summary>
        public float Health;
        /// <summary>The soldier it's after (its feet), whether there is one in the fight, and whether that one is on screen.</summary>
        public Vector2 Target;
        public bool HasTarget;
        /// <summary>A shot or blast on its way that would reach it soon, and from which side (-1 left, 1 right, 0 above).</summary>
        public bool Threat;
        public int ThreatSide;
        /// <summary>Its hologram (Trollogram) is up, and where it stands.</summary>
        public bool DecoyUp;
        public Vector2 Decoy;
        /// <summary>Its special's charges, and whether its super is charged.</summary>
        public int Specials;
        public bool SuperReady;
        /// <summary>The screen's edges: it keeps inside them.</summary>
        public float Left, Right;
    }

    /// <summary>
    /// A kit's playbook: how the one pilot brain plays that kit (<c>docs/design/tanks.md</c>, A playbook per kit). A
    /// handful of settings, plus the odd rule for a move the buttons alone don't explain (kie's swap).
    /// </summary>
    public sealed class PilotPlaybook
    {
        public string Name;
        /// <summary>The range it likes to fight from (units along the ground).</summary>
        public float NearRange = 5f, FarRange = 9f;
        /// <summary>Held FIRE (a stream) or tapped (each press a shot), and its bursts: firing, then a pause.</summary>
        public bool HoldFire;
        public float BurstTime = 1.2f, PauseTime = 1f;
        /// <summary>Seconds between taps while a tapped burst lasts.</summary>
        public float TapGap = 0.55f;
        /// <summary>Uses its special when the target comes this close (0: never for that reason).</summary>
        public float SpecialWhenClose;
        /// <summary>Uses its super as soon as it's charged.</summary>
        public bool SuperWhenReady = true;
        /// <summary>
        /// kie's line of code: SP again swaps with the hologram. It swaps when the target closes in on the real one
        /// and the hologram stands further off, so the hologram is left standing next to them, waiting to be shot.
        /// </summary>
        public bool SwapsWithDecoy;
        public float SwapWhenClose = 3f, SwapMinGap = 4f;

        /// <summary>kie's (Mission one): Weasel Pop from mid-range and over cover; a Trollogram when you close in, swapping to escape; Steal when charged.</summary>
        public static readonly PilotPlaybook Kie = new PilotPlaybook
        {
            Name = "kie", NearRange = 5f, FarRange = 9f, HoldFire = false, BurstTime = 1.4f, PauseTime = 1.1f, TapGap = 0.6f,
            SpecialWhenClose = 3.5f, SuperWhenReady = true, SwapsWithDecoy = true, SwapWhenClose = 3f, SwapMinGap = 4f,
        };
    }

    /// <summary>The pilot brain's tuning: how human it is, and how it hardens as it's hurt.</summary>
    public sealed class PilotSpec
    {
        /// <summary>Seconds between its decisions (its reaction time) at full health, and at its last.</summary>
        public float Reaction = 0.3f, RagedReaction = 0.16f;
        /// <summary>Chance it notices a shot coming in time to jump it, at full health and at its last.</summary>
        public float Dodge = 0.55f, RagedDodge = 0.85f;
        /// <summary>Its bursts' pauses are this much shorter at its last.</summary>
        public float RagedPause = 0.45f;
        /// <summary>Seconds of warning (a flash and a sound) before a big move lands.</summary>
        public float Tell = 0.5f;
        /// <summary>How much higher than it the target must be before it aims up, or jumps up after them.</summary>
        public float AimUpHeight = 2.2f, ClimbHeight = 1.6f;
        /// <summary>How far it keeps from the screen's edges.</summary>
        public float EdgeMargin = 1.2f;

        /// <summary>0 at full health, 1 at its last: how hard it's raging.</summary>
        public static float Rage(float health) => Mathf.Clamp01(1f - health);

        public float ReactionAt(float health) => Mathf.Lerp(Reaction, RagedReaction, Rage(health));
        public float DodgeAt(float health) => Mathf.Lerp(Dodge, RagedDodge, Rage(health));
        public float PauseAt(float pause, float health) => pause * Mathf.Lerp(1f, RagedPause, Rage(health));
    }

    /// <summary>
    /// The pilot brain: one brain for every pilot, tuned by a kit's <see cref="PilotPlaybook"/>. Each frame it reads
    /// a <see cref="PilotView"/> and writes the <see cref="SoldierInput"/> a player would: it keeps to its range and
    /// faces you, aims the eight ways a player can, jumps your shots when it sees them, follows you up and down,
    /// fires in bursts with pauses, warns before every big move (<see cref="Telling"/>), and gets quicker and bolder
    /// as it's hurt. Decisions come every reaction time, not every frame, so it reads as a person and not an
    /// aimbot.
    /// </summary>
    public sealed class PilotBrain
    {
        public readonly PilotPlaybook Book;
        public readonly PilotSpec Spec;
        readonly Rng m_Rng;

        float m_Think, m_Burst, m_Tap, m_Tell = -1f;
        bool m_Firing;
        int m_Move, m_AimY;
        bool m_Jump, m_Drop;
        PilotMove m_Pending;

        public PilotBrain(PilotPlaybook book, PilotSpec spec, uint seed)
        {
            Book = book;
            Spec = spec;
            m_Rng = new Rng(seed == 0 ? 1u : seed);
            m_Burst = book.PauseTime * 0.5f; // a moment to take you in before the first burst
        }

        /// <summary>A big move is coming: the pilot flashes and a sound plays, this long before it lands (0 to 1 of the tell).</summary>
        public bool Telling => m_Tell >= 0f;

        /// <summary>The buttons it presses this frame.</summary>
        public SoldierInput Tick(float dt, in PilotView v)
        {
            var input = new SoldierInput();
            if (!v.HasTarget)
            {
                m_Firing = false;
                return input; // nobody to fight on screen: it waits
            }

            // A big move's tell runs out, then it presses the button.
            if (m_Tell >= 0f)
            {
                m_Tell += dt;
                if (m_Tell >= Spec.Tell)
                {
                    m_Tell = -1f;
                    if (m_Pending == PilotMove.Special) input.Special = input.SpecialPressed = true;
                    else if (m_Pending == PilotMove.Super) input.Super = input.SuperPressed = true;
                    m_Pending = PilotMove.None;
                }
            }

            m_Think -= dt;
            if (m_Think <= 0f)
            {
                m_Think = Spec.ReactionAt(v.Health) * m_Rng.Range(0.8f, 1.25f);
                Decide(v, ref input);
            }

            // keep to the decisions between them
            int toward = v.Target.x >= v.Pos.x ? 1 : -1;
            int move = m_Move;
            if (move < 0 && v.Pos.x < v.Left + Spec.EdgeMargin) move = 0; // never backs off the screen
            if (move > 0 && v.Pos.x > v.Right - Spec.EdgeMargin) move = 0;
            if (move < 0) input.Left = true;
            else if (move > 0) input.Right = true;
            else if (v.Facing != toward)
            {
                // standing: a tap the target's way turns it to face them
                if (toward < 0) input.Left = true;
                else input.Right = true;
            }
            if (m_AimY > 0) input.Up = true;
            if (m_Drop) input.Down = true;
            if (m_Jump)
            {
                input.Jump = true;
                input.JumpPressed = v.Grounded;
                if (!v.Grounded && v.Vel.y <= 0f) m_Jump = false;
            }

            // fire in bursts with pauses: held, or tapped at its rhythm
            m_Burst -= dt;
            if (m_Burst <= 0f)
            {
                m_Firing = !m_Firing;
                m_Burst = m_Firing ? Book.BurstTime : Spec.PauseAt(Book.PauseTime, v.Health);
                m_Tap = 0f;
            }
            if (m_Firing && !Telling && FacingTarget(v, input))
            {
                if (Book.HoldFire) input.Fire = true;
                else
                {
                    m_Tap -= dt;
                    if (m_Tap <= 0f)
                    {
                        m_Tap = Book.TapGap;
                        input.Fire = input.FirePressed = true;
                    }
                }
            }
            return input;
        }

        static bool FacingTarget(in PilotView v, in SoldierInput input)
        {
            int facing = input.Right ? 1 : input.Left ? -1 : v.Facing;
            return (v.Target.x - v.Pos.x) * facing >= -0.3f;
        }

        void Decide(in PilotView v, ref SoldierInput input)
        {
            float dx = v.Target.x - v.Pos.x, dy = v.Target.y - v.Pos.y, dist = Mathf.Abs(dx);
            int toward = dx >= 0f ? 1 : -1;

            // keep to its range: back off when too close, close in when too far, otherwise drift a little
            if (dist < Book.NearRange) m_Move = -toward;
            else if (dist > Book.FarRange) m_Move = toward;
            else m_Move = m_Rng.Value < 0.3f ? (m_Rng.Value < 0.5f ? -1 : 1) : 0;

            // aim: up at a target well above, else forward (a little aim error: sometimes it's slow to look up)
            m_AimY = dy > Spec.AimUpHeight && m_Rng.Value < 0.85f ? 1 : 0;

            // follow them between levels: jump up after them, or drop through a bridge after them
            m_Drop = false;
            if (v.Grounded && dy > Spec.ClimbHeight && dist < Book.FarRange) m_Jump = true;
            else if (v.Grounded && dy < -Spec.ClimbHeight && v.OnBridge)
            {
                m_Drop = true;
                m_Jump = true;
            }

            // jump shots it sees coming
            if (v.Threat && v.Grounded && m_Rng.Value < Spec.DodgeAt(v.Health))
            {
                m_Jump = true;
                m_Drop = false;
                if (v.ThreatSide != 0) m_Move = -v.ThreatSide; // and away from it
            }

            if (Telling) return; // one big move at a time

            // the big moves, each with a tell
            if (Book.SuperWhenReady && v.SuperReady)
            {
                StartTell(PilotMove.Super);
                return;
            }
            if (Book.SwapsWithDecoy && v.DecoyUp)
            {
                // kie: swap when you close in on the real one and the hologram stands further off
                float decoyGap = Mathf.Abs(v.Decoy.x - v.Target.x);
                if (dist < Book.SwapWhenClose && decoyGap > Book.SwapMinGap) StartTell(PilotMove.Special);
                return;
            }
            if (Book.SpecialWhenClose > 0f && dist < Book.SpecialWhenClose && v.Specials > 0) StartTell(PilotMove.Special);
        }

        void StartTell(PilotMove move)
        {
            m_Pending = move;
            m_Tell = 0f;
        }
    }

    /// <summary>A big move the pilot is about to make, after its tell.</summary>
    public enum PilotMove { None, Special, Super }
}
