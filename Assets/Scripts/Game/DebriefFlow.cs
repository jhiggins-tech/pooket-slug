using System;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// The debrief's flow (<c>docs/design/cutscenes.md</c>, The first debrief): a won run's XP and rank go on the
    /// profile, and the debrief shows them part by part over the held world. In co-op each phone shows its own player's
    /// (their kills, lives and XP; the score is the shared one), and CONTINUE goes back to the room's den.
    /// </summary>
    public sealed class DebriefFlow
    {
        readonly Game m_Game;
        readonly DebriefView m_View;

        /// <summary>The debrief, while it's up; otherwise null.</summary>
        public DebriefPlayer Now { get; private set; }

        public DebriefFlow(Game game, Transform parent)
        {
            m_Game = game;
            m_View = new DebriefView(parent);
        }

        /// <summary>Whether the debrief is on a part that offers signing in (the sign-in button shows over it).</summary>
        public bool OffersSignIn => Now != null && (Now.Now == DebriefPlayer.Part.SignIn || Now.Now == DebriefPlayer.Part.Continue) && Now.Result.OfferSignIn;

        public void Show(int bestBefore, bool firstClear)
        {
            Game g = m_Game;
            World w = g.World;
            Profile profile = g.Profile;
            int score = w.Scoring.Points;
            var result = new DebriefResult
            {
                Score = score, Best = Math.Max(bestBefore, score), NewBest = score > bestBefore && bestBefore > 0,
                Seconds = Mathf.FloorToInt(w.Elapsed), Kills = w.Kills,
                Accuracy = w.ShotsFired > 0 ? Mathf.Clamp01(w.ShotsHit / (float)w.ShotsFired) : -1f,
                Lives = w.Player.Lives.Lives, Continues = w.Player.Lives.Continues,
                Xp = Progression.XpFor(true, Crew.FreedBy(w.Mission)?.Name, w.Kills),
                XpBefore = profile.xp, ClimbBefore = profile.rank,
                First = g.Story.FirstDebrief || g.Flags.Debrief,
                OfferSignIn = g.Account.Available && !g.Account.SignedIn,
            };
            int climb = Progression.ClimbFor(true, firstClear);
            result.ClimbAfter = result.ClimbBefore + climb;
            profile.Award(result.XpGained, climb);
            g.Story.Mark(StoryRecord.Debrief);
            Log.Game.Info("Debrief: " + result.XpGained + " XP, " + climb + " stages up the ladder" + (result.RankedUp ? ", a rank up" : ""));
            Now = new DebriefPlayer(result);
            m_View.Show(Now);
        }

        /// <summary>The debrief: taps fill each part and move on; CONTINUE goes on, to the story's next mission or the den.</summary>
        public void Tick(float dt, MenuInput keys, SoldierInput soldier)
        {
            bool tap = m_View.TakeTap() || keys.Start || soldier.FirePressed || soldier.JumpPressed;
            bool leave = m_View.TakeLeave() || (Now.Now == DebriefPlayer.Part.Continue && m_Game.StateTime > 1.2f && (tap || keys.Back));
            if (leave)
            {
                Close();
                m_Game.AfterDebrief();
                return;
            }
            Now.Tick(dt);
            if (tap) Now.Tap();
            m_View.Refresh();
        }

        void Close()
        {
            Now = null;
            m_View.Hide();
        }
    }
}
