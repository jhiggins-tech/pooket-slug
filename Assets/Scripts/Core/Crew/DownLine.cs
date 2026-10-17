using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// What the HUD says about a player's lives while they're down (<c>docs/design/player.md</c>, Health and lives):
    /// when they're back in, the last life going, the continue on offer, or that they're out and waiting on a
    /// partner's 1-up in co-op. Both lines are null when there's nothing to say.
    /// </summary>
    public readonly struct DownLine
    {
        /// <summary>The big line ("DOWN!  BACK IN 2"), or null.</summary>
        public readonly string Down;
        /// <summary>The small line under it, or null.</summary>
        public readonly string Note;

        DownLine(string down, string note)
        {
            Down = down;
            Note = note;
        }

        /// <summary>
        /// The lines for a player with <paramref name="lives"/>: <paramref name="dead"/> for
        /// <paramref name="deadTime"/> seconds, in <paramref name="coop"/> or not, playing by <paramref name="touch"/>.
        /// </summary>
        public static DownLine For(LifeBank lives, bool dead, float deadTime, bool coop, bool touch)
        {
            if (dead && !lives.OutOfLives)
            {
                int back = Mathf.Max(1, Mathf.CeilToInt(Tuning.Lives.RespawnDelay - deadTime));
                bool last = lives.Lives == 0 && deadTime < Tuning.Lives.RespawnDelay;
                return new DownLine("DOWN!  BACK IN " + back, last ? "NO LIVES LEFT AFTER THIS ONE" : null);
            }
            if (lives.Offering)
            {
                string press = touch ? "TAP FIRE" : "PRESS FIRE";
                return new DownLine("CONTINUE?  " + Mathf.CeilToInt(lives.OfferLeft),
                    press + " TO CONTINUE  ·  " + lives.Continues + (lives.Continues == 1 ? " CONTINUE LEFT" : " CONTINUES LEFT"));
            }
            if (lives.IsOut && coop) return new DownLine("OUT!", "YOUR PARTNER'S NEXT 1UP BRINGS YOU BACK");
            return default;
        }
    }
}
