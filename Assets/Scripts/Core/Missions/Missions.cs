using System.Collections.Generic;

namespace PooketSlug
{
    /// <summary>Whose tabk a mission's boss is: kie's (Mission one's) or kcaj's (Mission two's, <c>docs/design/tanks.md</c>).</summary>
    public enum BossTabk : byte { Kie, Kcaj }

    /// <summary>A mission: its map, its boss, and what the menus and saves call it.</summary>
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
        /// <summary>Whose tabk the boss is.</summary>
        public readonly BossTabk Boss;
        /// <summary>Who jumps out of the wrecked tabk to fight on foot, or null: the wreck wins the mission.</summary>
        public readonly EnemyDef Pilot;
        /// <summary>
        /// Where its painted far and middle plates stand (their bottom edge, in world units), when they're cut at its
        /// floor's top, as Mission two's are; 0 for the shared depths (<see cref="Backdrop.Layers"/>).
        /// </summary>
        public readonly float PlatesStand;

        public MissionDef(string id, byte netId, string name, string place, string[] map, MissionStory story = null,
            BossTabk boss = BossTabk.Kie, EnemyDef pilot = null, float platesStand = 0f)
        {
            PlatesStand = platesStand;
            Boss = boss;
            Pilot = pilot;
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
            "................................................B.s...............s...................s.......................................--------....................................................................................s..s.s......................................................",
            "..............................................-------...........------..............-----...........................s...........I.==....................+................................................................--------.....................................................",
            ".................................................I......-----...................................................-------...........==..................------..........................................................................................................................",
            "..............................V..s..==...r..................................L...s.......V+......................==..I.....===.....==......==................................................................................................................==........................",
            "...P.....W......s....x=..s..################..Z...W......r....x.............################...=r......B=.s...x.==......r.===V.W..==....s.==x.r..s..Z.........B.x...s.=.....K.......M.....T.........G..K..........s.s.rs.............s.r..Y...xx.sx...r.....==sssr...s.xr.s...E....F..",
            "#################################################################################################..r.r.###############################################################################################################################################################################",
            "######################################################################################################################################################################################################################################################################################",
        }, new MissionStory
        {
            // a single-player prelude: the opening, the motor pool reveal as the boss tabk's entrance, freeing kie, then
            // the coda out of the base; beating it opens the den, the crew and co-op (docs/design/story.md)
            Opening = SceneLibrary.Opening,
            BossReveal = SceneLibrary.MotorPool,
            PilotFreed = SceneLibrary.FreeingKie,
            Coda = true,
            SoloOnly = true,
            BeforeDen = true,
            OpensCoop = true,
        }, pilot: Content.Pilot);

        /// <summary>
        /// The Brain Freeze creamery (<c>docs/design/levels.md</c>, Mission two; the m2-map page): the loading bay, the
        /// production line's belts and scoop droppers, the sign yard, the nitrogen hall, the freezer corridor, and the
        /// cold store's yard, one flat screen where kcaj's tabk rolls out of the doors. The first mission open to co-op.
        /// Wreck the tabk and Trollogram kcaj fights on foot; beat him and the freeing scene plays, on both phones in co-op.
        /// </summary>
        public static readonly MissionDef Two = new MissionDef("mission-2", 1, "MISSION 2", "THE BRAIN FREEZE CREAMERY", new[]
        {
            "..................................................................................................................................................................................................................",
            "..................................................................................................................................................................................................................",
            "....................................Q....Q..............Q...Q...........Q...................................................................d.........d...........d...............................................",
            "..................................................................................................................................................................................................................",
            "........................................................................................................................................U.........................................................................",
            "..................s...............................................................................g..............g.................-------........................................................................",
            "................------................................................s.....s..................-------........-------.............................................................................................",
            "..................................................................>>>>>>>>>>>>>...................................................................................................................................",
            "...........=..................................................................................................................................==..................................................................",
            "...P...c..==.s......s..c...s==.g............=..hJ.....r...s.....h...r...........h.n.+....c..s........n..S...n........r.n..r...K..L..N..s..N...==g.N..s..N..nJ.N.h...+.f.s.c.r...h.n.r...s..K.f................T.CF",
            "################################>>>>>>>>>>>>>>####<<<<<<<<<<<<<###################################################################################################################################################",
            "##################################################################################################################################################################################################################",
        }, new MissionStory
        {
            // kcaj's string board briefs it as it starts from the den, and kcaj's tabk rolls out of the cold store in a
            // reveal (on both phones in co-op)
            Opening = SceneLibrary.MissionTwoBriefing,
            BossReveal = SceneLibrary.KcajTabk,
            BossRevealCoop = true,
            PilotFreed = SceneLibrary.FreeingKcaj,
            PilotFreedCoop = true,
            // Trollogram kcaj looks out of his tabk's hatch the first time
            PilotIntro = "TROLLO-KCAJ",
            PilotIntroLine = "YOU'RE NOT IN MY NOTES.",
        }, boss: BossTabk.Kcaj, pilot: Content.KcajPilot, platesStand: 2f);

        public static readonly IReadOnlyList<MissionDef> All = new[] { One, Two };

        /// <summary>
        /// What a co-op room plays when <paramref name="picked"/> is asked for: a solo-only mission (Mission one, the
        /// prelude) gives way to the first one open to co-op.
        /// </summary>
        public static MissionDef ForCoop(MissionDef picked)
        {
            if (picked != null && !picked.Story.SoloOnly) return picked;
            foreach (MissionDef m in All)
                if (!m.Story.SoloOnly) return m;
            return picked;
        }

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
