using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// A pair's team super (<c>docs/design/team-supers.md</c>): its name, the two soldiers it's for, and its number on
    /// the wire.
    /// </summary>
    public sealed class TeamCombo
    {
        public readonly int Id;
        public readonly string Name;
        public readonly SoldierDef A, B;

        public TeamCombo(int id, string name, SoldierDef a, SoldierDef b)
        {
            Id = id;
            Name = name;
            A = a;
            B = b;
        }

        public bool Pairs(SoldierDef x, SoldierDef y) => (x == A && y == B) || (x == B && y == A);

        /// <summary>The other one of the pair, or null if <paramref name="x"/> isn't in it.</summary>
        public SoldierDef Other(SoldierDef x) => x == A ? B : x == B ? A : null;
    }

    /// <summary>
    /// Team supers in co-op (<c>docs/design/team-supers.md</c>): when both supers are charged and the pair has a combo
    /// built, a TEAM button shows on both players' pads. Either player fires it, it spends both supers, and after a
    /// short show every enemy on screen dies (a boss takes a big hit instead). Only built combos are here.
    /// </summary>
    public static class TeamSupers
    {
        /// <summary>tones2 + kie: tones2 floods the floor with sludge and kie's weasels surf the wave across the screen.</summary>
        public static readonly TeamCombo SewerRats = new TeamCombo(1, "SEWER RATS", Content.Tones2, Content.Kie);

        public static readonly IReadOnlyList<TeamCombo> All = new[] { SewerRats };

        public static readonly TeamSuperSpec Spec = new TeamSuperSpec();

        /// <summary>The combo for these two soldiers, or null (the same soldier twice, or a pair not built yet).</summary>
        public static TeamCombo For(SoldierDef a, SoldierDef b)
        {
            if (a == null || b == null || a == b) return null;
            foreach (TeamCombo c in All)
                if (c.Pairs(a, b)) return c;
            return null;
        }

        public static TeamCombo ById(int id)
        {
            foreach (TeamCombo c in All)
                if (c.Id == id) return c;
            return null;
        }

        /// <summary>TEAM shows: both supers charged, the pair has a combo, and no team super is already playing.</summary>
        public static bool CanFire(bool mineReady, bool partnerReady, TeamCombo combo, bool playing) =>
            mineReady && partnerReady && combo != null && !playing;
    }

    /// <summary>
    /// How a team super plays out, in seconds from the press: the freeze with the split card, then the combo (for Sewer
    /// Rats, the flood rising, then the weasels surfing across), then every enemy on screen pops at once.
    /// </summary>
    public sealed class TeamSuperSpec
    {
        public enum Stage { Card, Flood, Surf, Pop, Done }

        /// <summary>The action freezes and the split card slams in.</summary>
        public float Card = 0.9f;
        /// <summary>tones2's sludge floods the floor.</summary>
        public float Flood = 0.6f;
        /// <summary>kie's weasels surf the wave from one side to the other.</summary>
        public float Surf = 1.2f;
        /// <summary>Everything on screen pops, and the flood drains away.</summary>
        public float Pop = 0.6f;
        /// <summary>How much of a boss's full health it takes; it never ends the fight.</summary>
        public float BossShare = 0.3f;
        /// <summary>Weasels on the wave.</summary>
        public int Weasels = 9;

        public float Length => Card + Flood + Surf + Pop;
        /// <summary>When everything on screen dies.</summary>
        public float KillAt => Card + Flood + Surf;

        public Stage At(float t)
        {
            if (t < Card) return Stage.Card;
            if (t < Card + Flood) return Stage.Flood;
            if (t < KillAt) return Stage.Surf;
            return t < Length ? Stage.Pop : Stage.Done;
        }

        /// <summary>How high the flood stands, 0 to 1: rising, held, then draining away.</summary>
        public float FloodLevel(float t)
        {
            if (t < Card) return 0f;
            if (t < Card + Flood) return Mathf.SmoothStep(0f, 1f, (t - Card) / Flood);
            if (t < KillAt) return 1f;
            return Mathf.Clamp01(1f - (t - KillAt) / Pop);
        }

        /// <summary>How far across the screen the weasels have surfed, 0 to 1 (0 before they start).</summary>
        public float SurfProgress(float t) => Mathf.Clamp01((t - Card - Flood * 0.5f) / (Surf + Flood * 0.5f));

        /// <summary>A boss's big hit: a share of its full health, but always leaving it at least one point.</summary>
        public int BossDamage(int hp, int maxHp) => Mathf.Clamp(Mathf.RoundToInt(maxHp * BossShare), 0, Mathf.Max(0, hp - 1));
    }
}
