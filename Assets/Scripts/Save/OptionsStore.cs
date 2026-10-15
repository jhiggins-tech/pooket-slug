using System;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>The settings in this browser's storage, beside the profile. They stay on this device: they aren't progress.</summary>
    public static class OptionsStore
    {
        const string Key = "pooket-slug.options";

        public static Options Load()
        {
            try { return Options.FromBits(PlayerPrefs.GetInt(Key, 0)); }
            catch (Exception e)
            {
                Log.Game.Warn("Couldn't read the settings: " + e.Message);
                return Options.Default;
            }
        }

        public static void Save(Options options)
        {
            try
            {
                PlayerPrefs.SetInt(Key, options.Bits);
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                Log.Game.Warn("Couldn't keep the settings: " + e.Message);
            }
        }
    }
}
