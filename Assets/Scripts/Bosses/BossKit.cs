using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// A crewmate's attacks on a boss tabk (<see cref="TankBoss"/>): what its charged shots do in place of a shell, and
    /// what of that rides in the tabk's network object. kie's is <see cref="KieBoss"/>, kcaj's <see cref="KcajBoss"/>.
    /// </summary>
    public abstract class BossKit
    {
        static readonly IReadOnlyList<TankBoss> NoCopies = new List<TankBoss>();

        /// <summary>What the HUD and the intro call the tabk.</summary>
        public abstract string Name { get; }
        /// <summary>Authority: Trollogram copies standing with it, which fire its shells with it (none unless it makes them).</summary>
        public virtual IReadOnlyList<TankBoss> Copies => NoCopies;
        /// <summary>It's wearing its Trollogram look among its copies.</summary>
        public virtual bool Disguised => false;
        /// <summary>Authority: the next charged shot comes down somewhere, so the landing marker means something.</summary>
        public abstract bool NextLands { get; }
        /// <summary>Extra seconds the tabk rests after the attack just made.</summary>
        public float Wait { get; protected set; }

        /// <summary>Authority: the tabk's charged shot, the crewmate's attack in its place, or false for a plain shell.</summary>
        /// <param name="thrown">Something came out of the barrel, so the tabk kicks as if it fired.</param>
        public abstract bool Fire(Vector2 muzzle, float angle, out bool thrown);
        public virtual void SetDisguised(bool on) { }
        /// <summary>Each tick of the tabk once it's awake (on a follower, each tick it's alive).</summary>
        public abstract void Tick(float dt);
        /// <summary>Authority: the real tabk took <paramref name="damage"/>.</summary>
        public virtual void OnHurt(int damage) { }
        /// <summary>Authority: the tabk is destroyed.</summary>
        public abstract void OnDie();
        public abstract void Render();

        /// <summary>Authority: its state for the network object (<see cref="NetBossState.KieState"/>), with whether the landing marker shows.</summary>
        public abstract int NetState(bool markerShown);
        public abstract float Progress { get; }
        public abstract int Drops { get; }
        /// <summary>Every screen: follow the authority's state.</summary>
        public abstract void ApplyNet(int state, float progress, int drops);
        /// <summary>Follower: whether the authority's tabk shows its landing marker (its state carries <see cref="KieTankSpec.NoMarker"/> when not).</summary>
        public bool ShowsMarker(int netState) => (netState & KieTankSpec.NoMarker) == 0;
    }

    /// <summary>Which crewmate's attacks a boss tabk has, by whose it is (<see cref="BossTabk"/>): a new boss tabk is its row here.</summary>
    public static class BossKits
    {
        public static BossKit For(BossTabk tabk, World world, TankBoss boss)
        {
            switch (tabk)
            {
                case BossTabk.Kcaj: return new KcajBoss(world, boss);
                default: return new KieBoss(world, boss);
            }
        }
    }
}
