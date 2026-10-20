using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// The lobby's flow, the crew's den (<c>docs/design/lobby.md</c>, Den 2.0): once the crew's open, START, CREW and co-op
    /// go through it. Walking your soldier round it (the places are the menu), swapping to a crewmate, the couch and
    /// READY, the invite, hosting, and in a co-op room the partner's seat and the countdown, then the mission. As it
    /// opens, any of its beats due play first (<see cref="DenBeats"/>: the first visit, a crewmate moving in).
    /// </summary>
    public sealed class DenFlow
    {
        readonly Game m_Game;
        readonly DenView m_View;
        readonly RollerDoor m_Door = new RollerDoor();
        Den m_Den;
        List<DenBeat> m_Beats = new List<DenBeat>();
        DenBeat m_Beat;

        public DenFlow(Game game, Transform parent)
        {
            m_Game = game;
            m_View = new DenView(parent);
        }

        public bool Showing => m_View.Showing;

        public void Hide() => m_View.Hide();

        /// <summary>Opens the den: to start a solo mission, to look round the crew, or a co-op room's lobby.</summary>
        public void Show(DenMode mode)
        {
            m_Den = new Den(mode, m_Game.Freed, m_Game.Soldier);
            m_ToCouch = false;
            m_HeldDir = 0;
            m_Door.Reset();
            m_Beat = null;
            m_View.Begin(m_Den, InfoNow());
            m_Game.SetState(GameState.Den);
            Log.Game.Info("Den open: " + mode + ", on the couch: " + (m_Den.Pick?.Name ?? "nobody"));
            // a ?test= jump is checking something else: no beats in its way
            m_Beats = m_Game.Flags.Testing ? new List<DenBeat>() : DenBeats.Due(m_Game.Profile, m_Game.Freed, mode);
            foreach (DenBeat b in m_Beats)
                if (b.Newcomer != null) m_View.DarkCorner(b.Newcomer); // still wrecked and dark till they move in
            NextBeat();
        }

        /// <summary>One of the den's beats is playing (the den's theme waits for it).</summary>
        public bool BeatPlaying => m_Beat != null;

        /// <summary>The next of the den's beats due, if there is one.</summary>
        void NextBeat()
        {
            m_Beat = null;
            if (m_Beats.Count == 0) return;
            m_Beat = m_Beats[0];
            m_Beats.RemoveAt(0);
            m_View.PlayBeat(m_Beat);
            Log.Game.Info("Den beat: " + (m_Beat.Kind == DenBeatKind.FirstVisit ? "the first visit" : m_Beat.Newcomer.Name + " moves in"));
            if (m_Beat.Kind == DenBeatKind.MoveIn) Sfx.Play(Sfx.Servo, 0.7f); // the roller door rattles up
        }

        /// <summary>The beat playing moves on, with its sounds; once it's done (or skipped) it's kept as played, and the next one's up.</summary>
        void Beat(float dt)
        {
            DenBeat b = m_Beat;
            b.Tick(dt);
            if (b.Kind == DenBeatKind.FirstVisit)
            {
                if (b.Passed(DenBeat.HeaveTo, dt)) Sfx.Play(Sfx.Thump, 0.7f); // the couch lands back on its feet
                if (b.Passed(DenBeat.PlugTo, dt)) Sfx.Play(Sfx.CableClunk, 0.6f);
                if (b.Passed(DenBeat.WarmFrom, dt)) Sfx.Play(Sfx.Charge, 0.35f); // the lamps, the string lights, the screens
            }
            else
            {
                if (b.Passed(DenBeat.LitAt, dt)) Sfx.Play(Sfx.CableClunk, 0.6f); // a chip clunk: their corner lights up
                if (b.Passed(DenBeat.DownFrom, dt)) Sfx.Play(Sfx.Servo, 0.5f);
            }
            if (!b.Done) return;
            Game g = m_Game;
            if (DenBeats.Played(g.Profile, b)) g.Store.Save(g.Profile);
            m_View.EndBeat(b);
            Log.Game.Info("Den beat done");
            NextBeat();
        }

        /// <summary>A tap, a key or a button while a beat plays skips it (and does nothing else).</summary>
        void SkipBeat(MenuInput keys, SoldierInput soldier)
        {
            bool any = m_View.TakeBack() | m_View.TakeKit() | m_View.TakeHost() | m_View.TakeInvite() | m_View.TakePick() != null
                | m_View.TakePlace() != DenPlace.None | m_View.TakeWalk(out _, out _) | m_View.TakeReady() | m_View.TakeReached() != DenPlace.None;
            any |= keys.Back || keys.Start || keys.Host || soldier.FirePressed || soldier.JumpPressed || soldier.SpecialPressed || soldier.AbilityPressed;
            if (any && m_Game.StateTime > 0.3f) m_Beat.Skip(); // not the START that opened the den
        }

        DenInfo InfoNow()
        {
            Game g = m_Game;
            bool coop = m_Den.Mode == DenMode.Coop;
            MissionDef mission = coop ? Missions.ForCoop(Missions.ByNetId(g.Net.Link.Mission) ?? g.Mission) : g.Mission;
            return new DenInfo
            {
                // Mission one is solo only (docs/design/lobby.md, Co-op): a co-op room plays the first mission open to it
                Mission = m_Den.Mode == DenMode.Crew ? null : coop && mission.Story.CoopName != null ? mission.Story.CoopName : mission.Name.ToUpperInvariant(),
                Room = coop ? g.Net.RoomCode : null,
                InviteLink = coop ? CoopRoom.Link(g.PageUrl, g.Net.RoomCode) : null,
                Watchers = coop ? g.Net.Watchers : 0,
                CanHost = g.CoopReady && m_Den.Mode == DenMode.Solo,
                Skin = coop ? g.Net.Link.LocalSkin : 0,
                DoorLift = Mathf.Max(m_Door.Lift, m_Beat?.DoorLift ?? 0f), // the roller door's rolled up for someone moving in
                DoorThrough = m_Door.Through,
            };
        }

        /// <summary>
        /// The den's buttons and keys. Walking is the menu (docs/design/lobby.md, Den 2.0): a tap on the floor (or left and
        /// right, up and down at the ladder) walks your soldier, a tap on a place walks there and uses it (Jump uses the
        /// one you're standing at), and READY (Enter, fire) walks to the couch and sits down. A crewmate tapped (or Special
        /// for the next) swaps you to them; the kit, hosting and BACK as before.
        /// </summary>
        public void Tick(MenuInput keys, SoldierInput soldier)
        {
            Game g = m_Game;
            bool coop = m_Den.Mode == DenMode.Coop;
            if (coop && !g.CoopAlive()) return;
            if (m_Beat != null)
            {
                SkipBeat(keys, soldier);
                return;
            }
            if (m_View.TakeBack() || keys.Back)
            {
                Log.Game.Info("Den left");
                if (coop) g.Net.Leave();
                g.NewTitleWorld(); // the title's world again, with whoever's on the couch standing in it
                g.SetState(GameState.Title);
                return;
            }
            if (m_View.TakeKit() || soldier.AbilityPressed)
            {
                g.Kit.Show(m_Den.Pick ?? g.Soldier);
                return;
            }
            Invites(coop, keys.Host);
            if ((m_View.TakeHost() || keys.Host) && g.CoopReady && m_Den.Mode == DenMode.Solo)
            {
                Log.Game.Info("Den: into the co-op room from the couch");
                g.JoinCoop();
                return;
            }
            SoldierDef tapped = m_View.TakePick();
            if (tapped == null && soldier.SpecialPressed) tapped = m_Den.NextFree();
            if (tapped != null && tapped != m_Den.Pick)
            {
                if (m_Den.TryPick(tapped)) Log.Game.Info("Den pick: " + tapped.Name);
                else Sfx.Play(Sfx.Thump, 0.3f); // taken, or too late
            }
            if (m_Door.Busy)
            {
                // through the roller door, or back out of it: nothing else till it's done (and nothing tapped meanwhile kept)
                m_View.TakePlace();
                m_View.TakeWalk(out _, out _);
                m_View.TakeReached();
                m_View.TakeReady();
                return;
            }
            Walking(keys, soldier);
            Use(m_View.TakeReached());
            if (m_Den.Pick != null && m_Den.Pick != g.Soldier)
            {
                Options o = g.Options; // the title and the next den remember who's on the couch
                o.Crew = m_Den.Pick.Id;
                g.ChangeOptions(o);
            }
        }

        int m_HeldDir;
        bool m_ToCouch;

        /// <summary>Where your soldier's told to go: taps, the keys, READY. Setting off from the couch gets you up.</summary>
        void Walking(MenuInput keys, SoldierInput soldier)
        {
            DenWalker w = m_View.Walker;
            DenPlace place = m_View.TakePlace();
            bool walk = m_View.TakeWalk(out DenFloor floor, out float x);
            bool keyReady = m_Game.StateTime > 0.3f && (keys.Start || soldier.FirePressed); // not the START that opened the den
            bool ready = m_View.TakeReady() || keyReady;
            if (m_Den.Zoom >= 0f) return;
            if (ready && m_Den.Pick == null)
            {
                // nobody's ours (the partner took our pick): READY picks the next free crewmate, who heads for the couch
                if (m_Den.TryPick(m_Den.NextFree())) m_ToCouch = true;
                return;
            }
            if (w == null) return;
            if (m_ToCouch && m_View.Walker != null) // they're ours now
            {
                m_ToCouch = false;
                ready = true;
            }
            if (ready && m_Den.Ready)
            {
                Sit(false);
                return;
            }
            if (ready) place = DenPlace.Couch;
            if (soldier.JumpPressed && w.Standing)
            {
                // Jump uses the place you stand at, there and then (a wide one like the bunks has no spot to walk to);
                // the couch still walks you to your seat on it
                DenPlace here = DenMap.PlaceAt(w.X, w.Y + 10f);
                if (here != DenPlace.None && here != DenPlace.Couch)
                {
                    if (m_Den.Ready) Sit(false);
                    w.Walk(0);
                    m_HeldDir = 0;
                    Use(here);
                    return;
                }
                place = here;
            }
            int dir = soldier.Left == soldier.Right ? 0 : soldier.Left ? -1 : 1;
            int climb = soldier.Up == soldier.Down ? 0 : soldier.Up ? 1 : -1;
            bool moving = place != DenPlace.None || walk || dir != 0 || (climb != 0 && !w.Climbing && Mathf.Abs(w.X - DenMap.LadderX) <= 12f);
            if (moving && m_Den.Ready && place != DenPlace.Couch) Sit(false);
            if (place != DenPlace.None) w.Go(place);
            else if (walk) w.WalkTo(floor, x);
            else if (climb != 0 && w.Climb(climb)) { }
            else if (dir != 0) w.Walk(dir);
            else if (m_HeldDir != 0) w.Walk(0); // let go of the key: stop
            m_HeldDir = place != DenPlace.None || walk ? 0 : dir;
        }

        /// <summary>Your soldier's walked up to <paramref name="place"/>: use it.</summary>
        void Use(DenPlace place)
        {
            Game g = m_Game;
            switch (place)
            {
                case DenPlace.None: return;
                case DenPlace.Couch:
                    if (m_Den.Mode == DenMode.Crew) m_View.Note("NO MISSION TODAY: JUST HANGING OUT");
                    else Sit(true);
                    return;
                case DenPlace.Board:
                    if (m_Den.Mode == DenMode.Solo) g.Board.Show(fromDen: true); // the host's pick in a co-op room comes later
                    return;
                case DenPlace.Gym:
                    g.Kit.Show(m_Den.Pick ?? g.Soldier);
                    return;
                case DenPlace.RollerDoor:
                    if (m_Den.Ready) Sit(false);
                    if (m_Door.GoIn()) Log.Game.Info("Den: through the roller door to the tabk bay");
                    return;
                case DenPlace.Trophies:
                    m_View.Note("THE TROPHY SHELF: " + Progression.RankAt(g.Profile.rank).Name.ToUpperInvariant()); // the one you hold, so never a spoiler
                    return;
                default:
                    m_View.Note(DenMap.Note(place));
                    return;
            }
        }

        /// <summary>The roller door: up, and through into the tabk bay; out of the bay, back under it and down it comes.</summary>
        void Door(float dt)
        {
            Game g = m_Game;
            if (m_Door.Tick(dt)) g.Bay.Show(m_Den.Pick ?? g.Soldier, m_Den.Mode == DenMode.Coop ? g.Net.Link.LocalSkin : 0);
            else if (m_Door.Inside && !g.Bay.Open) m_Door.ComeOut();
            if (m_Door.TakeRolled())
            {
                Sfx.Play(Sfx.Servo, 0.7f);
                Sfx.Play(Sfx.RocketRumble, 0.25f);
            }
        }

        /// <summary>Sits down on the couch, ready, or gets up.</summary>
        void Sit(bool ready)
        {
            if (!m_Den.SetReady(ready)) return;
            Sfx.Play(Sfx.Pickup, ready ? 0.7f : 0.3f);
            Log.Game.Info("Den " + (ready ? "ready: " : "unready: ") + m_Den.Pick.Name);
        }

        /// <summary>
        /// The TV's invite: armed as a finger goes down on it (or H, hosting's key, goes down in a room), shared or copied
        /// as it lifts; the TV says how it went.
        /// </summary>
        void Invites(bool coop, bool key)
        {
            INetSession net = m_Game.Net;
            string link = coop ? CoopRoom.Link(m_Game.PageUrl, net.RoomCode) : null;
            if ((m_View.TakeInvite() || key) && link != null)
            {
                Invite.Arm(link, "Come play Pooket Slug with me");
                Log.Game.Info("Invite armed: " + link);
            }
            switch (Invite.Result())
            {
                case InviteResult.Shared: m_View.TvSay("LINK SENT"); Log.Game.Info("Invite shared"); break;
                case InviteResult.Copied: m_View.TvSay("LINK COPIED"); Log.Game.Info("Invite copied"); break;
                case InviteResult.Failed:
                    if (link != null) m_View.Note("SEND THIS LINK: " + link, 8f);
                    Log.Game.Info("Invite couldn't be shared or copied: showing the link");
                    break;
            }
        }

        /// <summary>
        /// Moves the den on each frame (over the kit screen and the mission screen too): the partner's seat and flags in,
        /// ours out, the countdown, the zoom, and the mission once it's done.
        /// </summary>
        public void Move(float dt)
        {
            Game g = m_Game;
            bool coop = m_Den.Mode == DenMode.Coop && g.Net != null && g.Net.Ready;
            DenPartner partner = default;
            bool authority = true;
            if (coop)
            {
                authority = g.Net.Link.IsAuthority;
                IReadOnlyList<IAvatarLink> partners = g.Net.Link.Partners;
                if (partners.Count > 0)
                {
                    IAvatarLink p = partners[0];
                    partner = new DenPartner { Flags = p.DenFlags, Playing = p.Playing };
                    if (partner.Seated || partner.Playing) partner.Soldier = p.Soldier; // otherwise it's whoever they played last
                }
            }
            bool zooming = m_Den.Zoom >= 0f;
            m_Den.Tick(dt, partner, authority);
            if (m_Den.TakeUndone())
            {
                m_View.Note("YOUR MATE GOT THERE FIRST: PICK SOMEONE ELSE");
                Sfx.Play(Sfx.Glitch, 0.4f);
                Log.Game.Info("Den pick undone: the partner has it");
            }
            if (!zooming && m_Den.Zoom >= 0f)
            {
                Sfx.Play(Sfx.Start, 0.6f);
                Log.Game.Info("Den START: " + m_Den.Pick.Name + (coop && partner.Present ? ", partner " + (partner.Playing ? "playing" : "in the den") : ""));
            }
            SceneLibrary scenes = g.Scenes.Library;
            if (coop)
            {
                g.Net.Link.SendDen(m_Den.Pick, m_Den.Flags);
                // which copies of the scenes we'd play, fixed for the session (SceneLibrary): only a partner with
                // exactly the same plays them with us, otherwise both play the built-in ones
                if (!scenes.InCoop) Log.Game.Info("Scenes for co-op: " + scenes.StartCoop().ToString("x8"));
                g.Net.Link.SendSceneVersions(scenes.Versions);
            }
            Door(dt);
            if (m_Beat != null) Beat(dt);
            m_View.Refresh(dt, m_Den, InfoNow());
            if (!m_Den.Go || g.Kit.Open || g.Board.Open || g.Bay.Open) return;
            m_View.Hide();
            if (m_Den.Mode == DenMode.Coop)
            {
                // starting together, both phones play the mission's opening scene (Mission two's briefing), synced;
                // ?test=scene plays a stand-in instead, to check the two-phone sync, and any other ?test= skips it.
                // Mission one is single player only, so its scenes never play in a co-op room.
                MissionDef mission = Missions.ForCoop(Missions.ByNetId(g.Net.Link.Mission) ?? g.Mission);
                string scene = g.Flags.Scene ? SceneLibrary.CoopTest : g.Flags.Testing ? null : mission.Story.Opening;
                if (scene != null && partner.InDenNow) g.Scenes.Play(scenes.ForCoop(scene, g.Scenes.PartnerVersions()), true, g.BeginCoop);
                else g.BeginCoop();
            }
            else
            {
                g.NewTitleWorld(); // the soldier on the couch plays
                g.StartFromTitle();
            }
        }
    }
}
