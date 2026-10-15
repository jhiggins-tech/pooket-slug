#if FUSION2
using Fusion;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>The boss tank's state (see <see cref="NetBossState"/>).</summary>
    public struct NetBoss : INetworkStruct
    {
        public byte Phase; // 0 until the authority writes one
        public float Angle, Charge, Power;
        public Vector2 Landing;
        public byte Shots;
        public byte KieState, KieDrops;
        public float KieProgress;
    }

    /// <summary>
    /// The boss's network object: its turret, charge and shots, for the followers' tank to swing and fire with.
    /// Its health, position and death go in the enemy snapshot like any enemy's. A master-client object, like
    /// <see cref="WorldNet"/>, so the client running the shared world always writes it.
    /// </summary>
    public sealed class BossNet : NetworkBehaviour
    {
        [Networked] public NetBoss State { get; set; }

        public override void Spawned() => NetSession.Instance?.Replication.OnBossSpawned(this);

        public override void Despawned(NetworkRunner runner, bool hasState) => NetSession.Instance?.Replication.OnBossDespawned(this);
    }
}
#endif
