using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// The world's side of a co-op session (<see cref="INetLink"/>). Each tick it pulls what has arrived (the
    /// partner's soldier, and on followers the authority's enemies, points and scroll), then pushes our own.
    /// The session also delivers its events here: shots, hits, pickups and damage.
    /// </summary>
    public sealed class NetSync
    {
        readonly World m_World;
        readonly List<NetEnemyState> m_Snapshot = new List<NetEnemyState>();
        readonly HashSet<int> m_SeenIds = new HashSet<int>();
        readonly HashSet<int> m_DeadIds = new HashSet<int>();

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

            if (w.IsAuthority) return;

            w.Scoring.Points = Net.Score;
            if (Net.Victory) w.OnBossDestroyed();
            foreach (Pickup pk in w.Pickups)
                if (!pk.Removed && pk.NetId >= 0 && Net.IsTaken(pk.NetId)) pk.Collect(); // state beat the grant message

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
        }

        /// <summary>Publishes our soldier, and (authority) the shared world state.</summary>
        public void Push()
        {
            World w = m_World;
            Net.SendAvatar(w.Player.Pos, w.Player.Vel, w.Player.Pose.Pack(), w.Player.Hp);
            if (!w.IsAuthority) return;
            Net.CamX = w.View.X;
            Net.Score = w.Scoring.Points;
            Net.Victory = w.Victory;
            Net.NextSpawn = w.Spawner.Next;
            Net.WriteEnemies(w.Enemies);
        }

        // network events (delivered by the session)

        public void OnRemoteShot(int weaponId, Vector2 origin, Vector2 vel)
        {
            WeaponDef weapon = Content.Weapon(weaponId);
            if (weapon == null) return; // a weapon this build doesn't have
            m_World.SpawnProjectile(weapon.Projectile, origin, vel, false, cosmetic: true);
            Sfx.PlayShot(weapon, 0.35f);
        }

        public void OnEnemyShot(int weaponId, Vector2 origin, Vector2 vel)
        {
            WeaponDef weapon = Content.Weapon(weaponId);
            if (weapon != null) m_World.Combat.SpawnEnemyShot(weapon, origin, vel);
        }

        public void OnHitReported(int enemyId, int damage)
        {
            if (!m_World.IsAuthority) return;
            Enemy e = m_World.FindEnemy(enemyId);
            if (e != null && e.Shootable) e.Hurt(damage);
        }

        public void OnPickupGranted(int spawnIndex, bool mine)
        {
            World w = m_World;
            Pickup pk = null;
            foreach (Pickup p in w.Pickups)
                if (p.NetId == spawnIndex && !p.Removed) pk = p;
            if (mine)
            {
                if (pk != null) pk.ApplyTo(w.Player);
                else if (spawnIndex >= 0 && spawnIndex < w.Level.Spawns.Count) Pickup.Give(Spawner.ToPickup(w.Level.Spawns[spawnIndex].Kind), w.Player);
            }
            pk?.Collect();
        }

        public void OnLocalHurt(int damage) => m_World.Player.Hurt(damage);
    }
}
