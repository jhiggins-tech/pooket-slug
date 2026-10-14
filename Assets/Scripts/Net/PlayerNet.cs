#if FUSION2
using Fusion;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// One per player in the session, owned (state authority) by that player's client. It replicates the
    /// soldier as the game simulates it locally, and carries the RPCs aimed at that soldier.
    /// </summary>
    public sealed class PlayerNet : NetworkBehaviour, IAvatarLink
    {
        [Networked] public Vector2 NetPos { get; set; }
        [Networked] public Vector2 NetVel { get; set; }
        [Networked] public int NetPose { get; set; }  // AvatarPose.Pack()
        [Networked] public int NetHp { get; set; }
        [Networked] public int NetSkin { get; set; }
        [Networked] public int NetRound { get; set; } // World.Round + 1 once the soldier is in play; 0 = still in the lobby

        bool Live => Object != null && Object.IsValid;

        public Vector2 Pos => Live ? NetPos : default;
        public Vector2 Vel => Live ? NetVel : default;
        public int PoseFlags => Live ? NetPose : 0;
        public int Hp => Live ? NetHp : 0;
        public int Skin => Live ? NetSkin : 0;
        public int PlayingRound => Live ? NetRound - 1 : -1;
        public int PlayerId => Live ? Object.StateAuthority.PlayerId : -1;

        public override void Spawned() => NetSession.Instance?.OnPlayerSpawned(this);

        public override void Despawned(NetworkRunner runner, bool hasState) => NetSession.Instance?.OnPlayerDespawned(this);

        /// <summary>The authority's melee/contact damage, applied by the client that owns this soldier.</summary>
        public void Hurt(int damage)
        {
            if (Live) RPC_Hurt((byte)Mathf.Clamp(damage, 0, 255));
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_Hurt(byte damage) => NetSession.Instance?.Events?.OnHurt(damage);

        /// <summary>Replays one of our shots on the other clients (as a harmless copy).</summary>
        [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies, InvokeLocal = false)]
        public void RPC_Shot(byte weapon, Vector2 origin, Vector2 vel)
        {
            WeaponDef def = Content.Weapon(weapon);
            if (def != null) NetSession.Instance?.Events?.OnShot(new ShotEvent(def, origin, vel)); // else a weapon this build doesn't have
        }
    }
}
#endif
