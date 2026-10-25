using System.Collections.Generic;
using System.Globalization;

namespace PooketSlug
{
    /// <summary>One card on the mission screen: a mission and how the player stands on it, or the one still to come.</summary>
    public sealed class MissionCard
    {
        /// <summary>The mission, or null for the card of the next one, which isn't in the game yet.</summary>
        public MissionDef Mission;
        /// <summary>Its number in the story, from 1.</summary>
        public int Number;
        /// <summary>Played alone only: Mission one, the prelude (<c>docs/design/levels.md</c>).</summary>
        public bool SoloOnly;
        /// <summary>Best score and times cleared.</summary>
        public int Best, Clears;

        public bool ComingSoon => Mission == null;
        public bool Cleared => Clears > 0;
    }

    /// <summary>
    /// The mission screen (<c>docs/design/levels.md</c>, Missions; opened from the den's mission
    /// board): every mission the player has reached, to play again for score, XP, rank and stats, with their best score
    /// and clears, then a card for the next mission. A mission is reached once the one before it is cleared, so the board
    /// grows one mission at a time and never shows one further on. There's no hard setting: each mission is harder than
    /// the one before it (levels.md, Difficulty).
    /// </summary>
    public static class MissionBoard
    {
        public static bool SoloOnly(MissionDef mission) => mission.Story.SoloOnly;

        /// <summary>
        /// The HUD's banner as a mission starts. A co-op room never plays a solo-only mission (<see cref="Missions.ForCoop"/>),
        /// so should one start in co-op the banner doesn't name it.
        /// </summary>
        public static string StartBanner(MissionDef mission, bool coop) => coop && SoloOnly(mission) ? "START!" : mission.Name + "  START!";

        /// <summary>Has <paramref name="profile"/> cleared <paramref name="mission"/>?</summary>
        public static bool Cleared(Profile profile, MissionDef mission) =>
            profile != null && mission != null && profile.Clears(mission) > 0;

        /// <summary>The cards, in story order: the missions reached, then the next one, coming soon if it isn't in yet.</summary>
        public static List<MissionCard> Cards(Profile profile)
        {
            var cards = new List<MissionCard>();
            for (int i = 0; i < Missions.All.Count; i++)
            {
                MissionDef m = Missions.All[i];
                cards.Add(new MissionCard
                {
                    Mission = m,
                    Number = i + 1,
                    SoloOnly = SoloOnly(m),
                    Best = profile.Best(m),
                    Clears = profile.Clears(m),
                });
                if (!cards[i].Cleared) return cards; // the next one isn't reached yet
            }
            cards.Add(new MissionCard { Number = Missions.All.Count + 1 });
            return cards;
        }

        /// <summary>The card's record line, e.g. "BEST 12,400 · CLEARED 3 TIMES".</summary>
        public static string RecordLine(MissionCard card)
        {
            if (card.ComingSoon) return "COMING SOON";
            int best = card.Best;
            int clears = card.Clears;
            if (clears == 0) return best > 0 ? "BEST " + best.ToString("N0", CultureInfo.InvariantCulture) + " · NOT CLEARED YET" : "NOT CLEARED YET";
            string cleared = "CLEARED " + (clears == 1 ? "ONCE" : clears + " TIMES");
            return best > 0 ? "BEST " + best.ToString("N0", CultureInfo.InvariantCulture) + " · " + cleared : cleared;
        }
    }
}
