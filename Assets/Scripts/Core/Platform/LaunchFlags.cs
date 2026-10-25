using System;

namespace PooketSlug
{
    /// <summary>
    /// What the page address asks of this visit, read once as the game starts. <c>?unlock=</c> opens what's normally
    /// earned and <c>?test=</c> jumps straight to a part of the game, both for testing by hand and in CI (several at
    /// once, comma separated: <c>?test=boss,reveal</c>). <c>?mission=</c> picks the mission, <c>?skit=</c> a skit to see
    /// as the den opens; <c>?demo=</c>,
    /// <c>?room=</c> and <c>?log=</c> are passed on.
    /// Spikes (<c>?spike=</c>) are <see cref="Spike"/>'s, read before the art and sounds are made.
    /// </summary>
    public sealed class LaunchFlags
    {
        /// <summary>?unlock=coop: co-op is open for this visit.</summary>
        public readonly bool UnlockCoop;
        /// <summary>?unlock=tank: a tabk can be called in each mission.</summary>
        public readonly bool UnlockTank;
        /// <summary>?unlock=crew: the crew is open (the den opens from START).</summary>
        public readonly bool UnlockCrew;

        /// <summary>?test=crate: a crate just ahead of the start, so the co-op test sees one handed out.</summary>
        public readonly bool Crate;
        /// <summary>?test=tank: a tabk of your own parked just ahead of the start, and one call-in.</summary>
        public readonly bool Tank;
        /// <summary>?test=kietabk: as <see cref="Tank"/>, and it's kie's, whatever's loaded in the bay.</summary>
        public readonly bool KieTabk;
        /// <summary>?test=softserve: as <see cref="Tank"/>, and it's kcaj's (SOFT SERVE), whatever's loaded in the bay.</summary>
        public readonly bool SoftServe;
        /// <summary>?test=1up: a 1-up just ahead of the start.</summary>
        public readonly bool OneUp;
        /// <summary>?test=debrief: the mission's won as it starts, so the debrief shows (as if it's the first); in co-op, the room's first round.</summary>
        public readonly bool Debrief;
        /// <summary>?test=coda: a game starts at the coda, the tabk run after the boss, with its prompts.</summary>
        public readonly bool Coda;
        /// <summary>?test=kcajtank: the boss is kcaj's tabk (Mission two's), starting at the boss.</summary>
        public readonly bool KcajTank;
        /// <summary>
        /// ?test=boss: a game starts just before the boss's arena (co-op too: the co-op test's boss round). Also on
        /// with <see cref="Pilot"/> and <see cref="KcajTank"/>, which start there too.
        /// </summary>
        public readonly bool Boss;
        /// <summary>?test=pilot: at the boss, and the tabk's wrecked as it wakes, so the pilot round starts at once.</summary>
        public readonly bool Pilot;
        /// <summary>?test=pilot,weak: and the pilot goes down a few seconds in (or to one shot), to see what beating him does.</summary>
        public readonly bool WeakPilot;
        /// <summary>?test=team: solo, a stand-in partner and both supers charged, to try TEAM.</summary>
        public readonly bool Team;
        /// <summary>?test=scene: in co-op, starting together from the den plays a stand-in scene on both phones.</summary>
        public readonly bool Scene;
        /// <summary>?test=reveal: the motor pool reveal plays even if it's been seen (with the boss: <c>?test=boss,reveal</c>).</summary>
        public readonly bool Reveal;

        /// <summary>Any <c>?test=</c>: a start straight to a part, so a co-op room skips the mission's opening scene.</summary>
        public bool Testing => Crate || Tank || OneUp || Debrief || Coda || KcajTank || Boss || Pilot || WeakPilot || Team || Scene || Reveal;

        /// <summary>?mission=2: the mission START plays (its number), reached or not; null for the usual one.</summary>
        public readonly MissionDef Mission;

        /// <summary>
        /// ?skit=: a skit to play as the den first opens on this visit, seen or not (<see cref="Skits.Named"/>:
        /// <c>two-of-us</c>, <c>special-delivery</c>, <c>trollogram-of-anything</c>), lower case, or null.
        /// </summary>
        public readonly string Skit;

        /// <summary>?demo=: a kit move's scripted demo to play (<c>all</c> records every one, for CI), or null.</summary>
        public readonly string Demo;
        /// <summary>?room=: the room an invite link joins, upper case, or null.</summary>
        public readonly string Room;
        /// <summary>?log=: log levels for this visit, such as <c>net:debug,game:warn</c>, or null.</summary>
        public readonly string LogLevels;

        /// <summary>Reads the flags from the page address.</summary>
        public static LaunchFlags Read(string url) => new LaunchFlags(url);

        LaunchFlags(string url)
        {
            string[] unlock = List(url, "unlock"), test = List(url, "test");
            UnlockCoop = Has(unlock, Reveals.Coop);
            UnlockTank = Has(unlock, Reveals.Tank);
            UnlockCrew = Has(unlock, "crew");
            Crate = Has(test, "crate");
            KieTabk = Has(test, "kietabk");
            SoftServe = Has(test, "softserve");
            Tank = KieTabk || SoftServe || Has(test, "tank");
            OneUp = Has(test, "1up");
            Debrief = Has(test, "debrief");
            Coda = Has(test, "coda");
            KcajTank = Has(test, "kcajtank");
            Pilot = Has(test, "pilot");
            WeakPilot = Pilot && Has(test, "weak");
            Boss = Pilot || KcajTank || Has(test, "boss");
            Team = Has(test, "team");
            Scene = Has(test, "scene");
            Reveal = Has(test, "reveal");
            string mission = PageQuery.Get(url, "mission");
            Mission = mission != null ? Missions.ById("mission-" + mission.Trim()) : null;
            Skit = PageQuery.Get(url, "skit")?.Trim().ToLowerInvariant();
            Demo = PageQuery.Get(url, "demo");
            Room = PageQuery.Get(url, "room")?.Trim().ToUpperInvariant();
            LogLevels = PageQuery.Get(url, "log");
        }

        static string[] List(string url, string name) => (PageQuery.Get(url, name) ?? "").ToLowerInvariant().Split(',');

        static bool Has(string[] list, string key) => Array.IndexOf(list, key) >= 0;
    }
}
