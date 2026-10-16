using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Shots, hits and damage. Projectiles meet their targets here, and damage goes to whoever owns it: the
    /// authority hurts enemies (the others report their hits), and each client hurts its own soldier.
    /// </summary>
    public sealed class Combat
    {
        readonly World m_World;
        readonly Dictionary<IAvatarLink, float> m_HurtCooldown = new Dictionary<IAvatarLink, float>();

        public Combat(World world) => m_World = world;

        /// <summary>Our soldier fires. A harmless copy of the shot plays on the other clients.</summary>
        public void FirePlayerShot(WeaponDef weapon, Vector2 origin, Vector2 vel)
        {
            m_World.SpawnProjectile(weapon.Projectile, origin, vel, false);
            m_World.Net?.SendShot(new ShotEvent(weapon, origin, vel));
        }

        /// <summary>Authority side: an enemy fires (replayed on the other clients).</summary>
        public void FireEnemyShot(WeaponDef weapon, Vector2 origin, Vector2 vel)
        {
            SpawnEnemyShot(weapon, origin, vel);
            if (m_World.Coop) m_World.Net.SendEnemyShot(new ShotEvent(weapon, origin, vel));
        }

        /// <summary>An enemy's shot, fired here or replayed from the authority.</summary>
        public void SpawnEnemyShot(WeaponDef weapon, Vector2 origin, Vector2 vel)
        {
            m_World.SpawnProjectile(weapon.Projectile, origin, vel, true);
            if (weapon.MuzzleBlast > 0f) m_World.SpawnEffect(Art.Explosion, 30f, origin, weapon.MuzzleBlast);
            Sfx.PlayShot(weapon);
            if (weapon.Kick > 0f) m_World.View.Shake(weapon.Kick);
        }

        /// <summary>Our soldier hit an enemy: apply it (authority) or report it to the authority.</summary>
        public void DamageEnemy(Enemy e, int damage)
        {
            if (e.Shootable && !e.KillCredited && damage >= e.Hp && e.Def.Brain != EnemyBrain.Barrel)
            {
                e.KillCredited = true;
                m_World.OnPlayerKill(e);
            }
            if (m_World.IsAuthority)
            {
                e.Hurt(damage);
                return;
            }
            e.FlashHit();
            m_World.Net.ReportHit(new HitReport(e.NetId, damage));
        }

        /// <summary>
        /// Nearest soldier still in the fight (the local one if everybody is down), or kie's hologram while it's up on
        /// screen: the goons shoot that instead (Trollogram).
        /// </summary>
        public Actor NearestAvatar(Vector2 from)
        {
            TrollDecoy nearestDecoy = null;
            float decoyDist = float.MaxValue;
            foreach (TrollDecoy d in m_World.Decoys)
            {
                if (!TrollogramSpec.Draws(d.Up, d.Pos.x, m_World.View.Left, m_World.View.Right)) continue;
                float dd = (d.Pos - from).sqrMagnitude;
                if (dd < decoyDist)
                {
                    nearestDecoy = d;
                    decoyDist = dd;
                }
            }
            if (nearestDecoy != null) return nearestDecoy; // a hologram on screen always comes first
            Player player = m_World.Player;
            Actor best = player;
            float bestDist = player.IsDown ? float.MaxValue : (player.Pos - from).sqrMagnitude;
            foreach (RemotePlayer r in m_World.Remotes)
            {
                if (r.IsDown) continue;
                float d = (r.Pos - from).sqrMagnitude;
                if (d < bestDist)
                {
                    best = r;
                    bestDist = d;
                }
            }
            return best;
        }

        /// <summary>Authority side: melee/contact damage. A partner's soldier is hurt by its own client.</summary>
        public void DamageAvatar(Actor target, int damage)
        {
            if (target is TrollDecoy decoy)
            {
                decoy.Blow();
                return;
            }
            if (target == m_World.Player)
            {
                m_World.Player.Hurt(damage);
                return;
            }
            if (target is RemotePlayer rp && !rp.Invulnerable)
            {
                if (m_HurtCooldown.TryGetValue(rp.Link, out float until) && m_World.Elapsed < until) return;
                m_HurtCooldown[rp.Link] = m_World.Elapsed + Tuning.Coop.RemoteHurtCooldown;
                rp.Link.Hurt(damage);
            }
        }

        public void DamageAvatarsTouching(Rect area, int damage)
        {
            if (area.Overlaps(m_World.Player.Bounds)) m_World.Player.Hurt(damage);
            foreach (RemotePlayer r in m_World.Remotes)
                if (!r.IsDown && area.Overlaps(r.Bounds)) DamageAvatar(r, damage);
        }

        /// <summary>Projectiles against soldiers and enemies, then our soldier against pickups.</summary>
        public void Collide()
        {
            World w = m_World;
            Player player = w.Player;
            foreach (Projectile p in w.Projectiles)
            {
                if (p.Removed) continue;
                if (p.Hostile)
                {
                    TrollDecoy hitDecoy = null;
                    foreach (TrollDecoy d in w.Decoys)
                        if (d.Up && Overlaps(d.Bounds, p.Pos, p.Radius)) hitDecoy = d;
                    if (hitDecoy != null)
                    {
                        hitDecoy.Blow();
                        p.Detonate();
                        continue;
                    }
                    if (!player.Invulnerable && Overlaps(player.Bounds, p.Pos, p.Radius))
                    {
                        if (p.Def.ContactDamage) player.Hurt(p.Damage);
                        p.Detonate();
                        continue;
                    }
                    // the partner's client applies the damage; here the shot just stops on them
                    foreach (RemotePlayer r in w.Remotes)
                    {
                        if (r.IsDown || r.Invulnerable || !Overlaps(r.Bounds, p.Pos, p.Radius)) continue;
                        p.Detonate();
                        break;
                    }
                    continue;
                }
                foreach (Enemy e in w.Enemies)
                {
                    if (!e.Shootable || !Overlaps(e.Bounds, p.Pos, p.Radius)) continue;
                    if (p.Def.ContactDamage)
                    {
                        if (!p.Cosmetic) DamageEnemy(e, p.Damage); // a partner's shot is theirs to report; here it just sparks
                        w.HitFeel.Spark(p.Pos, p.Vel, p.Damage >= 2, ours: !p.Cosmetic);
                    }
                    p.Detonate();
                    break;
                }
            }

            if (!player.Dead)
            {
                foreach (Pickup pk in w.Pickups)
                {
                    if (pk.Removed || pk.ClaimPending || !pk.Bounds.Overlaps(player.Bounds)) continue;
                    if (w.Coop)
                    {
                        pk.ClaimPending = true; // the authority decides who got it
                        w.Net.ClaimPickup(pk.NetId);
                    }
                    else
                    {
                        pk.ApplyTo(player);
                        pk.Collect();
                    }
                }
            }
        }

        static bool Overlaps(Rect r, Vector2 c, float radius)
        {
            float cx = Mathf.Clamp(c.x, r.xMin, r.xMax), cy = Mathf.Clamp(c.y, r.yMin, r.yMax);
            float dx = c.x - cx, dy = c.y - cy;
            return dx * dx + dy * dy <= radius * radius;
        }

        /// <summary>
        /// Authority: an enemy's blast that hurts every soldier in reach, ours and (through their owners) the partners'
        /// (a weasel popping).
        /// </summary>
        public void HostileBlast(Vector2 at, float radius, int damage, float size)
        {
            Explode(at, radius, damage, true, size);
            foreach (RemotePlayer r in m_World.Remotes)
                if (!r.IsDown && !r.Invulnerable && Overlaps(r.Bounds, at, radius)) DamageAvatar(r, damage);
        }

        public void Explode(Vector2 at, float radius, int damage, bool hostile, float size)
        {
            World w = m_World;
            w.SpawnEffect(Art.Explosion, 18f, at, size);
            w.Burst(at, Mathf.RoundToInt(6 * size), new Color(0.35f, 0.3f, 0.28f));
            Sfx.Play(Sfx.Explosion, Mathf.Clamp01(0.4f + size * 0.4f));
            w.View.Shake(0.1f * size);
            w.HitFeel.Blast(size);
            if (damage <= 0) return;
            if (w.Level.Craters != null && w.Level.Craters.Dig(at, radius)) w.CraterView.Refresh();

            if (hostile)
            {
                for (int i = w.Decoys.Count - 1; i >= 0; i--)
                    if (w.Decoys[i].Up && Overlaps(w.Decoys[i].Bounds, at, radius)) w.Decoys[i].Blow();
                if (!w.Player.Invulnerable && Overlaps(w.Player.Bounds, at, radius)) w.Player.Hurt(damage);
                return;
            }
            foreach (Enemy e in w.Enemies)
                if (e.Shootable && Overlaps(e.Bounds, at, radius)) DamageEnemy(e, damage);
        }
    }
}
