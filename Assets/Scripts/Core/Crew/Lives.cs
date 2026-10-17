using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// One player's lives and continues (<c>docs/design/player.md</c>, Health and lives). A fall spends a spare
    /// life; with none left the player is offered a continue for a few seconds, which refills the spare lives;
    /// with none of those left, or the offer let go, they're out. A 1-up adds a spare life, or brings an out
    /// player back in.
    /// </summary>
    public sealed class LifeBank
    {
        public enum State { Playing, Offering, Out }
        public enum Fall { Respawn, OfferContinue, Out }

        /// <summary>Lives left after this one.</summary>
        public int Lives { get; private set; }
        public int Continues { get; private set; }
        public State Now { get; private set; }
        /// <summary>Seconds left to take the continue, while it's offered.</summary>
        public float OfferLeft { get; private set; }

        readonly int m_StartLives, m_MaxLives;
        readonly float m_OfferTime;

        public LifeBank(int startLives, int continues, int maxLives, float offerTime)
        {
            Lives = m_StartLives = startLives;
            Continues = continues;
            m_MaxLives = maxLives;
            m_OfferTime = offerTime;
        }

        public static LifeBank Standard() =>
            new LifeBank(Tuning.Lives.Spare, Tuning.Lives.Continues, Tuning.Lives.MaxSpare, Tuning.Lives.ContinueSeconds);

        /// <summary>Out of lives: offered a continue, or out. A partner's 1-up goes to such a player first.</summary>
        public bool OutOfLives => Now != State.Playing;
        public bool IsOut => Now == State.Out;
        public bool Offering => Now == State.Offering;

        /// <summary>The soldier has fallen: what happens next.</summary>
        public Fall Fallen()
        {
            if (Now != State.Playing) return Now == State.Out ? Fall.Out : Fall.OfferContinue;
            if (Lives > 0)
            {
                Lives--;
                return Fall.Respawn;
            }
            if (Continues > 0)
            {
                Now = State.Offering;
                OfferLeft = m_OfferTime;
                return Fall.OfferContinue;
            }
            Now = State.Out;
            return Fall.Out;
        }

        /// <summary>Out for good, with no offer: someone watching a room has no soldier in play (rooms).</summary>
        public void Retire()
        {
            Lives = 0;
            Continues = 0;
            OfferLeft = 0f;
            Now = State.Out;
        }

        /// <summary>Counts the offer down. True on the frame it runs out.</summary>
        public bool Tick(float dt)
        {
            if (Now != State.Offering) return false;
            OfferLeft -= dt;
            if (OfferLeft > 0f) return false;
            OfferLeft = 0f;
            Now = State.Out;
            return true;
        }

        /// <summary>Takes the offered continue: the spare lives refill. False if none is offered.</summary>
        public bool TakeContinue()
        {
            if (Now != State.Offering) return false;
            Continues--;
            Lives = m_StartLives;
            Now = State.Playing;
            return true;
        }

        /// <summary>Lets the offered continue go.</summary>
        public void Decline()
        {
            if (Now == State.Offering) Now = State.Out;
        }

        /// <summary>A 1-up. True if it brought the player back in (they were out of lives).</summary>
        public bool AddLife()
        {
            if (Now != State.Playing)
            {
                Now = State.Playing; // back in on this life, with no spare
                OfferLeft = 0f;
                return true;
            }
            Lives = Mathf.Min(m_MaxLives, Lives + 1);
            return false;
        }
    }
}
