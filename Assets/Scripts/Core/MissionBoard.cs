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
        /// <summary>Its hard setting is open: it's from Mission two on, and it's been cleared.</summary>
        public bool HardOpen;
        /// <summary>Best score and times cleared, at each setting.</summary>
        public int Best, Clears, HardBest, HardClears;

        public bool ComingSoon => Mission == null;
        public bool Cleared => Clears > 0 || HardClears > 0;
    }

    /// <summary>
    /// The mission screen (<c>docs/design/levels.md</c>, Missions; opened from MISSIONS on the title and the den's mission
    /// board): every mission the player has reached, to play again for score, XP, rank and stats, with their best score
    /// and clears, then a card for the next mission. A mission is reached once the one before it is cleared, so the board
    /// grows one mission at a time and never shows one further on. Hard is a second setting for each cleared mission from
    /// Mission two on, tuned for two players, so it's only offered in co-op.
    /// </summary>
    public static class MissionBoard
    {
        public static bool SoloOnly(MissionDef mission) => mission == Missions.One;

        /// <summary>Hard can be picked for <paramref name="mission"/>: cleared, from Mission two on, and played in co-op.</summary>
        public static bool HardOpen(Profile profile, MissionDef mission, bool coop) =>
            coop && mission != null && !SoloOnly(mission) && profile.Clears(mission, Setting.Normal) + profile.Clears(mission, Setting.Hard) > 0;

        /// <summary>The cards, in story order: the missions reached, then the next one, coming soon if it isn't in yet.</summary>
        public static List<MissionCard> Cards(Profile profile, bool coop)
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
                    HardOpen = HardOpen(profile, m, coop),
                    Best = profile.Best(m, Setting.Normal),
                    Clears = profile.Clears(m, Setting.Normal),
                    HardBest = profile.Best(m, Setting.Hard),
                    HardClears = profile.Clears(m, Setting.Hard),
                });
                if (!cards[i].Cleared) return cards; // the next one isn't reached yet
            }
            cards.Add(new MissionCard { Number = Missions.All.Count + 1 });
            return cards;
        }

        /// <summary>The setting a pick plays at: hard only where it's open.</summary>
        public static Setting SettingFor(MissionCard card, bool wantHard) => wantHard && card.HardOpen ? Setting.Hard : Setting.Normal;

        /// <summary>The card's record line, e.g. "BEST 12,400 · CLEARED 3 TIMES".</summary>
        public static string RecordLine(MissionCard card, Setting setting)
        {
            if (card.ComingSoon) return "COMING SOON";
            int best = setting == Setting.Hard ? card.HardBest : card.Best;
            int clears = setting == Setting.Hard ? card.HardClears : card.Clears;
            if (clears == 0) return best > 0 ? "BEST " + best.ToString("N0", CultureInfo.InvariantCulture) + " · NOT CLEARED YET" : "NOT CLEARED YET";
            string cleared = "CLEARED " + (clears == 1 ? "ONCE" : clears + " TIMES");
            return best > 0 ? "BEST " + best.ToString("N0", CultureInfo.InvariantCulture) + " · " + cleared : cleared;
        }
    }
}
