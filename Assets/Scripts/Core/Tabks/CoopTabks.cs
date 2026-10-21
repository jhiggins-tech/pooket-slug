using System.Collections.Generic;

namespace PooketSlug
{
    /// <summary>
    /// One of each tabk in co-op (<c>docs/design/lobby.md</c>, The tabk bay; owner, 2026-10-21, unique-crew-pick). Each
    /// player loads a tabk in their own bay, and the one a partner has loaded is taken in yours: dimmed in its space with
    /// their tag, its card saying TAKEN. If you both have the same one loaded, it goes the couch's way: whoever's in the
    /// mission keeps it, else the host, and the other player's drone swaps in their next won tabk, in bay order. It's
    /// the den's rule for crewmates (<see cref="Den"/>), for tabks.
    /// </summary>
    public static class CoopTabks
    {
        /// <summary>The tabk <paramref name="partner"/> has loaded, while they're in the den or the mission; else null.</summary>
        public static TabkCard Taken(DenPartner partner) =>
            partner.Playing || partner.InDenNow ? TabkCards.ById(partner.Tabk) : null;

        /// <summary>
        /// Whether we give <paramref name="loaded"/> up to <paramref name="partner"/>: they have it too, and they're in
        /// the mission, or in the den with us and the host (<paramref name="authority"/> false: we aren't).
        /// </summary>
        public static bool GivesWay(TabkCard loaded, DenPartner partner, bool authority) =>
            loaded != null && Taken(partner) == loaded && (partner.Playing || !authority);

        /// <summary>
        /// The tabk the drone swaps in when we give <paramref name="loaded"/> up: the next of <paramref name="won"/> after
        /// it in bay order (its parking spaces, floor then top deck) that isn't <paramref name="taken"/>, round to the
        /// start. Null when there's none (never in co-op: every player there has beaten Mission one, so has two).
        /// </summary>
        public static TabkCard Next(TabkCard loaded, IReadOnlyList<TabkCard> won, TabkCard taken)
        {
            var order = new List<TabkCard>(won);
            order.Sort((a, b) => a.Home.CompareTo(b.Home));
            int from = order.IndexOf(loaded) + 1; // 0 when it isn't one of them
            for (int k = 0; k < order.Count; k++)
            {
                TabkCard c = order[(from + k) % order.Count];
                if (c != taken && c != loaded) return c;
            }
            return null;
        }
    }
}
