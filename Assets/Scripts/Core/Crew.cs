using System;
using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// One crewmate, and everything the game keeps about them in one place: their soldier (numbers and kit), the
    /// mission that frees them, their colours and what the ⓘ screen says about their moves. Their tabk lives in the
    /// game's half of the record (<c>TabkKit</c>), since it's sprites and code. Adding a crewmate: their
    /// <see cref="SoldierDef"/> in <see cref="Content"/>, their moves in <see cref="KitGuide"/>, a row in
    /// <see cref="Crew.All"/>, and their tabk, once they have one, in <c>TabkKit.For</c>.
    /// </summary>
    public sealed class CrewMember
    {
        public readonly SoldierDef Soldier;
        /// <summary>The mission whose first clear frees them; null for tones2, there from the start.</summary>
        public readonly MissionDef FreedBy;
        /// <summary>Their colour on the team card.</summary>
        public readonly Color Card;
        /// <summary>
        /// Their paint, a palette swap over the base soldier's (<c>ArtData</c>'s letters to colours), for player one and
        /// player two. Two partners playing the same crewmate wear one each.
        /// </summary>
        public readonly IReadOnlyList<IReadOnlyDictionary<char, uint>> Paint;
        /// <summary>Their moves, in the order of their buttons.</summary>
        public readonly IReadOnlyList<MoveGuide> Moves;

        public CrewMember(SoldierDef soldier, MissionDef freedBy, Color card, IReadOnlyList<MoveGuide> moves,
            params IReadOnlyDictionary<char, uint>[] paint)
        {
            Soldier = soldier;
            FreedBy = freedBy;
            Card = card;
            Moves = moves;
            Paint = paint;
        }

        public string Name => Soldier.Name;
        public override string ToString() => Name;
    }

    /// <summary>
    /// The crew, and who's been freed (<c>docs/design/levels.md</c>, The order things open up). Everyone is tones2 until
    /// Mission one is beaten solo; then the crew opens, with kie (freed from his tank), and each later mission frees
    /// another. Nothing about it is saved: a crewmate is freed once their mission has been beaten (the owner's call,
    /// 16 Oct), so it comes from the mission records (<see cref="Profile"/>). Who you play is kept with the settings
    /// (<see cref="Options.Crew"/>) and picked in the den. Two partners may play the same soldier.
    /// </summary>
    public static class Crew
    {
        /// <summary>tones2, in his Pooket Tabks colours: a pink bandana for player one, blue for player two.</summary>
        public static readonly CrewMember Tones2 = new CrewMember(Content.Tones2, null, new Color(1f, 0.72f, 0.1f, 0.95f), KitGuide.Tones2,
            new Dictionary<char, uint> { { 'r', 0xff7eb6 }, { 'R', 0xc0508a }, { 'g', 0xff8c42 }, { 'G', 0xc06428 }, { 'c', 0xff5a5f }, { 'C', 0xb83c40 } },
            new Dictionary<char, uint> { { 'r', 0x3a9ae8 }, { 'R', 0x22609c }, { 'g', 0xff8c42 }, { 'G', 0xc06428 }, { 'c', 0xff5a5f }, { 'C', 0xb83c40 } });

        /// <summary>kie, in his Pooket Tabks colours (blue, green and teal), a paler blue for player two.</summary>
        public static readonly CrewMember Kie = new CrewMember(Content.Kie, Missions.One, new Color(0.25f, 0.85f, 0.8f, 0.95f), KitGuide.Kie,
            new Dictionary<char, uint> { { 'r', 0x4ea8ff }, { 'R', 0x2a70c0 }, { 'g', 0x46d27a }, { 'G', 0x2a9050 }, { 'c', 0x2dd4bf }, { 'C', 0x1a9488 } },
            new Dictionary<char, uint> { { 'r', 0xa8d8ff }, { 'R', 0x6898c8 }, { 'g', 0x46d27a }, { 'G', 0x2a9050 }, { 'c', 0x2dd4bf }, { 'C', 0x1a9488 } });

        /// <summary>The whole crew, in the order they're freed (and stand in the den).</summary>
        public static readonly IReadOnlyList<CrewMember> All = new[] { Tones2, Kie };

        /// <summary>The crewmate who plays <paramref name="soldier"/>, or null (the trooper isn't one of the crew).</summary>
        public static CrewMember Of(SoldierDef soldier)
        {
            foreach (CrewMember m in All)
                if (m.Soldier == soldier) return m;
            return null;
        }

        /// <summary>The crewmate beating <paramref name="mission"/> frees, or null.</summary>
        public static CrewMember FreedBy(MissionDef mission)
        {
            foreach (CrewMember m in All)
                if (m.FreedBy != null && m.FreedBy == mission) return m;
            return null;
        }

        /// <summary>Has <paramref name="profile"/> freed <paramref name="member"/>: beaten their mission, at either setting?</summary>
        public static bool IsFree(CrewMember member, Profile profile) =>
            member.FreedBy == null || profile != null && profile.Clears(member.FreedBy) + profile.Clears(member.FreedBy, Setting.Hard) > 0;

        /// <summary>The soldiers <paramref name="profile"/> can pick, in crew order; <paramref name="everyone"/> frees them all (for testing).</summary>
        public static IReadOnlyList<SoldierDef> Freed(Profile profile, bool everyone = false)
        {
            var freed = new List<SoldierDef>();
            foreach (CrewMember m in All)
                if (everyone || IsFree(m, profile)) freed.Add(m.Soldier);
            return freed;
        }

        /// <summary>The soldier to play: the pick, if it's one of the <paramref name="freed"/>, else tones2.</summary>
        public static SoldierDef Pick(int soldierId, IReadOnlyList<SoldierDef> freed)
        {
            foreach (SoldierDef s in freed)
                if (s.Id == soldierId) return s;
            return Content.Tones2;
        }
    }
}
