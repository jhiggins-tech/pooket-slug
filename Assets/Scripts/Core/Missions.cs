using System.Collections.Generic;

namespace PooketSlug
{
    /// <summary>A mission: its map, and what the menus and saves call it.</summary>
    public sealed class MissionDef
    {
        /// <summary>What saves call it. It never changes.</summary>
        public readonly string Id;
        /// <summary>What the shared world sends. It never changes.</summary>
        public readonly byte NetId;
        public readonly string Name;
        /// <summary>Where it's set, as the mission screen and the opening's caption call it.</summary>
        public readonly string Place;
        /// <summary>The map's rows, top first (see <see cref="Level"/>).</summary>
        public readonly string[] Map;
        /// <summary>Its story: scenes, solo only, the coda (<see cref="MissionStory.None"/> for none).</summary>
        public readonly MissionStory Story;

        public MissionDef(string id, byte netId, string name, string place, string[] map, MissionStory story = null)
        {
            Story = story ?? MissionStory.None;
            Id = id;
            NetId = netId;
            Name = name;
            Place = place;
            Map = map;
        }

        public Level Load() => new Level(Map);

        public override string ToString() => Name;
    }

    /// <summary>
    /// The missions, in order. A mission is only ever added: its id is in players' saves and its net id goes on
    /// the wire, so neither may change.
    /// </summary>
    public static class Missions
    {
        public static readonly MissionDef One = new MissionDef("mission-1", 0, "MISSION 1", "THE MILITARY BASE", new[]
        {
            "......................................................................................................................................................................................................................................................................................",
            "......................................................................................................................................................................................................................................................................................",
            "....................................................................d.............d...........................................d.............d.....................d..........................................................................d......d...............d.................",
            "......................................................................U.s.............................................................................................................................................................................................................",
            "......................................................................-----.....................................................s.....................................................................................................................................................",
            "................................................H.s...............s...................s.......................................--------....................................................................................s..s.s......................................................",
            "..............................................-------...........------..............-----...........................s...........I.==....................+................................................................--------.....................................................",
            ".................................................I......-----...................................................-------...........==..................------..........................................................................................................................",
            "..............................V..s..==...r..................................L...s.......V+......................==..I.....===.....==......==................................................................................................................==........................",
            "...P.....W......s....x=..s..################..Z...W......r....x.............################...=r......B=.s...x.==......r.===V.W..==....s.==x.r..s..Z.........B.x...s.=.....K.......M.....T.........G..K..........s.s.rs.............s.r..Y...xx.sx...r.....==sssr...s.xr.s...E....F..",
            "#################################################################################################..r.r.###############################################################################################################################################################################",
            "######################################################################################################################################################################################################################################################################################",
        }, new MissionStory
        {
            // a single-player prelude: the opening, the motor pool reveal as the boss tabk's entrance, freeing kie, then
            // the coda out of the base; beating it opens co-op (docs/design/story.md)
            Opening = SceneLibrary.Opening,
            BossReveal = SceneLibrary.MotorPool,
            PilotFreed = SceneLibrary.FreeingKie,
            Coda = true,
            SoloOnly = true,
            OpensCoop = true,
            CoopName = "TEST MISSION",
        });

        public static readonly IReadOnlyList<MissionDef> All = new[] { One };

        /// <summary>The mission with this id, or null.</summary>
        public static MissionDef ById(string id)
        {
            foreach (MissionDef m in All)
                if (m.Id == id) return m;
            return null;
        }

        /// <summary>The mission with this net id, or null for one this build doesn't have.</summary>
        public static MissionDef ByNetId(int netId)
        {
            foreach (MissionDef m in All)
                if (m.NetId == netId) return m;
            return null;
        }
    }
}
