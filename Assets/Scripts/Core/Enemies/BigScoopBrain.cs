using UnityEngine;

namespace PooketSlug
{
    /// <summary>The Big Scoop's three moves.</summary>
    public enum ScoopMove : byte { None, Slam, Hurl, Charge }

    /// <summary>
    /// The Big Scoop (<c>docs/design/levels.md</c>, Mission two): the sign yard's miniboss, an oversized Trollogram heavy
    /// with a giant scoop. Every move is telegraphed: he winds up, then acts, then rests. The slam sends a shockwave
    /// along the floor both ways (jump it); the hurl lobs a scoop where you're standing (move); the charge runs him
    /// across the yard (jump over him). A nitrogen tank's freeze opens a damage window: frozen, he takes double. This
    /// is his brain, plain and tested; <c>Enemies/BigScoop.cs</c> plays it.
    /// </summary>
    public sealed class BigScoopSpec
    {
        public int Hp = 40, Score = 4000;
        /// <summary>The wind-up before each move: the tell.</summary>
        public float SlamTell = 0.8f, HurlTell = 0.6f, ChargeTell = 0.75f;
        /// <summary>How long each move lasts once it goes, and the rest after.</summary>
        public float SlamTime = 0.3f, HurlTime = 0.3f, ChargeTime = 1.5f, Rest = 1.1f;
        /// <summary>Walking between moves; charging.</summary>
        public float WalkSpeed = 1.4f, ChargeSpeed = 9f;
        /// <summary>The slam's shockwave: how fast it runs along the floor (how far is its projectile's life, <see cref="Content.ScoopWave"/>).</summary>
        public float WaveSpeed = 8f;
        /// <summary>A hurled scoop's time in the air.</summary>
        public float HurlFlight = 0.95f;
        /// <summary>Close enough to slam; far enough to charge.</summary>
        public float SlamRange = 4f, ChargeRange = 6f;
        /// <summary>Frozen by a nitrogen tank, every hit counts this many times: the damage window.</summary>
        public int FrozenDamage = 2;
        /// <summary>He's angry once he's down to this share of his health: shorter rests.</summary>
        public float AngryAt = 0.5f, AngryRest = 0.6f;

        /// <summary>His next move, for a soldier <paramref name="dx"/> away, never the same one three times running.</summary>
        public ScoopMove Pick(float dx, ScoopMove last, ScoopMove beforeLast, Rng rng)
        {
            float d = Mathf.Abs(dx);
            ScoopMove want;
            if (d <= SlamRange) want = rng.Range(0, 4) == 0 ? ScoopMove.Hurl : ScoopMove.Slam;
            else if (d >= ChargeRange) want = rng.Range(0, 2) == 0 ? ScoopMove.Charge : ScoopMove.Hurl;
            else want = rng.Range(0, 3) == 0 ? ScoopMove.Slam : ScoopMove.Hurl;
            if (want == last && want == beforeLast) want = want == ScoopMove.Hurl ? (d <= SlamRange ? ScoopMove.Slam : ScoopMove.Charge) : ScoopMove.Hurl;
            return want;
        }

        public float TellFor(ScoopMove m) => m == ScoopMove.Slam ? SlamTell : m == ScoopMove.Hurl ? HurlTell : m == ScoopMove.Charge ? ChargeTell : 0f;
        public float TimeFor(ScoopMove m) => m == ScoopMove.Slam ? SlamTime : m == ScoopMove.Hurl ? HurlTime : m == ScoopMove.Charge ? ChargeTime : 0f;
        public float RestFor(float health) => health <= AngryAt ? AngryRest : Rest;
    }

    /// <summary>What the Big Scoop's doing.</summary>
    public enum ScoopPhase : byte { Rest, Tell, Act }

    /// <summary>The Big Scoop's moves over time: rest, pick a move, wind up (the tell), act, rest again.</summary>
    public sealed class BigScoopBrain
    {
        readonly BigScoopSpec m_Spec;
        ScoopMove m_Last, m_BeforeLast;

        public ScoopPhase Phase { get; private set; } = ScoopPhase.Rest;
        public ScoopMove Move { get; private set; }
        /// <summary>Seconds into this phase.</summary>
        public float T { get; private set; }
        float m_Rest = 1.2f;

        public BigScoopBrain(BigScoopSpec spec) => m_Spec = spec;

        /// <summary>
        /// A step of <paramref name="dt"/>, with the soldier he's after <paramref name="dx"/> away and him at
        /// <paramref name="health"/> (0 to 1). True on the frame a move goes off (after its tell).
        /// </summary>
        public bool Tick(float dt, float dx, float health, Rng rng)
        {
            T += dt;
            switch (Phase)
            {
                case ScoopPhase.Rest:
                    if (T < m_Rest) return false;
                    Move = m_Spec.Pick(dx, m_Last, m_BeforeLast, rng);
                    m_BeforeLast = m_Last;
                    m_Last = Move;
                    Enter(ScoopPhase.Tell);
                    return false;
                case ScoopPhase.Tell:
                    if (T < m_Spec.TellFor(Move)) return false;
                    Enter(ScoopPhase.Act);
                    return true;
                default:
                    if (T < m_Spec.TimeFor(Move)) return false;
                    m_Rest = m_Spec.RestFor(health);
                    Move = ScoopMove.None;
                    Enter(ScoopPhase.Rest);
                    return false;
            }
        }

        /// <summary>The charge ran into a wall or the edge of the yard: it's over.</summary>
        public void StopCharge()
        {
            if (Move == ScoopMove.Charge && Phase == ScoopPhase.Act) T = m_Spec.ChargeTime;
        }

        void Enter(ScoopPhase p)
        {
            Phase = p;
            T = 0f;
        }
    }
}
