using System;
using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// The game's content as data. Each definition has a stable id. Network messages carry weapon ids, so an id
    /// is never changed or reused; the tests pin them. Sprites and sounds stay with the game code, keyed by id.
    /// </summary>
    public static class Content
    {
        // ------------------------------------------------------------------ projectiles

        public static readonly ProjectileDef Bullet = new ProjectileDef(0, "Bullet", radius: 0.12f, damage: 1, life: 4f, contactDamage: true);
        public static readonly ProjectileDef HeavyBullet = new ProjectileDef(1, "Heavy bullet", radius: 0.12f, damage: 1, life: 4f, contactDamage: true);
        public static readonly ProjectileDef EnemyBullet = new ProjectileDef(2, "Enemy bullet", radius: 0.14f, damage: 1, life: 4f, contactDamage: true,
            spin: ProjectileSpin.None);
        public static readonly ProjectileDef Shell = new ProjectileDef(3, "Shell", radius: 0.2f, damage: 1, life: 4f, gravity: 18f,
            blastRadius: 1f, blastSize: 0.7f);
        public static readonly ProjectileDef Grenade = new ProjectileDef(4, "Grenade", radius: 0.18f, damage: 6, life: 1.8f, gravity: 30f, bounces: 1,
            blastRadius: 1.7f, blastSize: 1f, spin: ProjectileSpin.Tumble);
        public static readonly ProjectileDef Bomb = new ProjectileDef(5, "Bomb", radius: 0.18f, damage: 1, life: 4f, gravity: 22f,
            blastRadius: 1f, blastSize: 0.7f, spin: ProjectileSpin.None);

        // ------------------------------------------------------------------ weapons
        // Ids 0-5 are the byte the projectile enum used to send for the same shot.

        public static readonly WeaponDef Pistol = new WeaponDef(0, "Pistol", WeaponBehaviour.Bullet, Bullet, speed: 16f, cooldown: 0.18f);
        public static readonly WeaponDef HeavyMachineGun = new WeaponDef(1, "Heavy machine gun", WeaponBehaviour.Bullet, HeavyBullet,
            speed: 20f, cooldown: 0.075f, spread: 4f, ammo: 200);
        /// <summary>The goon soldiers' rifle.</summary>
        public static readonly WeaponDef Rifle = new WeaponDef(2, "Rifle", WeaponBehaviour.Bullet, EnemyBullet, speed: 6.5f);
        /// <summary>The tank's cannon. It aims each shell to land near its target.</summary>
        public static readonly WeaponDef Cannon = new WeaponDef(3, "Cannon", WeaponBehaviour.Lob, Shell, muzzleBlast: 0.4f, kick: 0.08f);
        public static readonly WeaponDef GrenadeThrow = new WeaponDef(4, "Grenade", WeaponBehaviour.Lob, Grenade,
            throwVelocity: new Vector2(6.5f, 9f), carry: 0.3f);
        /// <summary>The drone's bombs, dropped with half its speed.</summary>
        public static readonly WeaponDef DroneBomb = new WeaponDef(5, "Bomb drop", WeaponBehaviour.Lob, Bomb, carry: 0.5f);
        /// <summary>The tank's machine gun.</summary>
        public static readonly WeaponDef TankGun = new WeaponDef(6, "Tank gun", WeaponBehaviour.Bullet, EnemyBullet, speed: 7.5f);

        // ------------------------------------------------------------------ lookups (after the definitions: statics initialise in order)

        public static readonly IReadOnlyList<ProjectileDef> Projectiles = ById(p => p.Id, Bullet, HeavyBullet, EnemyBullet, Shell, Grenade, Bomb);
        public static readonly IReadOnlyList<WeaponDef> Weapons = ById(w => w.Id, Pistol, HeavyMachineGun, Rifle, Cannon, GrenadeThrow, DroneBomb, TankGun);

        /// <summary>The weapon with this id, or null for an id no weapon has (say, from a different build).</summary>
        public static WeaponDef Weapon(int id) => id >= 0 && id < Weapons.Count ? Weapons[id] : null;

        /// <summary>A table where each definition sits at its id. Throws if two share an id or one leaves a gap.</summary>
        static T[] ById<T>(Func<T, byte> id, params T[] defs) where T : class
        {
            var table = new T[defs.Length];
            foreach (T def in defs)
            {
                int i = id(def);
                if (i >= table.Length || table[i] != null)
                    throw new InvalidOperationException($"{typeof(T).Name} ids must be 0 to {table.Length - 1}, each used once: {def} has {i}");
                table[i] = def;
            }
            return table;
        }
    }
}
