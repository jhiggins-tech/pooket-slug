using System;
using System.Collections.Generic;

namespace PooketSlug
{
    /// <summary>How hard a mission is played. Hard opens once a mission is beaten (SCR-04).</summary>
    public enum Setting { Normal, Hard }

    /// <summary>The ids of reveals: things the game shows once, when the player first meets them.</summary>
    public static class Reveals
    {
        /// <summary>Co-op has opened (Mission one beaten solo).</summary>
        public const string Coop = "coop";
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
    /// What the game keeps about a player between visits. It's the local half of the record that sign-in will
    /// keep in the cloud (<c>docs/research/roadmap/refactor-plan.md</c>, "The player record"), so nothing in it
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

        /// <summary>Records a finished run, won or lost. Beating Mission one solo opens co-op.</summary>
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
            if (won && solo && mission == Missions.One) coop = true;
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

    /// <summary>Where the profile is kept: this browser for now, and the cloud once sign-in arrives.</summary>
    public interface IProfileStore
    {
        /// <summary>The saved profile, or a new one.</summary>
        Profile Load();
        void Save(Profile profile);
    }
}
