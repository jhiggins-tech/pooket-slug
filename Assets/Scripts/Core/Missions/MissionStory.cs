namespace PooketSlug
{
    /// <summary>
    /// A mission's story (<c>docs/design/story.md</c>): which scenes it plays and when, whether it's solo only, what
    /// beating it opens, and the coda after its boss. The game asks the mission's script (<see cref="MissionDef.Story"/>)
    /// rather than knowing Mission one by name, so a new mission is a new script; the once-only beats (a scene that plays
    /// once, ever, or prompts that show until seen) are kept on the profile by <see cref="StoryRecord"/>.
    /// <para>
    /// The scenes are named as their files are (<c>scenes/&lt;name&gt;.txt</c>, <see cref="SceneLibrary"/>); null for none.
    /// </para>
    /// </summary>
    public sealed class MissionStory
    {
        /// <summary>
        /// Played as the mission starts: before a solo start (from the title, or a solo START in the den), and on both phones
        /// when a co-op room starts it together from the den (Mission two's briefing). A test start skips it.
        /// </summary>
        public string Opening;
        /// <summary>
        /// The boss's entrance the first time it wakes, solo, once ever (<see cref="StoryRecord.Due"/>); the boss intro
        /// after it skips the roll-out. With <see cref="BossRevealCoop"/> it plays in co-op too.
        /// </summary>
        public string BossReveal;
        /// <summary>
        /// <see cref="BossReveal"/> plays on both phones in co-op too (Mission two's, kcaj's tabk arriving): at the first
        /// boss wake of each co-op run, since the two phones' saves can differ on whether it's been seen.
        /// </summary>
        public bool BossRevealCoop;
        /// <summary>
        /// The pilot's intro at the end of a full boss intro (not the quick one) where <see cref="BossReveal"/> doesn't play
        /// first, so the pilot hasn't just been met in it: solo once the reveal's been seen, until the mission's cleared, and
        /// a test start. TROLLO-KCAJ as he looks out of the hatch, and what he says. Null where the pilot's only met in a scene.
        /// </summary>
        public string PilotIntro, PilotIntroLine;
        /// <summary>Played once the boss's pilot is freed, every time (it can be skipped once seen): solo, and in co-op too with <see cref="PilotFreedCoop"/>.</summary>
        public string PilotFreed;
        /// <summary>
        /// <see cref="PilotFreed"/> plays on both phones in co-op too (Mission two's freeing kcaj); without it a co-op
        /// pilot goes the goons' way and the mission's won at once.
        /// </summary>
        public bool PilotFreedCoop;
        /// <summary>After <see cref="PilotFreed"/>: the coda, the run out past the back gate, rather than the win at once.</summary>
        public bool Coda;
        /// <summary>Only ever played solo: never offered for a co-op room (the mission board says SOLO ONLY).</summary>
        public bool SoloOnly;
        /// <summary>Beating it solo opens co-op (<see cref="Profile.coop"/>).</summary>
        public bool OpensCoop;
        /// <summary>
        /// Part of the story's run before the den opens (<see cref="StartFlow"/>): until it's beaten, START plays it, and
        /// the debrief before it goes straight on into it.
        /// </summary>
        public bool BeforeDen;

        /// <summary>
        /// Tabk call-ins it starts with (<c>docs/design/tabks/driving.md</c>, Calling in a tabk): one, for the tabk loaded in the
        /// bay, in every mission after Mission one's coda; none in a mission with a coda, which gives the first call-in
        /// there, once its tabk's been freed. Each player has their own, so co-op phones start alike.
        /// </summary>
        public int CallIns => Coda ? 0 : 1;

        /// <summary>
        /// Whether the boss reveal plays as the boss wakes in <paramref name="run"/>: once ever, solo, the first time
        /// (<paramref name="firstWake"/>, and <paramref name="due"/>: not yet seen; co-op opens only once Mission one's
        /// beaten, so its motor pool has been seen by then); with <see cref="BossRevealCoop"/>, at the first boss wake of
        /// every co-op run instead, the same on both phones whatever their saves say. A test start skips it, unless it's
        /// <c>?test=reveal</c>; a sandbox always does.
        /// </summary>
        public bool PlaysBossReveal(StoryRun run, bool firstWake, bool due)
        {
            if (run.Sandbox || BossReveal == null) return false;
            if (run.TestReveal) return true;
            if (run.Testing) return false;
            if (run.Coop) return BossRevealCoop;
            return firstWake && due;
        }

        /// <summary>
        /// Whether freeing the boss's pilot plays <see cref="PilotFreed"/> (then the coda, or the win): solo, or in co-op
        /// where <see cref="PilotFreedCoop"/> says so; never in a sandbox. Otherwise the mission's won at once.
        /// </summary>
        public bool PlaysPilotFreed(StoryRun run) => PilotFreed != null && !run.Sandbox && PilotGlitches(run.Coop);

        /// <summary>The boss's pilot glitches out when beaten (and is freed), rather than going the goons' way: solo, or in co-op with <see cref="PilotFreedCoop"/>.</summary>
        public bool PilotGlitches(bool coop) => !coop || PilotFreedCoop;

        /// <summary>A mission with no story: no scenes, no coda, open to co-op.</summary>
        public static readonly MissionStory None = new MissionStory();
    }

    /// <summary>How a mission's being played, as its story's rules need to know: in co-op, a sandbox, a test start (<c>?test=</c>), or <c>?test=reveal</c>.</summary>
    public readonly struct StoryRun
    {
        public readonly bool Coop, Sandbox, Testing, TestReveal;

        public StoryRun(bool coop, bool sandbox = false, bool testing = false, bool testReveal = false)
        {
            Coop = coop;
            Sandbox = sandbox;
            Testing = testing;
            TestReveal = testReveal;
        }
    }

    /// <summary>
    /// The once-only beats, as the profile remembers them (<see cref="Profile.seen"/>): each is a key that, once marked,
    /// stays marked. The keys are in players' saves, so they never change.
    /// </summary>
    public sealed class StoryRecord
    {
        /// <summary>The coda's been cleared once, so its prompts don't show again.</summary>
        public const string Coda = "coda";
        /// <summary>The first debrief has played (it explains itself the first time).</summary>
        public const string Debrief = "debrief";
        /// <summary>The super has charged once: its meter, button and controls line show from then on.</summary>
        public const string Super = "super";

        readonly Profile m_Profile;

        public StoryRecord(Profile profile)
        {
            m_Profile = profile;
        }

        /// <summary>Scene <paramref name="scene"/> (a file name) has never played to the end, so a once-only beat of it is due.</summary>
        public bool Due(string scene) => scene != null && !m_Profile.HasSeen(SceneScript.IdOf(scene));

        /// <summary>The coda's prompts still show: it's never been cleared.</summary>
        public bool CodaPrompts => !m_Profile.HasSeen(Coda);

        /// <summary>
        /// The super shows (its meter, button and controls line): it's charged once, or Mission one's beaten, so a save
        /// from before this was kept, past Mission one, has it (docs/design/player.md: it lights up the first time it's charged).
        /// </summary>
        public bool SuperShown => ShowsSuper(m_Profile);

        /// <summary><see cref="SuperShown"/> for <paramref name="profile"/>, without making a record (it's asked every frame).</summary>
        public static bool ShowsSuper(Profile profile) => profile.HasSeen(Super) || profile.coop || MissionBoard.Cleared(profile, Missions.One);

        /// <summary>
        /// The boss intro is the quick one (banner and siren, the tabk already in place): a replay of a mission that's
        /// been cleared, or a retry after losing at the boss door (docs/design/cutscenes/mission-one.md, The boss intro).
        /// </summary>
        public bool QuickBossIntro(MissionDef mission, bool lostAtBoss) => lostAtBoss || Cleared(mission);

        /// <summary><paramref name="mission"/> has been beaten, on any setting.</summary>
        public bool Cleared(MissionDef mission)
        {
            foreach (Setting setting in System.Enum.GetValues(typeof(Setting)))
                if (m_Profile.Clears(mission, setting) > 0) return true;
            return false;
        }

        /// <summary>The next debrief is the first.</summary>
        public bool FirstDebrief => !m_Profile.HasSeen(Debrief);

        /// <summary>Marks <paramref name="key"/> seen; true if that's news (and the profile wants saving).</summary>
        public bool Mark(string key) => m_Profile.MarkSeen(key);
    }
}
