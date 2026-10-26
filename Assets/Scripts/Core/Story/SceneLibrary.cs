using System.Collections.Generic;

namespace PooketSlug
{
    /// <summary>
    /// The game's cutscenes (<c>scenes/&lt;name&gt;.txt</c>, read by <see cref="SceneScript"/>): the copy built into the game,
    /// and the site's, fetched at start-up, which takes over when it reads cleanly. So a changed line or timing goes
    /// live in the couple of minutes the scene deploy takes, with no build; if the site can't be reached, or its copy
    /// has a problem, the built-in one plays.
    /// <para>
    /// In co-op both phones have to play the same scene, and either might have fetched a newer copy than the other.
    /// Each phone says which copies it has (<see cref="Versions"/>, as they were when the session started), and plays
    /// the site's copy only when its partner has exactly the same; otherwise both play the built-in ones, which match
    /// because co-op needs the same build.
    /// </para>
    /// </summary>
    public sealed class SceneLibrary
    {
        public const string Opening = "opening", MotorPool = "motor-pool", FreeingKie = "freeing-kie", CoopTest = "coop-test",
            MissionTwoBriefing = "m2-briefing", FreeingKcaj = "freeing-kcaj", KcajTabk = "kcaj-tabk";
        /// <summary>The scenes the game plays (a draft played with <c>?scene=</c> needn't be one of them).</summary>
        public static readonly string[] Names = { Opening, MotorPool, FreeingKie, CoopTest, MissionTwoBriefing, FreeingKcaj, KcajTabk };

        readonly Dictionary<string, SceneScript> m_BuiltIn = new Dictionary<string, SceneScript>();
        readonly Dictionary<string, SceneScript> m_Site = new Dictionary<string, SceneScript>();
        Dictionary<string, SceneScript> m_Coop;
        int m_CoopVersions;

        /// <summary>Takes the built-in copy of <paramref name="name"/>; its problems, if any (a test keeps them at none).</summary>
        public List<string> AddBuiltIn(string name, string text)
        {
            SceneScript s = SceneScript.Read(name, text);
            m_BuiltIn[name] = s;
            return s.Problems;
        }

        /// <summary>
        /// Takes the site's copy of <paramref name="name"/> if it reads cleanly and keeps the built-in one's number for a
        /// partner's phone; otherwise keeps the built-in one, and says why.
        /// </summary>
        public bool UseSite(string name, string text, out string why)
        {
            SceneScript s = SceneScript.Read(name, text);
            why = null;
            if (!s.Clean) why = s.Problems.Count + " problem(s): " + s.Problems[0];
            else if (m_BuiltIn.TryGetValue(name, out SceneScript built) && built.Def.NetId != s.Def.NetId) why = "its number for a partner's phone changed (" + built.Def.NetId + " to " + s.Def.NetId + ")";
            else if (m_BuiltIn.TryGetValue(name, out built) && built.Version == s.Version) why = "it's the same as the built-in copy";
            if (why != null) return false;
            m_Site[name] = s;
            return true;
        }

        public bool Has(string name) => m_Site.ContainsKey(name) || m_BuiltIn.ContainsKey(name);

        /// <summary>The scene to play alone: the site's copy if there is one, else the built-in one (null if neither).</summary>
        public CutsceneDef Get(string name) => Script(name)?.Def;

        public SceneScript Script(string name) =>
            m_Site.TryGetValue(name, out SceneScript s) || m_BuiltIn.TryGetValue(name, out s) ? s : null;

        /// <summary>
        /// A number for which copy of each scene this phone plays: the same on two phones with the same copies. Fixed
        /// once <see cref="StartCoop"/> is called, so a copy fetched mid-session can't make the two phones differ.
        /// </summary>
        public int Versions => m_Coop != null ? m_CoopVersions : Combine(m_Site);

        /// <summary>The copies are fixed for a co-op session (<see cref="StartCoop"/>).</summary>
        public bool InCoop => m_Coop != null;

        /// <summary>A co-op session's starting: from now on its scenes are the copies this phone has now.</summary>
        public int StartCoop()
        {
            m_Coop = new Dictionary<string, SceneScript>(m_Site);
            m_CoopVersions = Combine(m_Coop);
            return m_CoopVersions;
        }

        /// <summary>
        /// The scene to play with a partner whose phone says <paramref name="partnerVersions"/>: the copies this phone
        /// had when the session started if theirs match, else the built-in one.
        /// </summary>
        public CutsceneDef ForCoop(string name, int partnerVersions)
        {
            Dictionary<string, SceneScript> site = m_Coop ?? m_Site;
            if (partnerVersions == Versions && site.TryGetValue(name, out SceneScript s)) return s.Def;
            return m_BuiltIn.TryGetValue(name, out SceneScript built) ? built.Def : null;
        }

        static int Combine(Dictionary<string, SceneScript> site)
        {
            if (site.Count == 0) return 0;
            var names = new List<string>(site.Keys);
            names.Sort(System.StringComparer.Ordinal);
            var all = new System.Text.StringBuilder();
            foreach (string n in names) all.Append(n).Append('=').Append(site[n].Version).Append(';');
            int h = SceneScript.Hash(all.ToString());
            return h != 0 ? h : 1;
        }
    }

    /// <summary>
    /// The checks a scene passes before it goes live (the scene deploy runs them, <c>tools/scenecheck</c>, and so do
    /// the tests): every line has time to be read, and every narrator line has time for its recording, whose length
    /// is read from the clip itself.
    /// </summary>
    public static class SceneCheck
    {
        /// <summary>A narrator line holds the screen for its recording plus this, before their next line or the shot's end.</summary>
        public const float Breath = 0.4f;
        /// <summary>A line stays up long enough to read at this many words a second.</summary>
        public const float WordsPerSecond = 3f;

        /// <summary>
        /// The scene's problems with its timing. With <paramref name="clips"/> (each recorded line's length in seconds,
        /// by name), every narrator line needs its clip and the time to play it.
        /// </summary>
        public static List<string> Timing(CutsceneDef scene, IReadOnlyDictionary<string, float> clips = null)
        {
            var problems = new List<string>();
            foreach (CutsceneShot shot in scene.Shots)
                for (int i = 0; i < shot.Lines.Length; i++)
                {
                    CutsceneLine line = shot.Lines[i];
                    if (line.Who == Speaker.Caption) continue;
                    float until = System.Math.Min(shot.Seconds, line.Until);
                    if (line.Who == Speaker.Narrator)
                        for (int j = 0; j < shot.Lines.Length; j++)
                            if (shot.Lines[j].Who == Speaker.Narrator && shot.Lines[j].At > line.At && shot.Lines[j].At < until) until = shot.Lines[j].At;
                    string where = "shot '" + shot.Name + "', the line at " + line.At + " s";
                    int words = line.Text.Split(new[] { ' ', '\n' }, System.StringSplitOptions.RemoveEmptyEntries).Length;
                    if (until - line.At < words / WordsPerSecond)
                        problems.Add(where + " is up for " + (until - line.At).ToString("0.0") + " s, too short to read " + words + " words");
                    if (line.Who != Speaker.Narrator || line.Voice == null || clips == null) continue;
                    if (!clips.TryGetValue(line.Voice, out float clip)) problems.Add(where + ": no recording called '" + line.Voice + "'");
                    else if (until - line.At < clip + Breath)
                        problems.Add(where + ": its recording runs " + clip.ToString("0.00") + " s, and it has " + (until - line.At).ToString("0.00") + " s (it needs " + Breath + " s more than the recording)");
                }
            return problems;
        }

        /// <summary>Every scene has its own number for a partner's phone.</summary>
        public static List<string> Numbers(IEnumerable<SceneScript> scenes)
        {
            var problems = new List<string>();
            var seen = new Dictionary<int, string>();
            foreach (SceneScript s in scenes)
            {
                if (s.Def.NetId == 0) continue;
                if (seen.TryGetValue(s.Def.NetId, out string other)) problems.Add(s.Name + " and " + other + " both have 'net " + s.Def.NetId + "'");
                else seen[s.Def.NetId] = s.Name;
            }
            return problems;
        }

        /// <summary>How long a WAV runs, in seconds, or -1 if it isn't one the game can play.</summary>
        public static float Seconds(byte[] wav)
        {
            float[] samples = Wav.Read(wav, out int rate);
            return samples != null && rate > 0 ? samples.Length / (float)rate : -1f;
        }
    }

    /// <summary>
    /// One of the narrator's recordings as the site has them (<c>scenes/voice.txt</c>, written by the scene check): its
    /// line's name, the hash of its WAV (<see cref="SceneScript.Hash(byte[])"/>, so a phone fetches only the ones that
    /// differ from its build's) and how long it runs.
    /// </summary>
    public struct VoiceClip
    {
        public string Id;
        public int Hash;
        public float Seconds;

        /// <summary>The list's line for this clip: <c>&lt;id&gt; &lt;hash, 8 hex digits&gt; &lt;seconds&gt;</c>.</summary>
        public override string ToString() => Id + " " + Hash.ToString("x8") + " " + Seconds.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>The clips in a list, skipping any line that isn't one.</summary>
        public static List<VoiceClip> Read(string text)
        {
            var clips = new List<VoiceClip>();
            foreach (string raw in (text ?? "").Split('\n'))
            {
                string[] w = raw.Trim().Split(' ');
                if (w.Length != 3 || !SetupText.IsName(w[0])) continue;
                if (!int.TryParse(w[1], System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out int hash)) continue;
                if (!float.TryParse(w[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float seconds)) continue;
                clips.Add(new VoiceClip { Id = w[0], Hash = hash, Seconds = seconds });
            }
            return clips;
        }
    }
}
