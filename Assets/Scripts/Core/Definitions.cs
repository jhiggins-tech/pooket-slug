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
    }

    /// <summary>A weapon: what it fires, and how. Shots are replicated by weapon id.</summary>
    public sealed class WeaponDef
    {
        public readonly byte Id;
        public readonly string Name;
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
            float spread = 0f, Vector2 throwVelocity = default, float carry = 0f, int ammo = 0, float muzzleBlast = 0f, float kick = 0f)
        {
            Id = id;
            Name = name;
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
}
