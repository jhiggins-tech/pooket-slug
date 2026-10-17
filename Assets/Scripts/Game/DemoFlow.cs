using System;
using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// <c>?demo=</c>'s flow: the kit moves' scripted demos (<see cref="KitGuide"/>) played in the game, in turn, for CI to
    /// record (<c>tools/demos.mjs</c>).
    /// </summary>
    public sealed class DemoFlow
    {
        readonly Game m_Game;
        IReadOnlyList<MoveGuide> m_Demos;
        int m_Index, m_Frames;
        float m_T, m_Wait;
        bool m_Started, m_Recording; // the first time through ?demo=all, the page records each demo for CI

        public DemoFlow(Game game) => m_Game = game;

        /// <summary>
        /// <c>?demo=tones2-fire</c> plays that move's demo over and over; <c>?demo=all</c> plays every move's in turn, as
        /// CI records them (<c>tools/demos.mjs</c>); <c>?demo=list</c> just logs their names. False for no demo.
        /// </summary>
        public bool Start(string demo)
        {
            if (string.IsNullOrEmpty(demo)) return false;
            var ids = new List<string>();
            foreach (MoveGuide m in KitGuide.All) ids.Add(m.DemoId);
            Log.Game.Info("Demos: " + string.Join(",", ids));
            if (string.Equals(demo, "all", StringComparison.OrdinalIgnoreCase)) m_Demos = KitGuide.All;
            else if (KitGuide.Find(demo) is MoveGuide one) m_Demos = new[] { one };
            else
            {
                if (!string.Equals(demo, "list", StringComparison.OrdinalIgnoreCase)) Log.Game.Warn("No demo called " + demo);
                return false;
            }
            Begin(0);
            return true;
        }

        /// <summary>A fresh world for a demo: its little map, its soldier, who nothing hurts, with the super charged if it's needed.</summary>
        void Begin(int index)
        {
            m_Index = index;
            MoveGuide move = m_Demos[index];
            World world = m_Game.ReplaceWorld(new MissionDef("demo", byte.MaxValue, "DEMO", "", move.Demo.Map), move.Soldier);
            world.Player.NoDamage = true;
            if (move.Demo.SuperReady)
                for (int i = 0; i < Tuning.Kit.SuperKills; i++) world.Player.Kit.AddKill();
            m_T = 0f;
            m_Frames = 0;
            world.Render();
            m_Game.SetState(GameState.Demo);
            if (m_Started) Started(move);
        }

        void Started(MoveGuide move)
        {
            Log.Game.Info("Demo start: " + move.DemoId);
            if (m_Recording) DemoRecording.Begin(move.DemoId);
        }

        /// <summary>
        /// One frame of a demo, on a fixed step (so a slow machine records it in slow motion, and the recorder speeds it
        /// back up). The first waits for Enter or a tap (one demo on its own starts by itself after two seconds), so the page
        /// has finished loading.
        /// </summary>
        public void Tick(MenuInput keys, SoldierInput soldier)
        {
            MoveGuide move = m_Demos[m_Index];
            if (!m_Started)
            {
                m_Wait += Time.unscaledDeltaTime;
                bool waited = m_Demos.Count == 1 && m_Wait > 2f; // ?demo=all (CI's recorder) waits for its Enter
                if (!keys.Start && !soldier.FirePressed && !waited) return;
                m_Started = true;
                m_Recording = m_Demos.Count > 1;
                Started(move);
            }
            const float dt = 1f / DemoScript.Fps;
            m_T += dt;
            m_Frames++;
            m_Game.World.Tick(dt, move.Demo.InputAt(m_Frames));
            if (m_T < move.Demo.Seconds) return;
            Log.Game.Info("Demo end: " + move.DemoId + " (" + m_Frames + " frames, " + (m_Frames / DemoScript.Fps).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " s)");
            if (m_Recording) DemoRecording.End();
            if (m_Index == m_Demos.Count - 1 && m_Demos.Count > 1)
            {
                m_Recording = false;
                Log.Game.Info("Demos done");
            }
            Begin((m_Index + 1) % m_Demos.Count);
        }
    }
}
