using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// A crewmate's attacks on a boss tabk (<see cref="TankBoss"/>): what its charged shots do in place of a shell, and
    /// what of that rides in the tabk's network object. kie's is <see cref="KieBoss"/>, kcaj's <see cref="KcajBoss"/>.
    /// </summary>
    public interface IBossKit
    {
        /// <summary>What the HUD and the intro call the tabk.</summary>
        string Name { get; }
        /// <summary>Authority: hologram copies standing with it, which fire its shells with it.</summary>
        IReadOnlyList<TankBoss> Copies { get; }
        /// <summary>It's wearing its hologram look among its copies.</summary>
        bool Disguised { get; }
        /// <summary>Authority: the next charged shot comes down somewhere, so the landing marker means something.</summary>
        bool NextLands { get; }
        /// <summary>Extra seconds the tabk rests after the attack just made.</summary>
        float Wait { get; }

        /// <summary>Authority: the tabk's charged shot, the crewmate's attack in its place, or false for a plain shell.</summary>
        /// <param name="thrown">Something came out of the barrel, so the tabk kicks as if it fired.</param>
        bool Fire(Vector2 muzzle, float angle, out bool thrown);
        void SetDisguised(bool on);
        /// <summary>Each tick of the tabk once it's awake (on a follower, each tick it's alive).</summary>
        void Tick(float dt);
        /// <summary>Authority: the real tabk took <paramref name="damage"/>.</summary>
        void OnHurt(int damage);
        /// <summary>Authority: the tabk is destroyed.</summary>
        void OnDie();
        void Render();

        /// <summary>Authority: its state for the network object (<see cref="NetBossState.KieState"/>), with whether the landing marker shows.</summary>
        int NetState(bool markerShown);
        float Progress { get; }
        int Drops { get; }
        /// <summary>Every screen: follow the authority's state.</summary>
        void ApplyNet(int state, float progress, int drops);
        /// <summary>Follower: whether the authority's tabk shows its landing marker.</summary>
        bool ShowsMarker(int netState);
    }
}
