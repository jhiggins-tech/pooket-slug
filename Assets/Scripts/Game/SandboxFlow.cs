using System;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// <c>?sandbox=</c>'s flow (<see cref="SandboxSetup"/>, <c>docs/sandbox.md</c>): a world from a link, its map, soldier,
    /// boss, cheats and start, played (or its scripted take run) over and over, and never put on the profile.
    /// </summary>
    public sealed class SandboxFlow
    {
        readonly Game m_Game;
        int m_Frames; // a scripted take's frames so far, on the demos' fixed step
        int m_Specials; // the most specials the soldier's had, which cheat specials keeps them at

        /// <summary>The sandbox this visit plays, or null; set once its file's loaded.</summary>
        public SandboxSetup Setup { get; private set; }
        public bool Active => Setup != null;
        /// <summary>Its file's still loading: the title shows behind until it's in.</summary>
        public bool Loading { get; private set; }

        public SandboxFlow(Game game) => m_Game = game;

        /// <summary>
        /// The setup itself, in setup lines (<c>?sandbox=soldier kie;ground P...s...T</c>), or the name of a file published
        /// beside the game (<c>?sandbox=boss</c> reads <c>sandbox/boss.txt</c>). <c>?sandbox=knobs</c> logs every knob there
        /// is, then plays the default. False for no sandbox.
        /// </summary>
        public bool Start(string sandbox, string url)
        {
            if (sandbox == null) return false;
            if (string.Equals(sandbox, "knobs", StringComparison.OrdinalIgnoreCase))
            {
                Log.Game.Info("Sandbox knobs:\n" + string.Join("\n", Knobs.All()));
                sandbox = "";
            }
            if (sandbox.Length > 0 && SetupText.IsName(sandbox))
            {
                string file = SetupText.FileUrl(url, "sandbox", sandbox);
                if (file == null)
                {
                    Begin(SandboxSetup.Failed("sandbox files only load on the site"));
                    return true;
                }
                Loading = true;
                Log.Game.Info("Sandbox: loading " + file);
                m_Game.StartCoroutine(SiteFile.Fetch(file, (text, error) =>
                {
                    Loading = false;
                    Begin(text != null ? SandboxSetup.Read(text) : SandboxSetup.Failed("couldn't load the sandbox file " + sandbox.ToLowerInvariant() + ".txt: " + error));
                }));
                return true;
            }
            Begin(SandboxSetup.Read(sandbox));
            return true;
        }

        void Begin(SandboxSetup setup)
        {
            Setup = setup;
            setup.ApplyTunes(); // before the world, which reads them as it's built
            m_Game.Mission = setup.Mission;
            Log.Game.Info("Sandbox: " + setup.Soldier.Name + ", " + setup.Mission.Map.Length + " rows" +
                          (setup.Script != null ? ", a scripted take of " + setup.Script.Seconds + " s" : "") +
                          ", tuned: " + string.Join(", ", setup.Tunes));
            foreach (string problem in setup.Problems) Log.Game.Warn("Sandbox: " + problem);
            m_Game.NewTitleWorld();
            m_Game.StartSolo();
        }

        /// <summary>What the start banner says in a sandbox: SANDBOX and any problem; nothing in a clean scripted take, which is being recorded.</summary>
        public string Note => Setup.Script != null && Setup.Problems.Count == 0 ? null : Setup.Label;

        /// <summary>The sandbox's world: its map and soldier, its boss, cheats and start, wired for play but never for the profile.</summary>
        public World MakeWorld(Camera camera)
        {
            SandboxSetup s = Setup;
            var world = new World(camera, s.Mission, null, s.Soldier)
            {
                TanksToCall = s.Tank ? 99 : 0,
                SkipToPilot = s.Boss == SandboxBoss.Pilot || s.Boss == SandboxBoss.WeakPilot,
                WeakPilot = s.Boss == SandboxBoss.WeakPilot,
                BossTabk = s.Boss == SandboxBoss.Kcaj ? BossTabk.Kcaj : s.Mission.Boss,
            };
            m_Game.Wire(world, coda: false);
            world.Player.NoDamage = s.Invincible;
            if (s.Super) world.Player.Kit.FillSuper();
            if (s.At == "boss") Game.StartAtBoss(world);
            else if (s.At == "coda") Game.StartAtCoda(world, false);
            else if (s.AtX >= 0f) Game.StartAt(world, s.AtX);
            m_Frames = 0;
            m_Specials = 0;
            return world;
        }

        /// <summary>
        /// One frame of a sandbox: played, or its scripted take on the demos' fixed step, starting again once it's run
        /// its length; then the cheats top up. True if it started again (the frame's over).
        /// </summary>
        public bool Tick(float dt, SoldierInput soldier)
        {
            SandboxSetup s = Setup;
            World world = m_Game.World;
            if (s.Script != null)
            {
                m_Frames++;
                world.Tick(1f / DemoScript.Fps, s.Script.InputAt(m_Frames));
                if (m_Frames / DemoScript.Fps >= s.Script.Seconds)
                {
                    m_Game.Restart();
                    return true;
                }
            }
            else world.Tick(dt, soldier);
            Player p = world.Player;
            if (s.Super) p.Kit.FillSuper();
            if (s.Specials)
            {
                // full: as many as it's had (each kit has its own count: tones2's rocket charges, kie's specials)
                m_Specials = Mathf.Max(m_Specials, p.Specials);
                p.Specials = m_Specials;
            }
            return false;
        }
    }
}
