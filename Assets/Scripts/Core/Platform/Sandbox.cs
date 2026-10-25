using System;
using System.Collections.Generic;
using System.Globalization;

namespace PooketSlug
{
    /// <summary>Which boss fight a sandbox sets up, besides the boss tabk its map places ('T').</summary>
    public enum SandboxBoss { Tabk, Kcaj, Pilot, WeakPilot }

    /// <summary>
    /// A sandbox (<c>?sandbox=</c>, <c>docs/sandbox.md</c>): a map, who plays, goons and a boss, tuning, and maybe a scripted
    /// take, set up from a link or a file beside the game with no build. It's read from setup lines (<see cref="SetupText"/>):
    /// <code>
    /// soldier kie                  who plays, by name: tones2 (the default), kie or kcaj
    /// ground P....s...s..r....T    one row of markers on flat ground (as Level reads them), sky above
    /// air ..........d.......       a row higher up, for drones
    /// map ..........               or the whole map, a row a line, top first (in a file: '#' can't go in a link)
    /// mission 1                    or a mission's own map
    /// at boss                      start a screen short of the boss tabk ('T'), at the coda, or at a column
    /// boss kcaj                    kcaj's tabk, or straight to the pilot (pilot, weak-pilot)
    /// cheat invincible super specials tank
    /// tune kit.superkills 6        a knob (Knobs): physics, kit numbers, defs
    /// keys 0 1.5 right             a scripted take: keys held from and to these seconds (+ for several)
    /// seconds 6                    how long the take runs before it starts again
    /// </code>
    /// It's solo, and a visit that opens one plays only it, so nothing it does is recorded.
    /// </summary>
    public sealed class SandboxSetup
    {
        /// <summary>The map when the setup gives none: flat ground with a few goons.</summary>
        public const string DefaultGround = "..P..........s.......s......r.......s..........";

        public SoldierDef Soldier = Content.Tones2;
        public MissionDef Mission;
        /// <summary>Where it starts: "boss", "coda", or null for the map's 'P' (or <see cref="AtX"/>).</summary>
        public string At;
        public float AtX = -1f;
        public SandboxBoss Boss;
        public bool Invincible, Super, Specials, Tank;
        /// <summary>A scripted take (for move videos), or null to play it.</summary>
        public DemoScript Script;
        /// <summary>What it couldn't follow, as "LINE 3: ...", in upper case for the screen.</summary>
        public readonly List<string> Problems = new List<string>();
        readonly List<(SetupLine line, string path, string value)> m_Tunes = new List<(SetupLine, string, string)>();
        bool m_Tuned;

        public IEnumerable<string> Tunes { get { foreach (var t in m_Tunes) yield return t.path + " " + t.value; } }

        /// <summary>What the screen calls it as it starts: SANDBOX, and the first problem if there was one.</summary>
        public string Label => Problems.Count == 0 ? "SANDBOX" :
            "SANDBOX · " + (Problems.Count == 1 ? "1 PROBLEM: " : Problems.Count + " PROBLEMS: ") + Problems[0];

        void Problem(SetupLine line, string what) => Problems.Add(("LINE " + line.Number + ": " + what).ToUpperInvariant());

        /// <summary>A sandbox that couldn't be loaded: the default one, saying why.</summary>
        public static SandboxSetup Failed(string why)
        {
            SandboxSetup s = Read("");
            s.Problems.Add(why.ToUpperInvariant());
            return s;
        }

        public static SandboxSetup Read(string text) => Read(SetupText.Read(text));

        public static SandboxSetup Read(List<SetupLine> lines)
        {
            var s = new SandboxSetup();
            var map = new List<string>();
            string ground = null, air = null;
            var keys = new List<(float, float, DemoKeys)>();
            float seconds = -1f;
            foreach (SetupLine line in lines)
            {
                string[] args = line.Args;
                string first = args.Length > 0 ? args[0].ToLowerInvariant() : "";
                switch (line.Key)
                {
                    case "soldier":
                        SoldierDef soldier = null;
                        foreach (SoldierDef d in Content.Soldiers)
                            if (string.Equals(d.Name, first, StringComparison.OrdinalIgnoreCase)) soldier = d;
                        if (soldier != null) s.Soldier = soldier;
                        else s.Problem(line, "no soldier called " + first);
                        break;
                    case "map":
                        if (line.Rest.Length > 0) map.Add(line.Rest);
                        break;
                    case "ground": ground = line.Rest; break;
                    case "air": air = line.Rest; break;
                    case "mission":
                        MissionDef mission = null;
                        for (int i = 0; i < Missions.All.Count; i++)
                        {
                            MissionDef m = Missions.All[i];
                            if (first == (i + 1).ToString(CultureInfo.InvariantCulture) || first == m.Id || (i == 0 && first == "one")) mission = m;
                        }
                        if (mission != null) s.Mission = mission;
                        else s.Problem(line, "no mission " + first);
                        break;
                    case "at":
                        if (first == "boss" || first == "coda") s.At = first;
                        else if (float.TryParse(first, NumberStyles.Float, CultureInfo.InvariantCulture, out float x) && x >= 0f) s.AtX = x;
                        else s.Problem(line, "at takes boss, coda or a column");
                        break;
                    case "boss":
                        switch (first)
                        {
                            case "tabk": case "tank": s.Boss = SandboxBoss.Tabk; break;
                            case "kcaj": s.Boss = SandboxBoss.Kcaj; break;
                            case "pilot": s.Boss = SandboxBoss.Pilot; break;
                            case "weak-pilot": s.Boss = SandboxBoss.WeakPilot; break;
                            default: s.Problem(line, "boss takes tabk, kcaj, pilot or weak-pilot"); break;
                        }
                        break;
                    case "cheat":
                        foreach (string a in args)
                        {
                            switch (a.ToLowerInvariant())
                            {
                                case "invincible": s.Invincible = true; break;
                                case "super": s.Super = true; break;
                                case "specials": s.Specials = true; break;
                                case "tank": case "tabk": s.Tank = true; break;
                                default: s.Problem(line, "no cheat called " + a); break;
                            }
                        }
                        break;
                    case "tune":
                        if (args.Length == 2) s.m_Tunes.Add((line, args[0], args[1]));
                        else s.Problem(line, "tune takes a knob and a value");
                        break;
                    case "keys":
                        // in a link a '+' reads as a space, so the keys may come as several words
                        if (args.Length >= 3 && Seconds(args[0], out float from) && Seconds(args[1], out float to) && to > from &&
                            KeysOf(string.Join("+", args, 2, args.Length - 2), out DemoKeys held))
                            keys.Add((from, to, held));
                        else s.Problem(line, "keys takes from, to and keys (left+fire)");
                        break;
                    case "seconds":
                        if (!Seconds(first, out seconds) || seconds <= 0f) s.Problem(line, "seconds takes a number");
                        break;
                    default:
                        s.Problem(line, "no setting called " + line.Key);
                        break;
                }
            }

            if (map.Count > 0) s.Mission = Custom(map.ToArray());
            else if (ground != null) s.Mission = Custom(DemoScript.Ground(ground, air));
            if (s.Mission != null && s.Mission.Load().PlayerStart == default)
            {
                s.Problems.Add("THE MAP HAS NO P, WHERE THE SOLDIER STARTS");
                s.Mission = null;
            }
            if (s.Mission == null) s.Mission = Custom(DemoScript.Ground(DefaultGround));

            if (keys.Count > 0 || seconds > 0f)
            {
                float end = 0f;
                foreach (var (_, to, _) in keys) end = Math.Max(end, to);
                s.Script = new DemoScript(s.Mission.Map, seconds > 0f ? seconds : end + 1f, s.Super, keys.ToArray());
            }
            return s;
        }

        static MissionDef Custom(string[] map) => new MissionDef("sandbox", byte.MaxValue, "SANDBOX", "", map, pilot: Content.Pilot);

        static bool Seconds(string value, out float seconds) =>
            float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out seconds) && seconds >= 0f && seconds < 600f;

        /// <summary>Keys as a take holds them: names joined with '+' or ',' (left, right, up, down, jump, fire, special, super, ability; sp, su, ab).</summary>
        public static bool KeysOf(string value, out DemoKeys keys)
        {
            keys = DemoKeys.None;
            foreach (string name in value.ToLowerInvariant().Split('+', ','))
            {
                switch (name)
                {
                    case "left": keys |= DemoKeys.Left; break;
                    case "right": keys |= DemoKeys.Right; break;
                    case "up": keys |= DemoKeys.Up; break;
                    case "down": keys |= DemoKeys.Down; break;
                    case "jump": keys |= DemoKeys.Jump; break;
                    case "fire": keys |= DemoKeys.Fire; break;
                    case "special": case "sp": keys |= DemoKeys.Special; break;
                    case "super": case "su": keys |= DemoKeys.Super; break;
                    case "ability": case "ab": keys |= DemoKeys.Ability; break;
                    default: return false;
                }
            }
            return keys != DemoKeys.None;
        }

        /// <summary>Turns the knobs (<see cref="Knobs"/>), once, before the first world is built; what can't be is a problem.</summary>
        public void ApplyTunes()
        {
            if (m_Tuned) return;
            m_Tuned = true;
            foreach ((SetupLine line, string path, string value) in m_Tunes)
                if (!Knobs.TrySet(path, value, out string problem)) Problem(line, problem);
        }
    }
}
