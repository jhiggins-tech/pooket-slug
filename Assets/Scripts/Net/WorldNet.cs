#if FUSION2
using Fusion;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>One enemy in the authority's snapshot (see <see cref="NetEnemyState"/>).</summary>
    public struct NetEnemy : INetworkStruct
    {
        public short Ref; // the enemy's NetIds id + 1; 0 marks an empty slot
        public byte Kind;  // EnemyDef id
        public byte Flags;
        public short Hp;
        public Vector2 Pos;
    }

    /// <summary>
    /// The shared world. It is a master-client object: the room's master client always has state authority,
    /// and if that client leaves, Photon hands it to the next one, whose game then becomes the authority.
    /// </summary>
    public sealed class WorldNet : NetworkBehaviour
    {
        public const int MaxEnemies = 24;

        [Networked] public int Round { get; set; }
        [Networked] public float CamX { get; set; }
        [Networked] public int Score { get; set; }
        [Networked] public NetworkBool Victory { get; set; }
        [Networked] public int NextSpawn { get; set; }
        [Networked] public int NextNetId { get; set; } // the next id for something spawned mid-game (NetIds)
        [Networked] public long Taken { get; set; } // one bit per Level.Spawns index: pickups already collected
        [Networked, Capacity(MaxEnemies)] public NetworkArray<NetEnemy> Enemies => default;

        public override void Spawned() => NetSession.Instance?.OnWorldSpawned(this);

        public override void Despawned(NetworkRunner runner, bool hasState) => NetSession.Instance?.OnWorldDespawned(this);

        [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies, InvokeLocal = false)]
        public void RPC_EnemyShot(byte weapon, Vector2 origin, Vector2 vel)
        {
            WeaponDef def = Content.Weapon(weapon);
            if (def != null) NetSession.Instance?.Events?.OnEnemyShot(new ShotEvent(def, origin, vel));
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_Hit(short enemyId, byte damage) => NetSession.Instance?.Events?.OnHit(new HitReport(enemyId, damage));

        /// <summary>Both soldiers may touch a crate in the same moment: the authority gives it to the first claim.</summary>
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_Claim(short spawnIndex, RpcInfo info = default)
        {
            if (spawnIndex < 0 || spawnIndex >= 64) return;
            long bit = 1L << spawnIndex;
            if ((Taken & bit) != 0) return;
            Taken |= bit;
            RPC_Granted(spawnIndex, info.IsInvokeLocal ? Runner.LocalPlayer : info.Source);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_Granted(short spawnIndex, PlayerRef player) =>
            NetSession.Instance?.Events?.OnPickupGranted(new PickupGrant(spawnIndex, player == Runner.LocalPlayer));

        /// <summary>Starts a won mission again for everyone (each client rebuilds its world when Round changes).</summary>
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_RequestRestart()
        {
            if (!Victory) return; // both players pressed Enter: only the first one counts
            Round++;
            CamX = 0f;
            Score = 0;
            Victory = false;
            NextSpawn = 0;
            NextNetId = 0;
            Taken = 0;
            for (int i = 0; i < Enemies.Length; i++) Enemies.Set(i, default);
        }
    }
}
#endif
