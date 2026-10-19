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
        /// <summary>Whose it is, as the bay's card has it (its id is what a partner's screen is told).</summary>
        public readonly TabkCard Card;
        /// <summary>What it says as you climb in.</summary>
        public readonly string Name;
        public readonly Sprite Hull, Barrel;
        /// <summary>The hull's second frame while a shot charges (kie's reels turning), or null for none.</summary>
        public readonly Sprite HullSpun;
        /// <summary>It whole, gun and all, parked (in the tabk bay, <see cref="BayView"/>).</summary>
        public readonly Sprite Parked;
        /// <summary>The cargo drone that lowers it when it's called in: two frames of rotors.</summary>
        public readonly Sprite[] Drone;
        /// <summary>Where its gun turns, in art pixels from its feet with the art facing left.</summary>
        public readonly Vector2 Pivot;
        /// <summary>How far from there its shots leave the gun (world units).</summary>
        public readonly float GunLength;
        readonly System.Func<TabkGuns> m_Guns;

        /// <summary>The tabk's turret, where its gun turns (<see cref="Art.TankBarrel"/>), unless a crewmate's says otherwise.</summary>
        public static readonly Vector2 TurretPivot = new Vector2(4.5f, 17.5f);

        public TabkKit(TabkCard card, Sprite hull, Sprite barrel, Sprite parked, Sprite[] drone, System.Func<TabkGuns> guns,
            Sprite hullSpun = null, Vector2? pivot = null, float gunPixels = 0f)
        {
            Card = card;
            Name = card.Name + "!";
            Hull = hull;
            HullSpun = hullSpun;
            Barrel = barrel;
            Parked = parked;
            Drone = drone;
            Pivot = pivot ?? TurretPivot;
            GunLength = gunPixels > 0f ? gunPixels / Art.PPU : Content.PlayerTank.BarrelLength;
            m_Guns = guns;
        }

        /// <summary>What the HUD calls it while you drive it.</summary>
        public string Label => Name.TrimEnd('!');

        /// <summary>A fresh set of its guns, for one tabk.</summary>
        public TabkGuns NewGuns() => m_Guns();

        /// <summary>Where the gun turns, from the tabk's feet, facing <paramref name="facing"/> (the art faces left, so it's mirrored).</summary>
        public Vector2 PivotLocal(int facing) => new Vector2(-facing * Pivot.x / Art.PPU, Pivot.y / Art.PPU);

        static TabkKit s_Tones2, s_Kie, s_Kcaj;

        /// <summary>tones2's tabk: his paint, and ten-1, ten-2 and ten-3 tabk-sized (<see cref="Tones2TabkGuns"/>).</summary>
        public static TabkKit Tones2 => s_Tones2 ??= new TabkKit(TabkCards.Tones2, Tones2Art.TabkHull, Tones2Art.TabkBarrel, Tones2Art.TabkParked,
            Tones2Art.TabkDrone, () => new Tones2TabkGuns());

        /// <summary>
        /// kie's tabk, THE PROJECTIONIST (<see cref="KieTabkArt"/>): Weasel Pop, Trollogram and Steal tabk-sized
        /// (<see cref="KieTabkGuns"/>), from a lens that turns at the projector's front.
        /// </summary>
        public static TabkKit Kie => s_Kie ??= new TabkKit(TabkCards.Kie, KieTabkArt.Hull, KieTabkArt.Lens, KieTabkArt.Parked, Tones2Art.TabkDrone,
            () => new KieTabkGuns(), KieTabkArt.HullSpun, KieTabkArt.LensPivot, KieTabkArt.LensLength);

        /// <summary>
        /// kcaj's tabk, won by beating Mission two (<see cref="TabkCards"/>): the tabk in his paint. For now it's only parked
        /// in the bay: missions drop tones2's until his is built (the owner's call, 19 Oct, while tabk select was built), so
        /// its guns are tones2's.
        /// </summary>
        public static TabkKit Kcaj => s_Kcaj ??= Painted(TabkCards.Kcaj);

        static TabkKit Painted(TabkCard card)
        {
            var paint = new System.Collections.Generic.Dictionary<char, uint> { { 't', card.Paint }, { 'T', card.Shade } };
            string[] whole = ArtData.Tank, hull = (string[])ArtData.Tank.Clone();
            for (int r = 4; r <= 5; r++) hull[r] = new string('.', 22) + hull[r].Substring(22); // the gun is drawn on its own
            var feet = new Vector2(0.5f, 0f);
            return new TabkKit(card, Art.FromRows(hull, feet, paint), Art.TankBarrel, Art.FromRows(whole, feet, paint),
                Tones2Art.TabkDrone, () => new Tones2TabkGuns());
        }

        /// <summary>The tabk a card in the bay is for (tones2's for none).</summary>
        public static TabkKit Of(TabkCard card) => card == TabkCards.Kie ? Kie : card == TabkCards.Kcaj ? Kcaj : Tones2;

        /// <summary>
        /// The tabk a mission drops with <paramref name="loaded"/> picked in the bay: it, once it's built, else tones2's
        /// (<see cref="TabkCards.Dropped"/>).
        /// </summary>
        public static TabkKit Dropped(TabkCard loaded) => Of(TabkCards.Dropped(loaded));
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
        /// <summary>Charging a shot, so its hull's second frame shows (kie's reels spin, <see cref="TabkKit.HullSpun"/>).</summary>
        public virtual bool Spinning => false;

        /// <summary>Each tick it's driven, after it's moved and aimed: the buttons, and whatever's firing.</summary>
        public abstract void Tick(PlayerTank tank, float dt, in SoldierInput input);
        /// <summary>It came down from a <see cref="Flying"/> move.</summary>
        public virtual void Land(PlayerTank tank) { }
        /// <summary>Nobody's driving: whatever's firing stops.</summary>
        public abstract void Stop();
    }
}
