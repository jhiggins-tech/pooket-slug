using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// The world's side of a co-op session (<see cref="INetLink"/>). Each tick it pulls what has arrived (the
    /// partner's soldier, and on followers the authority's enemies, the boss's turret, pickups, points and scroll), then pushes our own.
    /// The session also delivers its events here (<see cref="INetEvents"/>): shots, hits, pickups and damage.
    /// </summary>
    public sealed class NetSync : INetEvents
    {
        readonly World m_World;
        readonly List<NetEnemyState> m_Snapshot = new List<NetEnemyState>();
        readonly List<NetEnemyState> m_Outgoing = new List<NetEnemyState>();
        readonly List<NetPickupState> m_PickupSnapshot = new List<NetPickupState>();
        readonly List<NetPickupState> m_OutgoingPickups = new List<NetPickupState>();
        readonly HashSet<int> m_SeenPickups = new HashSet<int>();  // in the latest snapshot
        readonly HashSet<int> m_GonePickups = new HashSet<int>();  // collected here: a late snapshot mustn't bring them back
        readonly HashSet<int> m_SeenIds = new HashSet<int>();
        readonly HashSet<int> m_DeadIds = new HashSet<int>();
        readonly HashSet<int> m_PartnerScored = new HashSet<int>();
        bool m_BossFollowed;

        public NetSync(World world) => m_World = world;

        INetLink Net => m_World.Net;

        /// <summary>Applies what arrived from the network: remote soldiers and (followers) the authority's world.</summary>
        public void Pull()
        {
            World w = m_World;
            List<RemotePlayer> remotes = w.Remotes;
            IReadOnlyList<IAvatarLink> links = Net.RemoteAvatars;
            for (int i = remotes.Count - 1; i >= 0; i--)
            {
                bool present = false;
                for (int j = 0; j < links.Count; j++) present |= ReferenceEquals(links[j], remotes[i].Link);
                if (present) continue;
                remotes[i].Destroy();
                remotes.RemoveAt(i);
            }
            for (int j = 0; j < links.Count; j++)
            {
                bool known = false;
                foreach (RemotePlayer r in remotes) known |= ReferenceEquals(r.Link, links[j]);
                if (!known) remotes.Add(new RemotePlayer(w, links[j]));
            }
            w.Player.Skin = Net.LocalSkin;
            foreach (RemotePlayer r in remotes)
                if (r.Link.Score > 0 && m_PartnerScored.Add(r.Link.PlayerId)) Log.Net.Info($"Player {r.Link.PlayerId}'s own score: {r.Link.Score}"); // the drop-in round looks for it

            if (w.IsAuthority) return;

            w.Scoring.Points = Net.Score;
            if (Net.Victory) w.OnBossDestroyed();
            PullPickups();

            Net.ReadEnemies(m_Snapshot);
            m_SeenIds.Clear();
            foreach (NetEnemyState s in m_Snapshot)
            {
                m_SeenIds.Add(s.Id);
                Enemy e = w.FindEnemy(s.Id);
                if (e == null)
                {
                    if ((s.Flags & NetEnemyState.Dying) != 0 || m_DeadIds.Contains(s.Id) || s.Def == null) continue;
                    e = w.Spawner.CreateEnemy(s.Def, s.Pos, s.Id);
                    if (e == null) continue;
                    w.Enemies.Add(e);
                }
                e.ApplySnapshot(s);
                if (e.Dying) m_DeadIds.Add(s.Id);
            }
            // gone from the authority's world (left behind or finished dying)
            foreach (Enemy e in w.Enemies)
                if (!e.Dying && !m_SeenIds.Contains(e.NetId)) e.Remove();

            if (w.Boss != null && !w.Boss.Removed && Net.ReadBoss(out NetBossState bs))
            {
                BossEnemy boss = w.Boss;
                if (!m_BossFollowed) Log.Net.Info(boss is TankBoss ? "Following the boss tank's turret from the authority" : "Following the boss from the authority: " + boss.DisplayName); // the co-op test looks for the first
                m_BossFollowed = true;
                boss.ApplyNet(bs);
            }
        }

        /// <summary>Followers: make the authority's pickups, and collect the ones it no longer lists.</summary>
        void PullPickups()
        {
            World w = m_World;
            if (!Net.ReadPickups(m_PickupSnapshot)) return;
            m_SeenPickups.Clear();
            foreach (NetPickupState s in m_PickupSnapshot)
            {
                m_SeenPickups.Add(s.Id);
                if (s.Def == null || m_GonePickups.Contains(s.Id) || FindPickup(s.Id) != null) continue;
                w.Pickups.Add(new Pickup(w, s.Def, s.Pos) { NetId = s.Id });
            }
            // gone from the authority's world: someone collected it (the state beat the grant message)
            foreach (Pickup pk in w.Pickups)
            {
                if (pk.Removed || m_SeenPickups.Contains(pk.NetId)) continue;
                pk.Collect();
                m_GonePickups.Add(pk.NetId);
            }
        }

        Pickup FindPickup(int id)
        {
            foreach (Pickup p in m_World.Pickups)
                if (p.NetId == id && !p.Removed) return p;
            return null;
        }

        /// <summary>Publishes our soldier, and (authority) the shared world state.</summary>
        public void Push()
        {
            World w = m_World;
            // our tank goes with our soldier: what it's doing, and how many of ours have blown up (even with none out now)
            PlayerTank tank = w.Tank != null && !w.Tank.Removed ? w.Tank : null;
            TankPose tankPose = tank != null ? tank.Pose : default;
            tankPose.Blown = w.TanksBlown;
            // with the pose, whether we're here: our page's focus, and a beat that only changes while the game runs
            AvatarPose pose = w.Player.Pose;
            pose.Away = !Application.isFocused;
            pose.Beat = PartnerBeacon.BeatAt(Time.unscaledTime);
            Net.SendAvatar(w.Player.Def, w.Player.Pos, w.Player.Vel, pose.Pack(), w.Player.Hp, tankPose.Pack(),
                tank != null ? tank.Pos : w.LastTankPos);
            Net.SendScore(w.OwnPoints);
            string wire = WireMeter.Out.Tick(Time.unscaledDeltaTime); // spike coop-weapons: what we send, every half minute
            if (wire != null) Log.Net.Info(wire);
            if (!w.IsAuthority) return;
            Net.Mission = w.Mission.NetId;
            Net.CamX = w.View.X;
            Net.Score = w.Scoring.Points;
            Net.Victory = w.Victory;
            Net.NextSpawn = w.Spawner.Next;
            Net.NextNetId = w.Spawner.Ids.Next;
            m_Outgoing.Clear();
            foreach (Enemy e in w.Enemies)
                if (!e.Removed && e.NetId >= 0)
                    m_Outgoing.Add(new NetEnemyState { Id = e.NetId, Def = e.Def, Flags = e.SnapshotFlags, Hp = e.Hp, Pos = e.Pos });
            Net.WriteEnemies(m_Outgoing);
            m_OutgoingPickups.Clear();
            foreach (Pickup pk in w.Pickups)
                if (!pk.Removed && !pk.Granted && pk.NetId >= 0)
                    m_OutgoingPickups.Add(new NetPickupState { Id = pk.NetId, Def = pk.Def, Pos = pk.Pos });
            Net.WritePickups(m_OutgoingPickups);
            if (w.Boss != null && !w.Boss.Removed) Net.WriteBoss(w.Boss.NetState);
        }

        // ------------------------------------------------------------------ INetEvents (delivered by the session)

        public int Round => m_World.Round;

        public void OnShot(in ShotEvent shot)
        {
            if (shot.Weapon.Projectile == null)
            {
                // kie's weasels and holograms (KieKit)
                if (shot.Weapon == Content.WeaselPop) KieKit.CopyWeasel(m_World, shot);
                else if (shot.Weapon == Content.Trollogram) KieKit.OnTrollEvent(m_World, shot);
                else if (shot.Weapon == Content.TeamSuper) m_World.OnPartnerTeam(shot);
                // tones2's rocket jump: the same mud on our screen, harmless (it poisons goons on theirs)
                if (shot.Weapon == Content.RocketJump)
                {
                    m_World.Liquids.SpillMud(shot.Origin, Tones2Tuning.Rocket, cosmetic: true);
                    Tones2Sprays.Exhaust(m_World, shot.Origin);
                    Sfx.Play(Sfx.RocketLaunch, 0.35f);
                }
                return; // the jet and the spew come with their pose (Tones2Copy)
            }
            m_World.SpawnProjectile(shot.Weapon.Projectile, shot.Origin, shot.Velocity, false, cosmetic: true);
            Sfx.PlayShot(shot.Weapon, 0.35f);
        }

        public void OnEnemyShot(in ShotEvent shot) => m_World.Combat.SpawnEnemyShot(shot.Weapon, shot.Origin, shot.Velocity);

        public void OnHit(in HitReport hit)
        {
            if (!m_World.IsAuthority) return;
            Enemy e = m_World.FindEnemy(hit.EnemyId);
            if (e != null && e.Shootable) e.Hurt(hit.Damage);
        }

        public void OnPickupClaimed(int id, int playerId)
        {
            if (!m_World.IsAuthority) return;
            Pickup pk = FindPickup(id);
            if (pk == null || pk.Granted) return; // gone, or someone got there first
            pk.Granted = true;
            if (pk.Def.Effect == PickupEffect.Life) playerId = m_World.OneUpFor(playerId);
            Log.Net.Info($"Pickup {id} ({pk.Def.Name}) goes to player {playerId}");
            Net.GrantPickup(id, pk.Def, playerId);
        }

        public void OnPickupGranted(in PickupGrant grant)
        {
            Pickup pk = FindPickup(grant.Id);
            if (grant.Mine)
            {
                if (pk != null) pk.ApplyTo(m_World.Player);
                else if (grant.Def != null) Pickup.Give(grant.Def, m_World.Player); // never saw it ourselves
            }
            pk?.Collect();
            m_GonePickups.Add(grant.Id);
        }

        public void OnHurt(int damage) => m_World.Player.Hurt(damage);
    }
}
