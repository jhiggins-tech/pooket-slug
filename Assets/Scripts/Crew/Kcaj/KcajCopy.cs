// A partner's kcaj on our screen (his beam and pills, or his tabk's), drawn from what his phone sends.
using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// A partner's kcaj on our screen: his beam, drawn from his gun to what it's stuck to, and his pills, rained from
    /// the same seed. Both are harmless here; his screen does the damage. The same plays a partner's SOFT SERVE
    /// (<paramref name="tabk"/>), whoever drives it: its beam from its nozzle (<see cref="RemotePlayer.TabkMuzzle"/>).
    /// </summary>
    public sealed class KcajCopy : KitCopy
    {
        readonly SpriteRenderer m_Beam;
        readonly PillRain m_Rain;
        readonly PillView m_Pills;
        readonly KcajPills.Ground m_Ground;
        readonly HyperfixateSpec m_Spec;
        readonly bool m_Tabk;
        Enemy m_Target;
        float m_Held;
        bool m_Raining;

        public KcajCopy(RemotePlayer remote, bool tabk = false) : base(remote)
        {
            m_Tabk = tabk;
            m_Spec = tabk ? KcajTuning.Tabk.Beam : KcajTuning.Beam;
            m_Beam = Art.Renderer(remote.Go.transform, "Hyperfixate", Art.Pixel, SortOrder.Effects + 2, shown: false);
            m_Rain = new PillRain(tabk ? KcajTuning.Tabk.Pills : KcajTuning.Pills);
            m_Pills = new PillView(remote.Go.transform, m_Rain.Capacity, 1f);
            m_Ground = new KcajPills.Ground(remote.World);
        }

        public override void Tick(float dt, in AvatarPose pose, Vector2 vel)
        {
            HyperfixateSpec spec = m_Spec;
            if (m_Target != null)
            {
                m_Held += dt;
                if (pose.Dead || m_Target.Removed || !m_Target.Shootable || m_Held > spec.MaxHold + 0.5f) m_Target = null; // in case the end got lost
                else if (Rng.Fx.Value < 0.3f) Remote.World.HitFeel.Fizz(m_Target.Center);
            }
            if (!m_Raining) return;
            m_Rain.Step(dt, m_Ground, m_Ground.OnGoon);
            for (int p = 0; p < m_Rain.PopCount; p++) KcajArt.Pop(Remote.World, m_Rain.Pops[p], 3);
            if (m_Rain.PopCount > 0) Sfx.Play(Sfx.PillPopAt(), CrewKit.PartnerVolume * 0.6f);
            if (m_Rain.Done) m_Raining = false;
        }

        public override void Render(in AvatarPose pose)
        {
            bool locked = m_Target != null && (!m_Tabk || Remote.Driving);
            m_Beam.enabled = locked;
            if (locked)
            {
                Vector2 local = PlayerVisual.MuzzleLocal(pose.AimY, pose.Crouching, pose.Diagonal);
                Vector2 from = m_Tabk ? Remote.TabkMuzzle : Remote.Pos + new Vector2(local.x * pose.Facing, local.y);
                KcajArt.DrawBeam(m_Beam, from, m_Target.Center, Mathf.Clamp01(m_Held / m_Spec.RampTime));
            }
            m_Pills.Render(m_Rain, m_Raining);
        }

        public override void OnMove(in KitEvent move)
        {
            World w = Remote.World;
            switch (move.Move)
            {
                case KitMove.Fixate:
                    bool holds = move.Data.y > 0.5f;
                    m_Target = holds ? w.FindEnemy(Mathf.RoundToInt(move.Data.x)) : null;
                    m_Held = 0f;
                    if (holds) Sfx.Play(Sfx.BeamLock, CrewKit.PartnerVolume * 0.7f);
                    break;
                case KitMove.Pills:
                    m_Rain.Start((uint)Mathf.RoundToInt(move.Data.y), move.At.x, move.Data.x, move.At.y);
                    m_Raining = true;
                    Sfx.Play(Sfx.PillsRattle, CrewKit.PartnerVolume);
                    break;
            }
        }
    }
}
