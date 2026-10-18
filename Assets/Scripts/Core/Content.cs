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
        /// <summary>The boss tank's big shell: a wider blast than a shell.</summary>
        public static readonly ProjectileDef BigShell = new ProjectileDef(6, "Big shell", radius: 0.25f, damage: 1, life: 6f, gravity: 18f,
            blastRadius: 1.4f, blastSize: 1.1f);
        /// <summary>Knives stolen off a knife runner (kie's Steal): thrown, tumbling, a little drop.</summary>
        public static readonly ProjectileDef Knife = new ProjectileDef(7, "Knife", radius: 0.14f, damage: 2, life: 2f, gravity: 6f, contactDamage: true,
            spin: ProjectileSpin.Tumble);
        /// <summary>A boss tank's cannon in kie's hands (Steal): a heavy shell with a wide blast.</summary>
        public static readonly ProjectileDef StolenShell = new ProjectileDef(8, "Stolen shell", radius: 0.25f, damage: 8, life: 4f, gravity: 12f,
            blastRadius: 1.6f, blastSize: 1.2f);
        /// <summary>Retired: the first tank's shell and mortar, before it became tones2's (ids are never reused).</summary>
        public static readonly ProjectileDef PlayerTankShellShot = new ProjectileDef(9, "Tabk shell", radius: 0.22f, damage: 8, life: 3f,
            gravity: 2f, blastRadius: 1.5f, blastSize: 1f);
        public static readonly ProjectileDef PlayerTankMortarShot = new ProjectileDef(10, "Tabk mortar", radius: 0.25f, damage: 12, life: 4f,
            gravity: 18f, blastRadius: 2f, blastSize: 1.3f, spin: ProjectileSpin.Tumble);

        // ------------------------------------------------------------------ weapons
        // Ids 0-5 are the byte the projectile enum used to send for the same shot.

        public static readonly WeaponDef Pistol = new WeaponDef(0, "Pistol", WeaponBehaviour.Bullet, Bullet, speed: 16f, cooldown: 0.18f);
        public static readonly WeaponDef HeavyMachineGun = new WeaponDef(1, "Heavy machine gun", WeaponBehaviour.Bullet, HeavyBullet,
            speed: 20f, cooldown: 0.075f, spread: 4f, ammo: 200, hud: "HMG");
        /// <summary>The goon soldiers' rifle.</summary>
        public static readonly WeaponDef Rifle = new WeaponDef(2, "Rifle", WeaponBehaviour.Bullet, EnemyBullet, speed: 6.5f);
        /// <summary>The tank's cannon. It aims each shell to land near its target.</summary>
        public static readonly WeaponDef Cannon = new WeaponDef(3, "Cannon", WeaponBehaviour.Lob, Shell, muzzleBlast: 0.4f, kick: 0.08f);
        public static readonly WeaponDef GrenadeThrow = new WeaponDef(4, "Grenade", WeaponBehaviour.Lob, Grenade,
            throwVelocity: new Vector2(6.5f, 9f), carry: 0.3f, hud: "BOMB");
        /// <summary>The drone's bombs, dropped with half its speed.</summary>
        public static readonly WeaponDef DroneBomb = new WeaponDef(5, "Bomb drop", WeaponBehaviour.Lob, Bomb, carry: 0.5f);
        /// <summary>The tank's machine gun.</summary>
        public static readonly WeaponDef TankGun = new WeaponDef(6, "Tabk gun", WeaponBehaviour.Bullet, EnemyBullet, speed: 7.5f);
        // tones2's kit (Tones2Kit.cs, numbers in Tones2Tuning). None fires a projectile: the jet and the spew pour
        // out droplets, and the rocket jump launches him. Only the rocket jump is sent, so partners see the mud.
        public static readonly WeaponDef WaterJet = new WeaponDef(7, "ten-1", WeaponBehaviour.Jet, null, hud: "TEN-1");
        public static readonly WeaponDef RocketJump = new WeaponDef(8, "ten-2", WeaponBehaviour.RocketJump, null, hud: "TEN-2");
        public static readonly WeaponDef Spew = new WeaponDef(9, "ten-3", WeaponBehaviour.Spew, null, hud: "TEN-3");
        /// <summary>The boss tank's cannon: it aims its own lobs (<see cref="BossTank"/>).</summary>
        public static readonly WeaponDef BossCannon = new WeaponDef(10, "Boss cannon", WeaponBehaviour.Lob, BigShell, muzzleBlast: 0.6f, kick: 0.15f);
        // kie's kit (KieKit.cs, numbers in KieTuning). Weasels are critters, not projectiles.
        public static readonly WeaponDef WeaselPop = new WeaponDef(11, "Weasel Pop", WeaponBehaviour.Weasels, null, cooldown: KieTuning.Weasels.Cooldown, hud: "WEASELS");
        public static readonly WeaponDef Trollogram = new WeaponDef(12, "Trollogram", WeaponBehaviour.Trollogram, null, hud: "TROLL");
        public static readonly WeaponDef Steal = new WeaponDef(13, "Steal", WeaponBehaviour.Steal, null, hud: "STEAL");
        // What Steal lifts (StealSpec): each takes the primary's place, like a crate's gun, until its ammo runs out.
        public static readonly WeaponDef StolenRifle = new WeaponDef(14, "Stolen rifle", WeaponBehaviour.Bullet, Bullet, speed: 16f, cooldown: 0.12f,
            spread: 3f, ammo: 30, hud: "RIFLE");
        public static readonly WeaponDef StolenKnives = new WeaponDef(15, "Stolen knives", WeaponBehaviour.Bullet, Knife, speed: 14f, cooldown: 0.22f,
            ammo: 15, hud: "KNIVES");
        public static readonly WeaponDef StolenBombs = new WeaponDef(16, "Stolen bombs", WeaponBehaviour.Lob, Grenade, cooldown: 0.4f,
            throwVelocity: new Vector2(5.5f, 8f), carry: 0.3f, ammo: 8, hud: "BOMBS");
        public static readonly WeaponDef StolenCannon = new WeaponDef(17, "Stolen cannon", WeaponBehaviour.Bullet, StolenShell, speed: 15f, cooldown: 0.7f,
            ammo: 5, muzzleBlast: 0.5f, kick: 0.15f, hud: "CANNON");
        // Retired: the first tank's cannon and mortar. Your tabk is tones2's now, his guns in Tones2TabkSpec (ids are never reused).
        public static readonly WeaponDef PlayerTankCannon = new WeaponDef(18, "Tabk cannon", WeaponBehaviour.Bullet, PlayerTankShellShot, speed: 15f,
            cooldown: 0.35f, muzzleBlast: 0.45f, kick: 0.12f, hud: "SHELL");
        public static readonly WeaponDef PlayerTankMortar = new WeaponDef(19, "Tabk mortar", WeaponBehaviour.Lob, PlayerTankMortarShot,
            throwVelocity: new Vector2(6f, 12f), carry: 0.4f, hud: "MORTAR");
        /// <summary>A team super fired (<see cref="TeamSupers"/>): sent to the partner, so their screen plays it and spends their super.</summary>
        public static readonly WeaponDef TeamSuper = new WeaponDef(20, "Team super", WeaponBehaviour.TeamSuper, null, hud: "TEAM");

        /// <summary>How Mission one's boss tank fights.</summary>
        public static readonly BossTankSpec BossTank = new BossTankSpec();
        /// <summary>What makes the boss tank kie's: its three attacks (<see cref="KieTankSpec"/>).</summary>
        public static readonly KieTankSpec KieTank = new KieTankSpec();
        /// <summary>kcaj's tabk's sweep and rain (<see cref="KcajTankSpec"/>), Mission two's boss.</summary>
        public static readonly KcajTankSpec KcajTank = new KcajTankSpec();
        /// <summary>How your tank drives and fights, the one you climb into or call in.</summary>
        public static readonly PlayerTankSpec PlayerTank = new PlayerTankSpec();

        // ------------------------------------------------------------------ enemies
        // Ids are the byte the co-op snapshot sent for each kind before (its SpawnKind).

        public static readonly EnemyDef Rifleman = new EnemyDef(0, "Rifleman", EnemyBrain.Rifleman, halfWidth: 0.3f, height: 1.35f, hp: 3, score: 100);
        public static readonly EnemyDef Runner = new EnemyDef(1, "Knife runner", EnemyBrain.Runner, halfWidth: 0.3f, height: 1.35f, hp: 2, score: 150);
        public static readonly EnemyDef Drone = new EnemyDef(2, "Drone", EnemyBrain.Drone, halfWidth: 0.5f, height: 0.7f, hp: 3, score: 200);
        public static readonly EnemyDef Tank = new EnemyDef(3, "Tabk", EnemyBrain.Tank, halfWidth: 1.5f, height: 1.3f, hp: 45, score: 3000);
        public static readonly EnemyDef Barrel = new EnemyDef(4, "Explosive barrel", EnemyBrain.Barrel, halfWidth: 0.38f, height: 1.1f, hp: 3, score: 100);
        /// <summary>kie's tank's weasels and hologram copies (<see cref="KieTank"/>), made mid-fight.</summary>
        public static readonly EnemyDef Weasel = new EnemyDef(5, "Weasel", EnemyBrain.Weasel, halfWidth: 0.35f, height: 0.55f, hp: 1, score: 50);
        public static readonly EnemyDef TankCopy = new EnemyDef(6, "Tabk copy", EnemyBrain.TankCopy, halfWidth: 1.5f, height: 1.3f, hp: 45, score: 100);
        /// <summary>Hologram kie, who jumps out of the wrecked tank (<see cref="KiePilotSpec"/>), and the Trollogram he drops.</summary>
        public static readonly EnemyDef Pilot = new EnemyDef(7, "Trollogram kie", EnemyBrain.Pilot, halfWidth: 0.3f, height: 1.35f, hp: KieTuning.Pilot.Hp, score: 5000);
        public static readonly EnemyDef PilotDecoy = new EnemyDef(8, "Trollogram", EnemyBrain.PilotDecoy, halfWidth: 0.3f, height: 1.35f, hp: 1, score: 100);
        /// <summary>The coda's watchtower and the base's last gate (<c>docs/design/story.md</c>, The coda).</summary>
        public static readonly EnemyDef ToppleTower = new EnemyDef(9, "Watchtower", EnemyBrain.ToppleTower, halfWidth: 1.1f, height: 5.2f, hp: 14, score: 1000);
        public static readonly EnemyDef ExitGate = new EnemyDef(10, "Last gate", EnemyBrain.ExitGate, halfWidth: 1.0f, height: 4f, hp: 24, score: 2000);
        /// <summary>Mission two's breakables: a box of cones and a nitrogen tank (<see cref="Creamery"/>).</summary>
        public static readonly EnemyDef ConeBox = new EnemyDef(11, "Box of cones", EnemyBrain.ConeBox, halfWidth: 0.45f, height: 0.9f, hp: 3, score: 50);
        public static readonly EnemyDef NitrogenTank = new EnemyDef(12, "Nitrogen tank", EnemyBrain.NitrogenTank, halfWidth: 0.38f, height: 1.25f, hp: 3, score: 100);

        // ------------------------------------------------------------------ pickups

        public static readonly PickupDef HeavyMachineGunCrate = new PickupDef(0, "Heavy machine gun crate", PickupEffect.Gun,
            "HEAVY MACHINE GUN!", Palette.Gold, score: 500, gun: HeavyMachineGun);
        public static readonly PickupDef BombCrate = new PickupDef(1, "Bomb crate", PickupEffect.Specials,
            "BOMBS!", new Color(0.6f, 1f, 0.5f), score: 500);
        public static readonly PickupDef Medkit = new PickupDef(2, "Medkit", PickupEffect.Heal,
            "MEDKIT!", new Color(1f, 0.5f, 0.5f), score: 500, amount: Tuning.Crates.MedkitHearts);
        public static readonly PickupDef OneUp = new PickupDef(3, "1-up", PickupEffect.Life,
            "1UP!", new Color(0.5f, 1f, 0.6f), score: 1000, amount: 1);

        // ------------------------------------------------------------------ soldiers

        /// <summary>
        /// The first soldier, with a pistol and grenades. Nobody plays him now; he stays as the stand-in for a soldier
        /// a co-op partner's build has and this one doesn't.
        /// </summary>
        public static readonly SoldierDef Trooper = new SoldierDef(0, "Trooper", primary: Pistol, special: GrenadeThrow,
            startSpecials: Tuning.Kit.SpecialCharges, specialMax: Tuning.Kit.SpecialCharges, specialRefill: Tuning.Kit.SpecialRefill);

        /// <summary>
        /// tones2, from Pooket Tabks, who everyone plays until the crew is freed (<c>docs/design/player.md</c>): the water
        /// jet, the mud rocket jump and the spew.
        /// </summary>
        public static readonly SoldierDef Tones2 = new SoldierDef(1, "tones2", primary: WaterJet, special: RocketJump, startSpecials: 3,
            super: Spew, crateSpecials: 3); // the rocket jump is a strong special: a bomb crate gives about 3

        /// <summary>
        /// kie, Mission one's crewmate (<c>docs/design/kits.md</c>): Weasel Pop, Trollogram and Steal. Picked with CREW once Mission one is beaten (<see cref="Crew"/>), and played by Mission one's pilot.
        /// </summary>
        public static readonly SoldierDef Kie = new SoldierDef(2, "kie", primary: WeaselPop, special: Trollogram,
            startSpecials: Tuning.Kit.SpecialCharges, specialMax: Tuning.Kit.SpecialCharges, specialRefill: Tuning.Kit.SpecialRefill, super: Steal);

        // ------------------------------------------------------------------ lookups (after the definitions: statics initialise in order)

        public static readonly IReadOnlyList<ProjectileDef> Projectiles = ById(p => p.Id, Bullet, HeavyBullet, EnemyBullet, Shell, Grenade, Bomb, BigShell, Knife, StolenShell,
            PlayerTankShellShot, PlayerTankMortarShot);
        public static readonly IReadOnlyList<WeaponDef> Weapons = ById(w => w.Id, Pistol, HeavyMachineGun, Rifle, Cannon, GrenadeThrow, DroneBomb, TankGun,
            WaterJet, RocketJump, Spew, BossCannon, WeaselPop, Trollogram,
            Steal, StolenRifle, StolenKnives, StolenBombs, StolenCannon, PlayerTankCannon, PlayerTankMortar, TeamSuper);
        public static readonly IReadOnlyList<EnemyDef> Enemies = ById(e => e.Id, Rifleman, Runner, Drone, Tank, Barrel, Weasel, TankCopy, Pilot, PilotDecoy, ToppleTower, ExitGate, ConeBox, NitrogenTank);
        public static readonly IReadOnlyList<PickupDef> Pickups = ById(p => p.Id, HeavyMachineGunCrate, BombCrate, Medkit, OneUp);
        public static readonly IReadOnlyList<SoldierDef> Soldiers = ById(s => s.Id, Trooper, Tones2, Kie);

        /// <summary>The weapon with this id, or null for an id no weapon has (say, from a different build).</summary>
        public static WeaponDef Weapon(int id) => id >= 0 && id < Weapons.Count ? Weapons[id] : null;

        /// <summary>The weapon Steal lifts as <paramref name="prize"/>, or null for nothing.</summary>
        public static WeaponDef StolenWeapon(StealPrize prize)
        {
            switch (prize)
            {
                case StealPrize.Rifle: return StolenRifle;
                case StealPrize.Knives: return StolenKnives;
                case StealPrize.Bombs: return StolenBombs;
                case StealPrize.Cannon: return StolenCannon;
                default: return null;
            }
        }

        /// <summary>The pickup with this id, or null for an id no pickup has.</summary>
        public static PickupDef Pickup(int id) => id >= 0 && id < Pickups.Count ? Pickups[id] : null;

        /// <summary>The soldier with this id, or null for an id no soldier has.</summary>
        public static SoldierDef Soldier(int id) => id >= 0 && id < Soldiers.Count ? Soldiers[id] : null;

        /// <summary>The enemy with this id, or null for an id no enemy has.</summary>
        public static EnemyDef Enemy(int id) => id >= 0 && id < Enemies.Count ? Enemies[id] : null;

        /// <summary>The pickup a level marker places, or null if it places something else.</summary>
        public static PickupDef PickupFor(SpawnKind kind)
        {
            switch (kind)
            {
                case SpawnKind.CrateHeavy: return HeavyMachineGunCrate;
                case SpawnKind.CrateBombs: return BombCrate;
                case SpawnKind.Medkit: return Medkit;
                case SpawnKind.OneUp: return OneUp;
                default: return null;
            }
        }

        /// <summary>The enemy a level marker places, or null if it places something else.</summary>
        public static EnemyDef EnemyFor(SpawnKind kind)
        {
            switch (kind)
            {
                case SpawnKind.Rifleman: return Rifleman;
                case SpawnKind.Runner: return Runner;
                case SpawnKind.Drone: return Drone;
                case SpawnKind.Tank: return Tank;
                case SpawnKind.Barrel: return Barrel;
                case SpawnKind.ToppleTower: return ToppleTower;
                case SpawnKind.ExitGate: return ExitGate;
                case SpawnKind.ConeBox: return ConeBox;
                case SpawnKind.NitrogenTank: return NitrogenTank;
                default: return null;
            }
        }

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
