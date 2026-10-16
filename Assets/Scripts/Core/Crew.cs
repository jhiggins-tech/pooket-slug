using System.Collections.Generic;

namespace PooketSlug
{
    /// <summary>
    /// Who you play (<c>docs/design/levels.md</c>, The order things open up). Everyone is tones2 until Mission one is
    /// beaten solo; then the crew opens, with kie (freed from his tank), and the title's CREW button picks between
    /// them. The pick is kept with the settings (<see cref="Options.Crew"/>). Until the co-op lobby, two partners may
    /// play the same soldier.
    /// </summary>
    public static class Crew
    {
        /// <summary>The soldiers you can pick once the crew's open, in the order CREW cycles through them.</summary>
        public static readonly IReadOnlyList<SoldierDef> Freed = new[] { Content.Tones2, Content.Kie };

        /// <summary>The soldier to play: the pick, if the crew's open and it's one of the freed, else tones2.</summary>
        public static SoldierDef Pick(int soldierId, bool open)
        {
            if (!open) return Content.Tones2;
            foreach (SoldierDef s in Freed)
                if (s.Id == soldierId) return s;
            return Content.Tones2;
        }

        /// <summary>The next soldier CREW picks after <paramref name="soldierId"/>.</summary>
        public static SoldierDef Next(int soldierId)
        {
            for (int i = 0; i < Freed.Count; i++)
                if (Freed[i].Id == soldierId) return Freed[(i + 1) % Freed.Count];
            return Freed[0];
        }
    }
}
