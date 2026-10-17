using System;
using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// The cutscenes' flow (<c>docs/design/story.md</c>, Cutscenes; <c>docs/scenes.md</c>): which copy of each scene plays
    /// (built in, or the site's once fetched), playing one over the game and moving it on, in co-op on both phones, and
    /// <c>?scene=</c> drafts.
    /// </summary>
    public sealed class SceneFlow
    {
        readonly Game m_Game;
        readonly CutsceneView m_View;
        readonly List<string> m_Cues = new List<string>();
        CutscenePlayer m_Scene;
        bool m_Draft; // a draft played with ?scene=: never marked seen, and back to the title after
        Action m_After;
        bool m_Coop, m_Skip, m_SaidStart;
        int m_Round, m_Tapped;

        /// <summary>The scenes themselves (scenes/&lt;name&gt;.txt): the built-in copies, and the site's once fetched.</summary>
        public SceneLibrary Library { get; }

        public SceneFlow(Game game, Transform parent)
        {
            m_Game = game;
            m_View = new CutsceneView(parent);
            Library = BuiltIn();
        }

        /// <summary>True once, after a scene whose caption card called "mission start", so the start doesn't say it again.</summary>
        public bool TakeSaidStart()
        {
            bool said = m_SaidStart;
            m_SaidStart = false;
            return said;
        }

        /// <summary>
        /// Plays <paramref name="scene"/>, then <paramref name="after"/>. In co-op both phones play it, and a shot moves on
        /// early once both have tapped (<see cref="CutsceneSync"/>).
        /// </summary>
        public void Play(CutsceneDef scene, bool coop, Action after, string note = null)
        {
            m_Draft = note != null;
            m_Scene = new CutscenePlayer(scene, m_Draft || m_Game.Profile.HasSeen(scene.Id), coop ? 2 : 1);
            m_Coop = coop;
            m_Round = coop ? m_Game.Net.Link.Round : 0;
            m_Tapped = -1;
            m_Skip = false;
            m_SaidStart = false;
            m_After = after;
            m_View.Begin(m_Scene, note);
            m_Game.SetState(GameState.Cutscene);
            Log.Game.Info("Cutscene: " + scene.Id + (coop ? ", with a partner" : "") + (m_Scene.CanSkip ? ", seen before" : ""));
        }

        /// <summary>Moves the scene on: taps (and in co-op the partner's), SKIP, and its cues.</summary>
        public void Tick(float dt, MenuInput keys, SoldierInput soldier)
        {
            if (m_Coop && !m_Game.CoopAlive()) return;
            bool tap = m_View.TakeTap() || keys.Start || soldier.FirePressed || soldier.JumpPressed;
            bool skip = m_View.TakeSkip() || keys.Back;
            if (skip && !m_Coop && m_Scene.Skip())
            {
                End();
                return;
            }
            if (skip && m_Coop && m_Scene.CanSkip) m_Skip = true; // it skips once the partner asks too
            int beat = m_Scene.Beat, shot = m_Scene.Shot;
            float left = m_Scene.Scene.Shots[shot].Seconds - m_Scene.T; // a shot that moves on with time left was tapped on
            if (tap && (m_Scene.Tap(0) || m_Scene.HasTapped(0))) m_Tapped = Math.Max(m_Tapped, beat);
            if (m_Coop)
            {
                INetLink link = m_Game.Net.Link;
                IReadOnlyList<IAvatarLink> partners = link.Partners;
                bool here = partners.Count > 0;
                bool partnerSkips = CutsceneSync.Apply(m_Scene, here, here ? partners[0].SceneTaps : 0, m_Round);
                if (m_Skip && partnerSkips && m_Scene.Skip())
                {
                    End();
                    return;
                }
                link.SendScene(CutsceneSync.Pack(m_Round, m_Scene.Scene.NetId, m_Tapped, m_Skip));
                m_View.Waiting(m_Skip ? "SKIPPING ONCE YOUR PARTNER DOES" : null);
            }
            m_Scene.Tick(dt, m_Cues, Sfx.NarratorTalking);
            foreach (string cue in m_Cues)
            {
                if (cue == Cues.SayMissionStart) m_SaidStart = true;
                m_View.Cue(cue);
            }
            if (m_Scene.Shot != shot && !m_Scene.Done)
                Log.Game.Info("Cutscene shot " + (m_Scene.Shot + 1) + " of " + m_Scene.Scene.Shots.Length + (left > 0.5f ? ", tapped on" : ""));
            if (m_Scene.Done) End();
            else m_View.Refresh();
        }

        void End()
        {
            Game g = m_Game;
            if (!m_Draft && g.Profile.MarkSeen(m_Scene.Scene.Id)) g.Store.Save(g.Profile);
            Log.Game.Info("Cutscene over: " + m_Scene.Scene.Id + (m_Scene.Skipped ? ", skipped" : ""));
            // Leave the last word standing as "tapped through it all": the tap that ended it here went out with no
            // packet of its own, so the partner may still be waiting on it. The round and scene key keep it from
            // counting anywhere else.
            if (m_Coop)
                g.Net.Link.SendScene(CutsceneSync.Pack(m_Round, m_Scene.Scene.NetId, m_Scene.Scene.Shots.Length * 3, m_Skip || m_Scene.Skipped));
            Drop();
            Action after = m_After;
            m_After = null;
            after?.Invoke();
        }

        /// <summary>Stops a scene that's playing, and forgets what was to come after it (co-op's ended under it).</summary>
        public void Abandon()
        {
            Drop();
            m_After = null;
        }

        void Drop()
        {
            if (m_Scene == null) return;
            m_View.Hide();
            m_Scene = null;
            m_Coop = false;
        }

        // ------------------------------------------------------------------ built in, from the site, drafts

        /// <summary>
        /// The scenes built into the game (<c>Resources/Scenes/&lt;name&gt;.txt</c>, copied from <c>scenes/</c> by the build;
        /// in the editor, read from <c>scenes/</c> itself).
        /// </summary>
        static SceneLibrary BuiltIn()
        {
            var library = new SceneLibrary();
            foreach (string name in SceneLibrary.Names)
            {
                string text = Resources.Load<TextAsset>("Scenes/" + name)?.text;
#if UNITY_EDITOR
                if (text == null) text = System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath, "..", "scenes", name + ".txt"));
#endif
                if (text == null)
                {
                    Log.Game.Warn("Scene " + name + " isn't built in");
                    continue;
                }
                foreach (string problem in library.AddBuiltIn(name, text)) Log.Game.Warn("Built-in scene " + name + ": " + problem);
            }
            return library;
        }

        /// <summary>
        /// The site's copies of the scenes (<c>scenes/&lt;name&gt;.txt</c> beside the game, published by the scene deploy),
        /// fetched at start-up: each takes over from the built-in copy if it reads cleanly. And the narrator's
        /// recordings, where the site's (<c>scenes/voice.txt</c>) differ from the built-in ones.
        /// </summary>
        public void Fetch(string url)
        {
            foreach (string name in SceneLibrary.Names)
            {
                string file = SetupText.FileUrl(url, "scenes", name);
                if (file == null) return; // off the site: the built-in copies it is
                m_Game.StartCoroutine(SiteFile.Fetch(file, (text, error) =>
                {
                    if (text == null) Log.Game.Info("Scene " + name + ": the site's copy didn't load (" + error + "), playing the built-in one");
                    else if (Library.UseSite(name, text, out string why)) Log.Game.Info("Scene " + name + ": playing the site's copy " + Library.Script(name).Version.ToString("x8"));
                    else Log.Game.Info("Scene " + name + ": playing the built-in copy, the site's isn't used: " + why);
                }));
            }
            string folder = PageQuery.Folder(url);
            m_Game.StartCoroutine(SiteFile.Fetch(folder + "scenes/voice.txt", (text, error) =>
            {
                if (text == null)
                {
                    Log.Game.Info("Scene voices: no list on the site (" + error + ")");
                    return;
                }
                foreach (VoiceClip clip in VoiceClip.Read(text))
                {
                    TextAsset built = Resources.Load<TextAsset>("Narration/" + clip.Id);
                    if (built != null && SceneScript.Hash(built.bytes) == clip.Hash) continue;
                    string id = clip.Id;
                    m_Game.StartCoroutine(SiteFile.FetchBytes(folder + "scenes/voice/" + id + ".wav?v=" + clip.Hash.ToString("x8"), (bytes, why) =>
                    {
                        if (bytes != null && Sfx.UseLine("Narration/" + id, bytes)) Log.Game.Info("Scene voices: the site's recording of " + id);
                        else Log.Game.Warn("Scene voices: the site's recording of " + id + " didn't load (" + (why ?? "not a WAV") + ")");
                    }));
                }
            }));
        }

        /// <summary>The partner's copies of the scenes (<see cref="SceneLibrary.Versions"/>), or -1 with no partner.</summary>
        public int PartnerVersions()
        {
            IReadOnlyList<IAvatarLink> partners = m_Game.Net.Link.Partners;
            return partners.Count > 0 ? partners[0].SceneVersions : -1;
        }

        /// <summary>
        /// <c>?scene=&lt;name&gt;</c> (<c>docs/scenes.md</c>): plays the site's <c>scenes/&lt;name&gt;.txt</c> as a draft, so a
        /// scene can be tried before it's one the game plays: with SKIP, its name and any problems over it, never
        /// marked seen, and back to the title after. Only a name, so only ever a file on the game's own site; off the
        /// site it plays the built-in copy. False for no <c>?scene=</c>.
        /// </summary>
        public bool StartDraft(string name, string url)
        {
            if (name == null) return false;
            name = name.ToLowerInvariant();
            if (!SetupText.IsName(name))
            {
                Log.Game.Warn("?scene= takes a scene's file name, like ?scene=opening");
                return false;
            }
            string file = SetupText.FileUrl(url, "scenes", name);
            if (file == null)
            {
                if (!Library.Has(name)) return false;
                PlayDraft(Library.Script(name), "built-in copy");
                return true;
            }
            Log.Game.Info("Draft scene: loading " + file);
            m_Game.StartCoroutine(SiteFile.Fetch(file, (text, error) =>
            {
                if (text != null) PlayDraft(SceneScript.Read(name, text), "draft");
                else PlayDraft(null, "couldn't load scenes/" + name + ".txt: " + error);
            }));
            return true;
        }

        void PlayDraft(SceneScript script, string what)
        {
            var note = new System.Text.StringBuilder();
            note.Append("SCENE ").Append(script != null ? script.Name : "").Append(": ").Append(what);
            CutsceneDef scene = script?.Def;
            if (script != null)
            {
                var problems = new List<string>(script.Problems);
                problems.AddRange(SceneCheck.Timing(script.Def));
                note.Append(problems.Count == 0 ? ", no problems" : ", " + problems.Count + " problem(s)");
                for (int i = 0; i < problems.Count && i < 4; i++) note.Append("\n").Append(problems[i]);
                foreach (string problem in problems) Log.Game.Warn("Draft scene " + script.Name + ": " + problem);
            }
            if (scene == null || scene.Shots.Length == 0)
                scene = new CutsceneDef("draft", 0, new CutsceneShot("Nothing to play", 8f, new CutsceneLine[0]));
            Play(scene, false, () => m_Game.SetState(GameState.Title), note.ToString());
        }
    }
}
