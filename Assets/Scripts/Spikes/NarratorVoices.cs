using System;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// The narrator voice spike (<c>?spike=narrator</c>): the candidate voices the vendor repository's CI made with
    /// OpenRouter's text-to-speech (its voice/narrator.json), fetched into <c>Resources/Narrator</c> by the game's CI
    /// with a <c>candidates.json</c> naming them. A build without them has none, and the narrator stays subtitles only.
    /// </summary>
    public sealed class NarratorVoices
    {
        [Serializable]
        public sealed class Candidate
        {
            public string key, label, model, voice;
        }

        [Serializable]
        sealed class List
        {
            public Candidate[] candidates = new Candidate[0];
        }

        readonly Candidate[] m_All;
        int m_Index;

        NarratorVoices(Candidate[] all, int index)
        {
            m_All = all;
            m_Index = index;
        }

        /// <summary>The voices in this build, starting with <paramref name="wanted"/> (a key) if it's one; null if there are none.</summary>
        public static NarratorVoices Load(string wanted)
        {
            var json = Resources.Load<TextAsset>("Narrator/candidates");
            Candidate[] all = null;
            try
            {
                all = json != null ? JsonUtility.FromJson<List>(json.text)?.candidates : null;
            }
            catch (Exception e)
            {
                Log.Audio.Warn("Narrator candidates unreadable: " + e.Message);
            }
            if (all == null || all.Length == 0)
            {
                Log.Audio.Info("No narrator voices in this build");
                return null;
            }
            int index = Mathf.Max(0, Array.FindIndex(all, c => c.key == wanted));
            return new NarratorVoices(all, index);
        }

        public Candidate Current => m_All[m_Index];
        public int Count => m_All.Length;

        /// <summary>What the voice button says.</summary>
        public string Label => "VOICE " + Current.key.ToUpperInvariant() + " OF " + Count + " · " + Current.label.ToUpperInvariant();

        public void Next() => m_Index = (m_Index + 1) % m_All.Length;

        public void Say(string line)
        {
            if (!string.IsNullOrEmpty(line)) Sfx.Narrate(Current.key, line);
        }
    }
}
