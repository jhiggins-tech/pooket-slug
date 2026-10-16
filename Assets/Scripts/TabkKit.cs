using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// A crewmate's tabk, the game's half of their record (<see cref="CrewMember"/>): its name, its paint and the
    /// guns FIRE, SP and SU fire from inside it (<see cref="TabkGuns"/>). The body, driving, fuel and rounds are every
    /// tabk's (<see cref="PlayerTank"/>).
    /// </summary>
    public sealed class TabkKit
    {
        /// <summary>What it says as you climb in.</summary>
        public readonly string Name;
        public readonly Sprite Hull, Barrel;
        /// <summary>The cargo drone that lowers it when it's called in: two frames of rotors.</summary>
        public readonly Sprite[] Drone;
        readonly System.Func<TabkGuns> m_Guns;

        public TabkKit(string name, Sprite hull, Sprite barrel, Sprite[] drone, System.Func<TabkGuns> guns)
        {
            Name = name;
            Hull = hull;
            Barrel = barrel;
            Drone = drone;
            m_Guns = guns;
        }

        /// <summary>A fresh set of its guns, for one tabk.</summary>
        public TabkGuns NewGuns() => m_Guns();

        static TabkKit s_Tones2;

        /// <summary>tones2's tabk: his paint, and ten-1, ten-2 and ten-3 tabk-sized (<see cref="Tones2TabkGuns"/>).</summary>
        public static TabkKit Tones2 => s_Tones2 ??= new TabkKit("TONES2'S TABK!", Tones2Art.TabkHull, Tones2Art.TabkBarrel, Tones2Art.TabkDrone,
            () => new Tones2TabkGuns());

        /// <summary>
        /// <paramref name="soldier"/>'s own tabk. Only tones2's is built: the rest of the crew call his in until theirs
        /// are (<c>docs/design/tanks.md</c>). A crewmate's goes here when it's built.
        /// </summary>
        public static TabkKit For(SoldierDef soldier) => Tones2;
    }

    /// <summary>
    /// A tabk's guns: what FIRE, SP and SU do while someone's driving it, each from its rounds
    /// (<see cref="PlayerTank.Rounds"/>). A crewmate's are their kit, tabk-sized (<c>docs/design/tanks.md</c>).
    /// </summary>
    public abstract class TabkGuns
    {
        /// <summary>Building up a shot that holds it still: it can't drive or hop.</summary>
        public virtual bool Rooted => false;
        /// <summary>In the air from one of its own moves: it keeps the speed the move gave it until it lands.</summary>
        public virtual bool Flying => false;
        /// <summary>How much it shakes now, 0 for not at all.</summary>
        public virtual float Shake => 0f;

        /// <summary>Each tick it's driven, after it's moved and aimed: the buttons, and whatever's firing.</summary>
        public abstract void Tick(PlayerTank tank, float dt, in SoldierInput input);
        /// <summary>It came down from a <see cref="Flying"/> move.</summary>
        public virtual void Land(PlayerTank tank) { }
        /// <summary>Nobody's driving: whatever's firing stops.</summary>
        public abstract void Stop();
    }
}
