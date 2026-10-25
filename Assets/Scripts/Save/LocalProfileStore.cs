using System;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>The profile in this browser's storage: PlayerPrefs, which a WebGL build keeps in IndexedDB.</summary>
    public sealed class LocalProfileStore : IProfileStore
    {
        const string Key = "pooket-slug.profile";
        const string OldHiScoreKey = "pooket-slug.hiscore"; // before profiles, only the high score was kept

        public Profile Load()
        {
            Profile profile = null;
            try
            {
                string json = PlayerPrefs.GetString(Key, "");
                if (json.Length > 0) profile = JsonUtility.FromJson<Profile>(json);
            }
            catch (Exception e)
            {
                Log.Game.Warn("Couldn't read the saved profile, so starting a new one: " + e.Message);
            }
            profile = (profile ?? new Profile()).Repair();

            int oldHiScore = 0;
            try { oldHiScore = PlayerPrefs.GetInt(OldHiScoreKey, 0); }
            catch { /* storage may be unavailable in private browsing */ }
            if (oldHiScore > 0)
            {
                profile.RecordRun(Missions.One, oldHiScore, won: false, solo: true);
                Save(profile); // moves the old key into the profile
            }
            return profile;
        }

        public void Save(Profile profile)
        {
            profile.Stamp(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            try
            {
                PlayerPrefs.SetString(Key, JsonUtility.ToJson(profile));
                PlayerPrefs.DeleteKey(OldHiScoreKey);
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                Log.Game.Warn("Couldn't save the profile: " + e.Message); // storage may be unavailable in private browsing
            }
        }
    }
}
