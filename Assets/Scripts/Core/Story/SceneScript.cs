using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// A cutscene written as a script (<c>scenes/&lt;name&gt;.txt</c>; the format is in <c>docs/scenes.md</c>): its lines,
    /// their timing, its cues and its staging, read into a <see cref="CutsceneDef"/>. The game carries every scene
    /// built in, and at start-up takes the site's copy instead when it reads cleanly (<see cref="SceneLibrary"/>), so a
    /// change to a line or a timing goes live without a build. Anything it can't make sense of is a problem, with
    /// its line number; the check (<c>tools/scenecheck</c>) refuses to put a scene with problems live.
    /// </summary>
    public sealed class SceneScript
    {
        /// <summary>The scene's file name, without <c>.txt</c>; its id (what <see cref="Profile.seen"/> remembers) is <c>scene-</c> and this.</summary>
        public readonly string Name;
        public readonly CutsceneDef Def;
        public readonly List<string> Problems;
        /// <summary>A number for this exact text, so two phones can tell they have the same scene (<see cref="SceneLibrary"/>).</summary>
        public readonly int Version;

        SceneScript(string name, CutsceneDef def, List<string> problems, int version)
        {
            Name = name;
            Def = def;
            Problems = problems;
            Version = version;
        }

        public bool Clean => Problems.Count == 0;

        /// <summary>Who a line's word names.</summary>
        public static readonly Dictionary<string, Speaker> Speakers = new Dictionary<string, Speaker>
        {
            { "tones2", Speaker.Tones2 }, { "kie", Speaker.Kie }, { "goon", Speaker.Goon }, { "trollo-kie", Speaker.Hologram },
            { "kcaj", Speaker.Kcaj }, { "trollo-kcaj", Speaker.TrolloKcaj }, { "caption", Speaker.Caption }, { "narrator", Speaker.Narrator },
        };

        /// <summary>The soldier a speaker's bubble points at.</summary>
        public static string FigureOf(Speaker who)
        {
            switch (who)
            {
                case Speaker.Kie:
                case Speaker.Hologram: return "kie";
                case Speaker.Goon: return "goon";
                case Speaker.Kcaj:
                case Speaker.TrolloKcaj: return "kcaj";
                default: return "tones2";
            }
        }

        /// <summary>The verbs that run over a window, and so need one (<c>2.7-3.9</c>).</summary>
        static readonly string[] NeedWindow = { "move", "walk", "appear", "rip", "spray", "open", "flicker-out" };

        public static SceneScript Read(string name, string text)
        {
            var problems = new List<string>();
            var shots = new List<CutsceneShot>();
            int netId = 0;
            Shot shot = null;
            text = text ?? "";
            foreach (SetupLine line in SetupText.Read(text, links: false))
            {
                string Problem(string what) => "line " + line.Number + ": " + what;
                if (line.Key == "shot")
                {
                    if (shot != null) shots.Add(shot.Build(problems));
                    shot = new Shot(line.Rest.Length > 0 ? line.Rest : "Shot " + (shots.Count + 1), line.Number);
                    continue;
                }
                if (line.Key == "net")
                {
                    if (shot != null) problems.Add(Problem("'net' goes before the first shot"));
                    else if (!int.TryParse(line.Rest, NumberStyles.Integer, CultureInfo.InvariantCulture, out netId) || netId < 1 || netId > 255)
                        problems.Add(Problem("'net' takes a number from 1 to 255, not '" + line.Rest + "'"));
                    continue;
                }
                if (shot == null)
                {
                    problems.Add(Problem("start a shot ('shot: its name') before '" + line.Key + "'"));
                    continue;
                }
                string error = shot.Take(line);
                if (error != null) problems.Add(Problem(error));
            }
            if (shot != null) shots.Add(shot.Build(problems));
            if (shots.Count == 0) problems.Add("the scene has no shots ('shot: its name')");
            if (netId == 0) problems.Add("the scene needs its number for a partner's phone ('net 4')");
            var def = new CutsceneDef(IdOf(name), netId, shots.ToArray());
            return new SceneScript(name, def, problems, Hash(text.Replace("\r", "")));
        }

        /// <summary>The id of the scene in <c>scenes/&lt;name&gt;.txt</c>: what the profile remembers as seen.</summary>
        public static string IdOf(string name) => "scene-" + name;

        /// <summary>FNV-1a over the text's UTF-8 bytes: the same number on every phone for the same text.</summary>
        public static int Hash(string text) => Hash(System.Text.Encoding.UTF8.GetBytes(text ?? ""));

        /// <summary>FNV-1a over <paramref name="bytes"/> (the scene deploy's voice list uses it too: <c>tools/scenecheck</c>).</summary>
        public static int Hash(byte[] bytes)
        {
            unchecked
            {
                uint h = 2166136261;
                foreach (byte b in bytes)
                {
                    h ^= b;
                    h *= 16777619;
                }
                return (int)h;
            }
        }

        /// <summary>One shot as it's read.</summary>
        sealed class Shot
        {
            readonly string m_Name;
            readonly int m_Line;
            float m_Seconds = -1f;
            string m_Set;
            CutsceneIntro m_Intro;
            int m_IntroLine;
            readonly List<CutsceneLine> m_Lines = new List<CutsceneLine>();
            readonly List<int> m_LineNumbers = new List<int>();
            readonly List<CutsceneCue> m_Cues = new List<CutsceneCue>();
            readonly List<int> m_CueNumbers = new List<int>();
            readonly List<StageStep> m_Steps = new List<StageStep>();

            public Shot(string name, int line)
            {
                m_Name = name;
                m_Line = line;
            }

            /// <summary>Reads one of the shot's lines; a problem, or null.</summary>
            public string Take(SetupLine line)
            {
                switch (line.Key)
                {
                    case "length":
                        if (!Number(line.Rest, out m_Seconds) || m_Seconds <= 0f) return "'length' takes the shot's seconds, not '" + line.Rest + "'";
                        return null;
                    case "set":
                        if (!Staging.Sets.ContainsKey(line.Rest)) return "no set called '" + line.Rest + "' (there's " + string.Join(", ", Staging.Sets.Keys) + ")";
                        m_Set = line.Rest;
                        return null;
                    case "intro":
                        return Intro(line);
                }
                // a timed line: "2.2 ...", or "2.7-3.9 ..." for one that runs over a window
                bool timed = Time(line.Key, out float from, out float to, out bool window);
                if (line.Key.Length > 0 && char.IsDigit(line.Key[0]) && !timed) return "'" + line.Key + "' isn't a time (2.2, or 2.7-3.9 for a window)";
                if (window && to <= from) return "the window " + line.Key + " ends before it starts";
                string rest = timed ? line.Rest : line.Key + " " + line.Rest;
                if (!timed)
                {
                    from = 0f;
                    to = 0f;
                }
                rest = rest.Trim();
                if (rest.Length == 0) return "nothing after the time";
                string said = Said(rest, from, timed, line.Number);
                if (said != null) return said.Length == 0 ? null : said;
                return Staged(rest, from, to, window, line.Number);
            }

            string Intro(SetupLine line)
            {
                // intro 4 tones2: TONES2
                int colon = line.Rest.IndexOf(':');
                string[] head = (colon < 0 ? line.Rest : line.Rest.Substring(0, colon)).Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (colon < 0 || head.Length != 2 || !Number(head[0], out float at)) return "an intro is written 'intro 4 tones2: TONES2'";
                if (!Staging.IsFigure(head[1])) return "'" + head[1] + "' isn't a soldier who can have an intro";
                string shown = line.Rest.Substring(colon + 1).Trim();
                if (shown.Length == 0) return "the intro needs the name to show";
                if (m_Intro != null) return "a shot has one intro at most";
                m_Intro = new CutsceneIntro(at, head[1], shown);
                m_IntroLine = line.Number;
                return null;
            }

            /// <summary>A spoken line (<c>6.4 tones2: SNACKS</c>, <c>4.3 narrator opening-1: Friday...</c>), a cue or an announcer's line; null if it's none of them.</summary>
            string Said(string rest, float at, bool timed, int number)
            {
                string[] words = rest.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (words[0] == "cue" || words[0] == "say")
                {
                    if (words.Length != 2) return "'" + words[0] + "' takes one name: '2.2 " + words[0] + " drop'";
                    string id = words[0] == "say" ? Cues.Say(words[1]) : words[1];
                    if (!Cues.Handled(id)) return "no cue called '" + words[1] + "'";
                    m_Cues.Add(new CutsceneCue(at, id));
                    m_CueNumbers.Add(number);
                    return "";
                }
                int colon = rest.IndexOf(':');
                if (colon < 0) return null;
                string[] head = rest.Substring(0, colon).Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (head.Length == 0 || head.Length > 2 || !Speakers.TryGetValue(head[0].ToLowerInvariant(), out Speaker who)) return null;
                if (!timed) return "a line needs its time: '2.4 " + head[0] + ": ...'";
                string voice = head.Length > 1 ? head[1] : null;
                if (voice != null && who != Speaker.Narrator) return "only the narrator's lines are recorded, so only theirs name a voice";
                if (voice != null && !SetupText.IsName(voice)) return "'" + voice + "' isn't a voice line's name";
                string said = rest.Substring(colon + 1).Trim().Replace(" | ", "\n").Replace("|", "\n");
                if (said.Length == 0) return "the line has no words";
                m_Lines.Add(new CutsceneLine(at, who, said, voice));
                m_LineNumbers.Add(number);
                return "";
            }

            /// <summary>Staging: <c>[time] thing verb ...</c>, or <c>show</c>/<c>hide</c> with things, or the camera.</summary>
            string Staged(string rest, float from, float to, bool window, int number)
            {
                string[] w = rest.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < w.Length; i++) w[i] = w[i].ToLowerInvariant();
                StageStep Step(string thing, string verb) => new StageStep { Line = number, From = from, To = window ? to : from, Window = window, Thing = thing, Verb = verb };

                if (w[0] == "show" || w[0] == "hide")
                {
                    if (w.Length < 2) return "'" + w[0] + "' names what to " + w[0];
                    for (int i = 1; i < w.Length; i++)
                    {
                        if (!Staging.Things.ContainsKey(w[i])) return Unknown(w[i]);
                        m_Steps.Add(Step(w[i], w[0]));
                    }
                    return null;
                }
                if (w[0] == "camera")
                {
                    // camera 1.15 20 -12 [ease]: how far in, and where it looks (from the stage's middle)
                    StageStep cam = Step("camera", "camera");
                    if (!Numbers(w, 1, 1, 3, cam, out string bad, eases: true)) return bad ?? "'camera' takes how far in, then where it looks: 'camera 1.15 20 -12'";
                    m_Steps.Add(cam);
                    return null;
                }
                if (!Staging.Things.ContainsKey(w[0])) return Unknown(w[0]);
                if (w.Length < 2) return "what does " + w[0] + " do? (a verb, like '" + w[0] + " at 56')";
                string thing = w[0], verb = w[1];
                if (!Staging.Has(thing, verb)) return w[0] + " can't '" + verb + "'";
                if (Array.IndexOf(NeedWindow, verb) >= 0 && !window) return "'" + verb + "' runs over a window: '2.7-3.9 " + thing + " " + verb + " ...'";
                StageStep s = Step(thing, verb);
                string problem;
                switch (verb)
                {
                    case "show":
                    case "hide":
                    case "sway":
                    case "glitch-out":
                    case "up":
                    case "stutter":
                    case "appear":
                    case "rip":
                    case "rattle":
                    case "open":
                    case "flicker-out":
                        if (w.Length > 2) return "'" + verb + "' takes nothing after it";
                        break;
                    case "at":
                        if (!Numbers(w, 2, 1, 2, s, out problem)) return problem ?? "'at' takes where: 'at 56', or 'at 56 12' off the floor";
                        break;
                    case "move":
                        if (!Numbers(w, 2, 4, 4, s, out problem, eases: true)) return problem ?? "'move' takes from and to: 'move 66 12 66 0'";
                        break;
                    case "walk":
                        if (!Numbers(w, 2, 2, 3, s, out problem, eases: true)) return problem ?? "'walk' takes from and to: 'walk -16 196'";
                        break;
                    case "turn":
                    case "fade":
                        if (!Numbers(w, 2, 1, 2, s, out problem, eases: true)) return problem ?? "'" + verb + "' takes a value, or from and to";
                        if (s.N.Length == 2 && !window) return "'" + verb + "' from one value to another runs over a window";
                        break;
                    case "size":
                        if (!Numbers(w, 2, 2, 4, s, out problem, eases: true) || s.N.Length == 3) return problem ?? "'size' takes across and up ('size 1 0.5'), or from and to";
                        if (s.N.Length == 4 && !window) return "'size' from one size to another runs over a window";
                        break;
                    case "blink":
                        if (!Numbers(w, 2, 2, 2, s, out problem)) return problem ?? "'blink' takes how fast and one beat in how many: 'blink 9 3'";
                        break;
                    case "shake":
                        if (!Numbers(w, 2, 0, 1, s, out problem)) return problem ?? "'shake' takes how far, or nothing";
                        break;
                    case "spill":
                    case "burst":
                        if (!Numbers(w, 2, 1, 1, s, out problem)) return problem ?? "'" + verb + "' takes where: '" + verb + " 66'";
                        break;
                    case "spray":
                        if (!Numbers(w, 2, 2, 2, s, out problem)) return problem ?? "'spray' takes from and to: 'spray 66 168'";
                        break;
                    case "colour":
                    {
                        // colour cyan, or 1.2-1.8 colour warm cyan [ease]
                        int i = 2;
                        if (w.Length < 3 || !Staging.TryColour(w[2], out s.A)) return w.Length < 3 ? "'colour' takes a colour, or from and to" : "no colour called '" + w[2] + "'";
                        i++;
                        if (i < w.Length && Staging.TryColour(w[i], out s.B))
                        {
                            s.HasB = true;
                            i++;
                        }
                        if (i < w.Length && Staging.TryEase(w[i], out s.Ease)) i++;
                        if (i != w.Length) return "'" + w[i] + "' isn't a colour or an ease";
                        if (s.HasB && !window) return "'colour' from one to another runs over a window";
                        break;
                    }
                    case "flicker":
                        if (w.Length < 4 || w.Length > 5) return "'flicker' takes the colour it blinks, the one it is, and how fast: 'flicker warm cyan 14'";
                        if (!Staging.TryColour(w[2], out s.A)) return "no colour called '" + w[2] + "'";
                        if (!Staging.TryColour(w[3], out s.B)) return "no colour called '" + w[3] + "'";
                        s.HasB = true;
                        s.N = new[] { 14f };
                        if (w.Length == 5 && !Number(w[4], out s.N[0])) return "'" + w[4] + "' isn't how fast";
                        break;
                    case "face":
                        if (w.Length != 3 || w[2] != "left" && w[2] != "right") return "'face' takes left or right";
                        s.Word = w[2];
                        break;
                    case "pose":
                        if (w.Length != 3 || Array.IndexOf(Staging.Poses, w[2]) < 0) return "'pose' takes one of " + string.Join(", ", Staging.Poses);
                        s.Word = w[2];
                        break;
                    case "arms":
                        if (w.Length != 3 || Array.IndexOf(Staging.Arms, w[2]) < 0) return "'arms' takes one of " + string.Join(", ", Staging.Arms);
                        s.Word = w[2];
                        break;
                    case "look":
                        if (w.Length != 3 || Array.IndexOf(Staging.Looks, w[2]) < 0) return "'look' takes one of " + string.Join(", ", Staging.Looks);
                        s.Word = w[2];
                        break;
                    case "on":
                        // stars on kie 0 22: rides on a soldier, so far from their feet
                        if (w.Length != 3 && w.Length != 5 || !Staging.IsFigure(w[2])) return "'on' takes a soldier, and how far from their feet: 'on kie 0 22'";
                        s.Word = w[2];
                        if (!Numbers(w, 3, 0, 2, s, out problem) || s.N.Length == 1) return problem ?? "'on' takes across and up from their feet";
                        if (s.N.Length == 0) s.N = new[] { 0f, 0f };
                        break;
                    case "holds":
                        return Holds(w, s);
                }
                m_Steps.Add(s);
                return null;
            }

            /// <summary><c>kie holds bag</c>, <c>tones2 holds bag cup</c>, <c>kie holds bag -7 9</c>: each prop rides on them.</summary>
            string Holds(string[] w, StageStep s)
            {
                var props = new List<string>();
                var offset = new List<float>();
                for (int i = 2; i < w.Length; i++)
                {
                    if (Number(w[i], out float n)) offset.Add(n);
                    else if (!Staging.Things.TryGetValue(w[i], out Staging.Thing t) || t.Kind != Staging.Kind.Prop) return "'" + w[i] + "' isn't a prop to hold";
                    else if (offset.Count > 0) return "the props come first, then where they're held: 'holds bag -7 9'";
                    else props.Add(w[i]);
                }
                if (props.Count == 0) return "'holds' names what they hold: 'holds bag cup'";
                if (offset.Count != 0 && offset.Count != 2) return "'holds' takes across and up from their feet, or nothing";
                foreach (string prop in props)
                {
                    Vector2 hand = offset.Count == 2 ? new Vector2(offset[0], offset[1]) : Staging.Hand(prop);
                    m_Steps.Add(new StageStep { Line = s.Line, From = s.From, To = s.To, Window = false, Thing = prop, Verb = "held", Word = s.Thing, N = new[] { hand.x, hand.y } });
                }
                return null;
            }

            static string Unknown(string thing) => "no thing called '" + thing + "' on the stage";

            /// <summary>Numbers from word <paramref name="start"/> on (between <paramref name="min"/> and <paramref name="max"/>), and an ease after them if allowed.</summary>
            static bool Numbers(string[] w, int start, int min, int max, StageStep s, out string problem, bool eases = false)
            {
                problem = null;
                var n = new List<float>();
                for (int i = start; i < w.Length; i++)
                {
                    if (Number(w[i], out float v))
                    {
                        if (n.Count == max) return false;
                        n.Add(v);
                    }
                    else if (eases && i == w.Length - 1 && Staging.TryEase(w[i], out s.Ease)) { }
                    else
                    {
                        problem = "'" + w[i] + "' isn't a number" + (eases ? " or an ease (in, out, smooth)" : "");
                        return false;
                    }
                }
                if (n.Count < min) return false;
                s.N = n.ToArray();
                return true;
            }

            public CutsceneShot Build(List<string> problems)
            {
                string Problem(int line, string what) => "line " + line + ": " + what;
                if (m_Seconds <= 0f)
                {
                    problems.Add(Problem(m_Line, "shot '" + m_Name + "' needs its length ('length 8.5')"));
                    m_Seconds = 1f;
                }
                if (m_Set == null) problems.Add(Problem(m_Line, "shot '" + m_Name + "' needs its set ('set street')"));
                for (int i = 0; i < m_Lines.Count; i++)
                    if (m_Lines[i].At >= m_Seconds) problems.Add(Problem(m_LineNumbers[i], "the line comes after the shot's ended (it's " + m_Seconds + " s long)"));
                for (int i = 0; i < m_Cues.Count; i++)
                    if (m_Cues[i].At >= m_Seconds) problems.Add(Problem(m_CueNumbers[i], "the cue comes after the shot's ended (it's " + m_Seconds + " s long)"));
                foreach (StageStep s in m_Steps)
                    if (s.From >= m_Seconds) problems.Add(Problem(s.Line, "it comes after the shot's ended (it's " + m_Seconds + " s long)"));
                if (m_Intro != null)
                {
                    if (m_Intro.At < CutscenePlayer.BuildUp) problems.Add(Problem(m_IntroLine, "an intro needs " + CutscenePlayer.BuildUp + " s to build up first, so it lands at " + CutscenePlayer.BuildUp + " s or later"));
                    if (m_Intro.At >= m_Seconds) problems.Add(Problem(m_IntroLine, "the intro lands after the shot's ended"));
                }
                // steps in time order, keeping the order they're written in for steps at the same time
                var steps = new List<StageStep>(m_Steps);
                var order = new Dictionary<StageStep, int>();
                for (int i = 0; i < steps.Count; i++) order[steps[i]] = i;
                steps.Sort((a, b) => a.From != b.From ? a.From.CompareTo(b.From) : order[a].CompareTo(order[b]));
                var lines = new List<CutsceneLine>(m_Lines);
                lines.Sort((a, b) => a.At.CompareTo(b.At));
                var cues = new List<CutsceneCue>(m_Cues);
                cues.Sort((a, b) => a.At.CompareTo(b.At));
                return new CutsceneShot(m_Name, m_Seconds, lines.ToArray(), cues.ToArray(), m_Intro, m_Set ?? "street", steps.ToArray());
            }
        }

        static bool Number(string word, out float value) =>
            float.TryParse(word, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && !float.IsNaN(value) && !float.IsInfinity(value);

        /// <summary>A time: <c>2.2</c>, or a window <c>2.7-3.9</c>.</summary>
        static bool Time(string word, out float from, out float to, out bool window)
        {
            from = to = 0f;
            window = false;
            if (word.Length == 0 || !char.IsDigit(word[0])) return false;
            int dash = word.IndexOf('-');
            if (dash < 0) return Number(word, out from) && from >= 0f && (to = from) >= 0f;
            window = true;
            return Number(word.Substring(0, dash), out from) && Number(word.Substring(dash + 1), out to) && from >= 0f;
        }
    }
}
