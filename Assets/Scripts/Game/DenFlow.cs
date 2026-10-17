using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// The lobby's flow, the crew's gaming den (<c>docs/design/lobby.md</c>): once the crew's open, START, CREW and co-op go
    /// through it. The couch pick, READY, the invite, hosting, and in a co-op room the partner's seat and the countdown,
    /// then the mission.
    /// </summary>
    public sealed class DenFlow
    {
        readonly Game m_Game;
        readonly DenView m_View;
        Den m_Den;

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
            m_View.Begin(m_Den, InfoNow());
            m_Game.SetState(GameState.Den);
            Log.Game.Info("Den open: " + mode + ", on the couch: " + (m_Den.Pick?.Name ?? "nobody"));
        }

        DenInfo InfoNow()
        {
            Game g = m_Game;
            bool coop = m_Den.Mode == DenMode.Coop;
            MissionDef mission = coop ? Missions.ByNetId(g.Net.Link.Mission) ?? g.Mission : g.Mission;
            return new DenInfo
            {
                // Mission one is solo only (docs/design/lobby.md, Co-op): until Mission two is built, co-op plays it as a stand-in
                Mission = m_Den.Mode == DenMode.Crew ? null : coop && mission.Story.CoopName != null ? mission.Story.CoopName : mission.Name.ToUpperInvariant(),
                Room = coop ? g.Net.RoomCode : null,
                InviteLink = coop ? InviteCode.Link(g.PageUrl, g.Net.RoomCode) : null,
                CanList = coop && RoomBrowser.HostChooses && g.Net.Link.IsAuthority,
                Listed = coop && g.Net.Listed,
                Watchers = coop ? g.Net.Watchers : 0,
                CanHost = g.CoopReady && m_Den.Mode == DenMode.Solo,
                Skin = coop ? g.Net.Link.LocalSkin : 0,
            };
        }

        /// <summary>The den's buttons and keys: a crewmate tapped (or Special for the next), READY (Enter, fire), the kit, hosting, BACK.</summary>
        public void Tick(MenuInput keys, SoldierInput soldier)
        {
            Game g = m_Game;
            bool coop = m_Den.Mode == DenMode.Coop;
            if (coop && !g.CoopAlive()) return;
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
            if (m_View.TakeBoard() && m_Den.Mode == DenMode.Solo) // the host's pick in a co-op room comes later
            {
                g.Board.Show(fromDen: true);
                return;
            }
            Invites(coop, keys.Host);
            if (m_View.TakeList() && coop && g.Net.Link.IsAuthority) g.Net.Listed = !g.Net.Listed;
            if ((m_View.TakeHost() || keys.Host) && g.CoopReady && m_Den.Mode == DenMode.Solo)
            {
                Log.Game.Info("Den: hosting from the couch");
                g.GoOnline(() => g.Net.Host(RoomBrowser.HostListed));
                return;
            }
            SoldierDef tapped = m_View.TakePick();
            if (tapped == null && soldier.SpecialPressed) tapped = m_Den.NextFree();
            if (tapped != null && tapped != m_Den.Pick)
            {
                if (m_Den.TryPick(tapped)) Log.Game.Info("Den pick: " + tapped.Name);
                else Sfx.Play(Sfx.Thump, 0.3f); // taken, or too late
            }
            bool keyReady = g.StateTime > 0.3f && (keys.Start || soldier.FirePressed || soldier.JumpPressed); // not the START that opened the den
            if (m_View.TakeReady() || keyReady)
            {
                if (m_Den.ToggleReady())
                {
                    Sfx.Play(Sfx.Pickup, m_Den.Ready ? 0.7f : 0.3f);
                    Log.Game.Info("Den " + (m_Den.Ready ? "ready: " : "unready: ") + m_Den.Pick.Name);
                }
            }
            if (m_Den.Pick != null && m_Den.Pick != g.Soldier)
            {
                Options o = g.Options; // the title and the next den remember who's on the couch
                o.Crew = m_Den.Pick.Id;
                g.ChangeOptions(o);
            }
        }

        /// <summary>
        /// The TV's invite: armed as a finger goes down on it (or H, hosting's key, goes down in a room), shared or copied
        /// as it lifts; the TV says how it went.
        /// </summary>
        void Invites(bool coop, bool key)
        {
            INetSession net = m_Game.Net;
            string link = coop ? InviteCode.Link(m_Game.PageUrl, net.RoomCode) : null;
            if ((m_View.TakeInvite() || key) && link != null)
            {
                Invite.Arm(link, "Come play Pooket Slug with me: room " + net.RoomCode);
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
            m_View.Refresh(dt, m_Den, InfoNow());
            if (!m_Den.Go || g.Kit.Open || g.Board.Open) return;
            m_View.Hide();
            if (m_Den.Mode == DenMode.Coop)
            {
                // Mission one is single player only, so its scenes never play in a co-op room; ?test=scene plays a
                // stand-in on both phones when starting together, to check the two-phone sync
                if (g.Flags.Scene && partner.InDenNow) g.Scenes.Play(scenes.ForCoop(SceneLibrary.CoopTest, g.Scenes.PartnerVersions()), true, g.BeginCoop);
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
