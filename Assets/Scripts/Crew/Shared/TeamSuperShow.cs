using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// A team super playing out (<c>docs/design/team-supers.md</c>; the numbers are <see cref="TeamSuperSpec"/>). The
    /// action freezes while the split card slams in (drawn by the HUD from <see cref="CardTime"/>), then the combo plays
    /// across the whole screen (its <see cref="TeamAct"/>) and every enemy on it dies; a boss takes a big hit instead.
    /// Every screen in co-op plays the show; only the world's authority deals the damage, and the snapshot carries the
    /// deaths to the partner.
    /// </summary>
    public sealed class TeamSuperShow
    {
        public readonly TeamCombo Combo;
        /// <summary>The soldier who pressed TEAM (on the card's left) and their partner.</summary>
        public readonly SoldierDef Fired, Partner;

        readonly World m_World;
        readonly Transform m_Root;
        readonly TeamAct m_Act;
        float m_Time;
        bool m_Built, m_Killed;
        TeamSuperSpec.Stage m_Stage = TeamSuperSpec.Stage.Card;

        static TeamSuperSpec Spec => TeamSupers.Spec;

        public TeamSuperShow(World world, TeamCombo combo, SoldierDef fired, SoldierDef partner, int facing)
        {
            m_World = world;
            Combo = combo;
            Fired = fired;
            Partner = partner;
            m_Root = new GameObject("Team super").transform;
            m_Root.SetParent(world.Root, false);
            m_Act = TeamAct.For(combo, world, m_Root, facing >= 0 ? 1 : -1);
            Sfx.Play(Sfx.Charge, 1f);
            Sfx.Say("team-super", 0.1f);
            world.View.Shake(0.25f);
            Log.Game.Info("Team super: " + combo.Name);
        }

        /// <summary>The action is frozen for the split card.</summary>
        public bool Freezing => m_Time < Spec.Card;
        /// <summary>Seconds since TEAM was pressed: the HUD draws the card from it.</summary>
        public float CardTime => m_Time;
        public bool Over => m_Time >= Spec.Length;

        public void Tick(float dt)
        {
            m_Time += dt;
            TeamSuperSpec.Stage stage = Spec.At(m_Time);
            if (stage != TeamSuperSpec.Stage.Card && !m_Built)
            {
                m_Built = true;
                m_Act.Build();
            }
            if (stage != m_Stage)
            {
                m_Stage = stage;
                m_Act.Begin(stage);
            }
            m_Act.Tick(dt, m_Time);
            if (m_Time >= Spec.KillAt) Kill();
        }

        /// <summary>Every enemy on screen dies (bosses take a big hit), each in the combo's splash.</summary>
        void Kill()
        {
            if (m_Killed) return;
            m_Killed = true;
            World w = m_World;
            float left = w.View.Left - 0.5f, right = w.View.Right + 0.5f;
            Sfx.Play(Sfx.BigExplosion, 1f);
            w.View.Shake(0.5f);
            m_Act.Kill();
            for (int i = 0; i < w.Enemies.Count; i++)
            {
                Enemy e = w.Enemies[i];
                if (!e.Shootable || e.Pos.x < left || e.Pos.x > right) continue;
                m_Act.Splash(e, i);
                w.SpawnEffect(Art.Explosion, 30f, e.Center, 0.8f);
                if (!w.IsAuthority) continue; // the authority's damage comes back in the snapshot
                if (e is BossEnemy)
                {
                    int damage = Spec.BossDamage(e.Hp, e.MaxHp);
                    if (damage > 0) e.Hurt(damage);
                    w.Scoring.Popup("TEAM SUPER!", e.Center + Vector2.up * 1.6f, m_Act.Highlight);
                }
                else e.Hurt(e.Hp);
            }
            // the goons' shots in the air go too
            foreach (Projectile p in w.Projectiles)
                if (p.Hostile && !p.Removed && p.Pos.x >= left && p.Pos.x <= right) p.Remove();
        }

        public void Render()
        {
            if (m_Built) m_Act.Render(m_Time);
        }

        public void Destroy() => Object.Destroy(m_Root.gameObject);
    }
}
