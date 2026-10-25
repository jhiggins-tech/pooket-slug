using System;
using System.Collections.Generic;

namespace PooketSlug
{
    /// <summary>How hard a mission is played. Hard opens for a mission from Mission two on once it's cleared, in co-op (<see cref="MissionBoard.HardOpen"/>).</summary>
    public enum Setting { Normal, Hard }

    /// <summary>The ids of reveals: things the game shows once, when the player first meets them.</summary>
    public static class Reveals
    {
        /// <summary>Co-op has opened (Mission one beaten solo).</summary>
        public const string Coop = "coop";
        /// <summary>The key of <c>?unlock=tank</c> (a tabk to call in from the start, for testing); play grants the call-in in the coda.</summary>
        public const string Tank = "tank";
    }

    /// <summary>A mission's record at one setting.</summary>
    [Serializable]
    public struct MissionRecord
    {
        /// <summary>The mission's id (<see cref="MissionDef.Id"/>).</summary>
        public string mission;
        public Setting setting;
        /// <summary>Best score.</summary>
        public int best;
        /// <summary>Times beaten.</summary>
        public int clears;
    }

    /// <summary>
    /// What the game keeps about a player between visits. It's the local half of the record that sign-in
    /// keeps in the cloud (<c>Save/CloudAccount.cs</c>), so nothing in it
    /// goes back down, and two copies merge by taking the better of each. Fields are lower case because they're
    /// the saved form.
    /// </summary>
    [Serializable]
    public sealed class Profile
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        /// <summary>Co-op is open: Mission one has been beaten solo.</summary>
        public bool coop;
        public List<MissionRecord> missions = new List<MissionRecord>();
        /// <summary>Reveals already shown, so each plays once (progressive disclosure).</summary>
        public List<string> seen = new List<string>();
        /// <summary>Career XP (<see cref="Progression"/>).</summary>
        public int xp;
        /// <summary>Stages beaten: how far up the rank ladder (<see cref="Progression.RankAt"/>). Never goes down.</summary>
        public int rank;
        /// <summary>First played and last saved, in Unix milliseconds; 0 until the first save.</summary>
        public long first, ts;

        public int Best(MissionDef mission, Setting setting = Setting.Normal)
        {
            int i = Find(mission.Id, setting);
            return i < 0 ? 0 : missions[i].best;
        }

        public int Clears(MissionDef mission, Setting setting = Setting.Normal)
        {
            int i = Find(mission.Id, setting);
            return i < 0 ? 0 : missions[i].clears;
        }

        /// <summary>Records a finished run, won or lost. Beating a mission whose story opens co-op (Mission one), solo, opens it.</summary>
        public void RecordRun(MissionDef mission, Setting setting, int score, bool won, bool solo)
        {
            int i = Find(mission.Id, setting);
            if (i < 0)
            {
                missions.Add(new MissionRecord { mission = mission.Id, setting = setting });
                i = missions.Count - 1;
            }
            MissionRecord r = missions[i];
            r.best = Math.Max(r.best, score);
            if (won) r.clears++;
            missions[i] = r;
            if (won && solo && mission.Story.OpensCoop) coop = true;
        }

        /// <summary>Adds a run's XP, and its climb up the rank ladder.</summary>
        public void Award(int xpGained, int stages)
        {
            xp += Math.Max(0, xpGained);
            rank += Math.Max(0, stages);
        }

        public bool HasSeen(string reveal) => seen.Contains(reveal);

        /// <summary>Marks a reveal as shown. False if it already had been.</summary>
        public bool MarkSeen(string reveal)
        {
            if (seen.Contains(reveal)) return false;
            seen.Add(reveal);
            return true;
        }

        /// <summary>Takes the better of each field from another copy of the record, from another device or the cloud.</summary>
        public void Merge(Profile other)
        {
            if (other == null) return;
            other.Repair();
            version = Math.Max(version, other.version);
            coop |= other.coop;
            xp = Math.Max(xp, other.xp); // the same runs may be on both, so never add them up
            rank = Math.Max(rank, other.rank);
            foreach (MissionRecord theirs in other.missions)
            {
                int i = Find(theirs.mission, theirs.setting);
                if (i < 0)
                {
                    missions.Add(theirs);
                    continue;
                }
                MissionRecord r = missions[i];
                r.best = Math.Max(r.best, theirs.best);
                r.clears = Math.Max(r.clears, theirs.clears); // the same clears may be on both, so never add them up
                missions[i] = r;
            }
            foreach (string s in other.seen) MarkSeen(s);
            if (other.first > 0 && (first == 0 || other.first < first)) first = other.first;
            ts = Math.Max(ts, other.ts);
        }

        /// <summary>Notes a save at <paramref name="nowMs"/> (Unix milliseconds).</summary>
        public void Stamp(long nowMs)
        {
            if (first == 0) first = nowMs;
            ts = nowMs;
        }

        /// <summary>Fills in what an older or damaged save left out. Returns this profile.</summary>
        public Profile Repair()
        {
            if (missions == null) missions = new List<MissionRecord>();
            if (seen == null) seen = new List<string>();
            missions.RemoveAll(r => string.IsNullOrEmpty(r.mission));
            seen.RemoveAll(string.IsNullOrEmpty);
            return this;
        }

        int Find(string mission, Setting setting)
        {
            for (int i = 0; i < missions.Count; i++)
                if (missions[i].mission == mission && missions[i].setting == setting) return i;
            return -1;
        }
    }

    /// <summary>Where the profile is kept: this browser, and the cloud too while signed in (<c>Save/CloudAccount.cs</c>).</summary>
    public interface IProfileStore
    {
        /// <summary>The saved profile, or a new one.</summary>
        Profile Load();
        void Save(Profile profile);
    }

    /// <summary>
    /// Whether a visit keeps what's played on the profile: clears, XP and rank, reveals, the story's records and the den's
    /// beats, in this browser and the cloud. A sandbox (<c>?sandbox=</c>) and a spike (<c>?spike=</c>, <see cref="Spike.Any"/>)
    /// keep nothing: what they play is tuned or experimental. Every other visit keeps it, test starts (<c>?test=</c>) and
    /// unlocks (<c>?unlock=</c>) too; a demo (<c>?demo=</c>) plays no run, so nothing it does would be saved anyway.
    /// </summary>
    public static class ProfileKeeping
    {
        public static bool Saves(LaunchFlags flags, bool sandbox, bool spike) => !sandbox && !spike;

        /// <summary><paramref name="store"/> itself for a visit that saves, else one that loads from it and never writes.</summary>
        public static IProfileStore For(IProfileStore store, bool saves) => saves ? store : new Unsaved(store);

        /// <summary>A store that reads the saved profile and keeps nothing: the visit plays from it, and the next visit finds it as it was.</summary>
        sealed class Unsaved : IProfileStore
        {
            readonly IProfileStore m_Store;
            public Unsaved(IProfileStore store) => m_Store = store;
            public Profile Load() => m_Store.Load();
            public void Save(Profile profile) { }
        }
    }
}
