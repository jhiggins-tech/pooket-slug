using UnityEngine;

namespace PooketSlug
{
    /// <summary>How a projectile turns as it flies.</summary>
    public enum ProjectileSpin { AlongFlight, Tumble, None }

    /// <summary>One kind of projectile: how it flies, and what it does when it lands.</summary>
    public sealed class ProjectileDef
    {
        public readonly byte Id;
        public readonly string Name;
        public readonly float Radius;
        /// <summary>Dealt to what it touches, if it has <see cref="ContactDamage"/>, or else by its blast.</summary>
        public readonly int Damage;
        /// <summary>Seconds before it bursts by itself.</summary>
        public readonly float Life;
        public readonly float Gravity;
        /// <summary>How many times it bounces off the floor before it bursts.</summary>
        public readonly int Bounces;
        /// <summary>Hurts what it touches. Without it, a projectile only hurts with its blast.</summary>
        public readonly bool ContactDamage;
        /// <summary>The blast's reach. 0 means no blast: it just sparks.</summary>
        public readonly float BlastRadius;
        /// <summary>How big the blast looks and shakes.</summary>
        public readonly float BlastSize;
        public readonly ProjectileSpin Spin;

        public ProjectileDef(byte id, string name, float radius, int damage, float life, float gravity = 0f, int bounces = 0,
            bool contactDamage = false, float blastRadius = 0f, float blastSize = 0f, ProjectileSpin spin = ProjectileSpin.AlongFlight)
        {
            Id = id;
            Name = name;
            Radius = radius;
            Damage = damage;
            Life = life;
            Gravity = gravity;
            Bounces = bounces;
            ContactDamage = contactDamage;
            BlastRadius = blastRadius;
            BlastSize = blastSize;
            Spin = spin;
        }

        public bool Explodes => BlastRadius > 0f;

        public override string ToString() => Name;
    }

    /// <summary>
    /// How a weapon fires. The crew's kits will add beams, cones, homing shots and sprays
    /// (<c>docs/design/player.md</c>).
    /// </summary>
    public enum WeaponBehaviour
    {
        /// <summary>Straight along the aim, at <see cref="WeaponDef.Speed"/>.</summary>
        Bullet,
        /// <summary>Thrown or dropped, then falls: <see cref="WeaponDef.Throw"/> plus some of the thrower's own speed.</summary>
        Lob,
        /// <summary>A stream of droplets while FIRE is held, building pressure (spike: <see cref="JetSpec"/>).</summary>
        Jet,
        /// <summary>The soldier shakes, then launches upward (spike: <see cref="RocketSpec"/>).</summary>
        RocketJump,
        /// <summary>A short-range spew that leaves burning sludge (spike: <see cref="SpewSpec"/>).</summary>
        Spew,
    }

    /// <summary>A weapon: what it fires, and how. Shots are replicated by weapon id.</summary>
    public sealed class WeaponDef
    {
        public readonly byte Id;
        public readonly string Name;
        /// <summary>Its short name on the HUD.</summary>
        public readonly string Hud;
        public readonly WeaponBehaviour Behaviour;
        public readonly ProjectileDef Projectile;
        /// <summary>Muzzle speed of a <see cref="WeaponBehaviour.Bullet"/> weapon.</summary>
        public readonly float Speed;
        /// <summary>Seconds between shots while the trigger is held.</summary>
        public readonly float Cooldown;
        /// <summary>Random spread either side of the aim, in degrees.</summary>
        public readonly float Spread;
        /// <summary>A lobbed weapon's throw, facing right.</summary>
        public readonly Vector2 Throw;
        /// <summary>How much of the thrower's own velocity a lobbed shot keeps.</summary>
        public readonly float Carry;
        /// <summary>Shots one pickup of this weapon gives. 0 means unlimited.</summary>
        public readonly int Ammo;
        /// <summary>The size of the flash of fire at the muzzle; 0 for none.</summary>
        public readonly float MuzzleBlast;
        /// <summary>How hard each shot shakes the camera; 0 for not at all.</summary>
        public readonly float Kick;

        public WeaponDef(byte id, string name, WeaponBehaviour behaviour, ProjectileDef projectile, float speed = 0f, float cooldown = 0f,
            float spread = 0f, Vector2 throwVelocity = default, float carry = 0f, int ammo = 0, float muzzleBlast = 0f, float kick = 0f,
            string hud = null)
        {
            Id = id;
            Name = name;
            Hud = hud ?? name.ToUpperInvariant();
            Behaviour = behaviour;
            Projectile = projectile;
            Speed = speed;
            Cooldown = cooldown;
            Spread = spread;
            Throw = throwVelocity;
            Carry = carry;
            Ammo = ammo;
            MuzzleBlast = muzzleBlast;
            Kick = kick;
        }

        /// <summary>The velocity of a lobbed shot, thrown facing <paramref name="facing"/> by something moving at <paramref name="thrower"/>.</summary>
        public Vector2 LobVelocity(int facing, Vector2 thrower) => new Vector2(facing * Throw.x + thrower.x * Carry, Throw.y);

        public override string ToString() => Name;
    }

    /// <summary>The behaviour that drives an enemy. Each is a class in the game code, with its own timings.</summary>
    public enum EnemyBrain
    {
        /// <summary>Advances, stops at range and fires level shots.</summary>
        Rifleman,
        /// <summary>Sprints at the nearest soldier and slashes up close.</summary>
        Runner,
        /// <summary>Hovers above the nearest soldier and drops bombs.</summary>
        Drone,
        /// <summary>The boss: patrols its arena, lobs shells and sweeps machine-gun bursts.</summary>
        Tank,
        /// <summary>An explosive barrel: stands still and blows up when it's destroyed.</summary>
        Barrel,
    }

    /// <summary>One kind of enemy: its body, health and worth. The co-op snapshot carries its id.</summary>
    public sealed class EnemyDef
    {
        public readonly byte Id;
        public readonly string Name;
        public readonly EnemyBrain Brain;
        public readonly float HalfWidth, Height;
        public readonly int Hp;
        /// <summary>Points for destroying it.</summary>
        public readonly int Score;

        public EnemyDef(byte id, string name, EnemyBrain brain, float halfWidth, float height, int hp, int score)
        {
            Id = id;
            Name = name;
            Brain = brain;
            HalfWidth = halfWidth;
            Height = height;
            Hp = hp;
            Score = score;
        }

        public override string ToString() => Name;
    }

    /// <summary>What a pickup does for the soldier who collects it.</summary>
    public enum PickupEffect
    {
        /// <summary>Gives <see cref="PickupDef.Gun"/>, which replaces the primary until its ammo runs out.</summary>
        Gun,
        /// <summary>Adds charges to the special: the soldier's own <see cref="SoldierDef.CrateSpecials"/>, with no cap.</summary>
        Specials,
        /// <summary>Heals <see cref="PickupDef.Amount"/> health.</summary>
        Heal,
        /// <summary>A spare life; in co-op it goes first to a partner who's out of lives.</summary>
        Life,
    }

    /// <summary>One kind of pickup. A pickup in the world is known by its level spawn, so this id stays local.</summary>
    public sealed class PickupDef
    {
        public readonly byte Id;
        public readonly string Name;
        public readonly PickupEffect Effect;
        public readonly WeaponDef Gun;
        public readonly int Amount;
        /// <summary>The popup when it's collected, and its colour.</summary>
        public readonly string Announce;
        public readonly Color AnnounceColor;
        /// <summary>Points for collecting it.</summary>
        public readonly int Score;

        public PickupDef(byte id, string name, PickupEffect effect, string announce, Color announceColor, int score,
            WeaponDef gun = null, int amount = 0)
        {
            Id = id;
            Name = name;
            Effect = effect;
            Announce = announce;
            AnnounceColor = announceColor;
            Score = score;
            Gun = gun;
            Amount = amount;
        }

        public override string ToString() => Name;
    }

    /// <summary>
    /// A playable soldier: how they move, their health, and their kit. The crew in <c>docs/design/player.md</c>
    /// each play differently, in how they move as well as how they fight.
    /// </summary>
    public sealed class SoldierDef
    {
        public readonly byte Id;
        public readonly string Name;
        public readonly float RunSpeed, CrawlSpeed, JumpVelocity;
        public readonly float HalfWidth, StandHeight, CrouchHeight;
        public readonly int MaxHp;
        /// <summary>Tier 1 of the kit: unlimited ammo.</summary>
        public readonly WeaponDef Primary;
        /// <summary>Tier 2 of the kit: fired with charges.</summary>
        public readonly WeaponDef Special;
        /// <summary>Charges of the special at the start of a mission.</summary>
        public readonly int StartSpecials;
        /// <summary>The special refills one charge every <see cref="SpecialRefill"/> seconds, up to this many (0: no refill).</summary>
        public readonly int SpecialMax;
        public readonly float SpecialRefill;
        /// <summary>Tier 3 of the kit, charged by kills. Null: the stand-in super (a fan of the primary's shots).</summary>
        public readonly WeaponDef Super;
        /// <summary>The bonus move, on a cooldown. Null for soldiers without one: their AB button doesn't show.</summary>
        public readonly WeaponDef Ability;
        public readonly float AbilityCooldown;
        /// <summary>
        /// Charges of the special a bomb crate adds. There's no cap, so this is how a strong special is kept in
        /// check: about 3 for a strong one, up to 10 for a weak one (<c>docs/design/player.md</c>, Crates).
        /// </summary>
        public readonly int CrateSpecials;

        public SoldierDef(byte id, string name, float runSpeed, float crawlSpeed, float jumpVelocity, float halfWidth, float standHeight,
            float crouchHeight, int maxHp, WeaponDef primary, WeaponDef special, int startSpecials,
            int specialMax = 0, float specialRefill = 0f, WeaponDef super = null, WeaponDef ability = null, float abilityCooldown = 0f,
            int crateSpecials = Tuning.Crates.DefaultSpecials)
        {
            CrateSpecials = crateSpecials;
            SpecialMax = specialMax;
            SpecialRefill = specialRefill;
            Super = super;
            Ability = ability;
            AbilityCooldown = ability != null ? abilityCooldown : 0f;
            Id = id;
            Name = name;
            RunSpeed = runSpeed;
            CrawlSpeed = crawlSpeed;
            JumpVelocity = jumpVelocity;
            HalfWidth = halfWidth;
            StandHeight = standHeight;
            CrouchHeight = crouchHeight;
            MaxHp = maxHp;
            Primary = primary;
            Special = special;
            StartSpecials = startSpecials;
        }

        public override string ToString() => Name;
    }
}
