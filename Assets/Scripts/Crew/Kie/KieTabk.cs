using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// kie's tabk's guns, THE PROJECTIONIST's (<c>docs/design/tabks/kie.md</c>), numbers in <see cref="KieTabkSpec"/>.
    /// FIRE is Weasel Pop: the reels spin up, then the lens tumbles out a volley of his weasels, bigger. SP is
    /// Trollogram: the lens beams a Trollogram of the whole tabk just ahead, which the goons shoot instead and which
    /// blows up on them (<see cref="TrollDecoy"/>). SU is Steal: the roulette over the toughest enemy on screen, as on
    /// foot (<see cref="StealRoulette"/>); the loot is his, and while he drives FIRE shoots it from the lens until it's
    /// spent. Weasels and Trollograms go to a partner as his own do (<see cref="KieKit"/>).
    /// </summary>
    public sealed class KieTabkGuns : TabkGuns
    {
        static KieTabkSpec Guns => KieTuning.Tabk;

        // seconds into a volley's charge and the beam (-1 when not), the roulette still spinning, the loot's cooldown
        float m_Charge = -1f, m_Beam = -1f, m_Stealing, m_LootCooldown;
        int m_NextId;

        public override bool Spinning => KieTabkSpec.ReelsSpin(m_Charge >= 0f, m_Beam >= 0f, m_Stealing > 0f);

        public override void Tick(PlayerTank tank, float dt, in SoldierInput input)
        {
            World w = tank.World;
            Player driver = w.Player;
            m_Stealing -= dt;
            m_LootCooldown -= dt;
            if (KieTabkSpec.FiresLoot(driver.GunAmmo))
            {
                if (input.Fire && m_LootCooldown <= 0f && m_Charge < 0f) FireLoot(tank, driver);
            }
            else if (input.FirePressed && m_Charge < 0f)
            {
                // a volley only goes when there's room for it, so a round is never thrown away
                if (PopWeasel.Out(w) + Guns.Weasels.Count <= Guns.Weasels.MaxOut && tank.Rounds.UseMain()) StartCharge();
                else tank.Click();
            }
            if (tank.Fire(TabkGun.Special, input.SpecialPressed && m_Beam < 0f)) StartBeam(tank);
            if (input.SuperPressed && m_Stealing <= 0f)
            {
                // nobody to rob: the round isn't spent
                if (tank.Rounds.Supers <= 0) tank.Click();
                else if (StealRoulette.Start(w, driver))
                {
                    tank.Rounds.UseSuper();
                    m_Stealing = KieTuning.Steal.SpinTime;
                }
            }
            Charging(tank, dt);
            Beaming(tank, dt);
        }

        /// <summary>Weasel Pop: one round, and the reels spin up first.</summary>
        void StartCharge()
        {
            m_Charge = 0f;
            Sfx.Play(Sfx.Servo, 0.5f);
        }

        void Charging(PlayerTank tank, float dt)
        {
            if (m_Charge < 0f) return;
            m_Charge += dt;
            if (m_Charge < Guns.Charge) return;
            m_Charge = -1f;
            if (KieKit.ThrowWeasels(tank.World, tank.MuzzleAlong(tank.Aim), tank.Aim, tank.Vel, Guns.Weasels)) tank.FlashMuzzle();
        }

        /// <summary>Trollogram: one round; the reels spin while the lens beams it out.</summary>
        void StartBeam(PlayerTank tank)
        {
            m_Beam = 0f;
            Sfx.Play(Sfx.Charge, 0.5f);
        }

        void Beaming(PlayerTank tank, float dt)
        {
            if (m_Beam < 0f) return;
            m_Beam += dt;
            World w = tank.World;
            float x = Guns.DecoyX(tank.Pos.x, tank.Facing, w.View.Left, w.View.Right, tank.HalfW);
            Vector2 lens = tank.MuzzleAlong(new Vector2(tank.Facing, 0f)), at = new Vector2(x, tank.Pos.y + 0.2f);
            // the beam: friendly sparks from the lens to where it'll stand
            if (Rng.Fx.Value < dt * 40f)
            {
                Vector2 from = Vector2.Lerp(lens, at + Vector2.up * tank.Height * 0.5f, Rng.Fx.Value);
                w.SpawnParticle(from, new Vector2(Rng.Fx.Range(-1f, 1f), Rng.Fx.Range(-1f, 1f)), TrollDecoy.Tint, 2f, 0.3f);
            }
            if (m_Beam < Guns.BeamTime) return;
            m_Beam = -1f;
            var decoy = new TrollDecoy(w, at, tank.Facing, m_NextId++ & 255, mine: true);
            w.Decoys.Add(decoy);
            w.Props.Add(decoy);
            KieKit.Send(w, TrollEvent.Drop, decoy);
            Sfx.Play(Sfx.TrollDrop, 0.9f);
            w.Burst(decoy.Center, 14, TrollDecoy.Tint);
            w.Scoring.Popup("TROLLOGRAM!", at + Vector2.up * 2.4f, TrollDecoy.Tint);
        }

        /// <summary>Loot he stole, fired from the lens along the aim, from its own ammo.</summary>
        void FireLoot(PlayerTank tank, Player driver)
        {
            WeaponDef gun = driver.Gun;
            if (gun == null || gun.Projectile == null)
            {
                driver.GunAmmo = 0;
                return;
            }
            Vector2 dir = tank.Aim;
            if (gun.Spread > 0f)
            {
                float spread = tank.World.Rng.Range(-gun.Spread, gun.Spread) * Mathf.Deg2Rad;
                dir = new Vector2(dir.x * Mathf.Cos(spread) - dir.y * Mathf.Sin(spread), dir.x * Mathf.Sin(spread) + dir.y * Mathf.Cos(spread));
            }
            Vector2 vel = gun.Behaviour == WeaponBehaviour.Lob ? gun.LobVelocity(tank.Facing, tank.Vel) : dir * gun.Speed;
            tank.World.Combat.FirePlayerShot(gun, tank.MuzzleAlong(tank.Aim), vel);
            m_LootCooldown = gun.Cooldown;
            tank.FlashMuzzle();
            Sfx.PlayShot(gun);
            if (--driver.GunAmmo == 0) tank.World.Scoring.Popup("OUT OF AMMO", tank.Pos + Vector2.up * 2.6f, new Color(1f, 0.6f, 0.4f));
        }

        public override void Stop()
        {
            m_Charge = m_Beam = -1f;
        }
    }
}
