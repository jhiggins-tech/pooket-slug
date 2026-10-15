using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// The kit's meters (<c>docs/design/player.md</c>, The kit): the special's charges, which refill over time;
    /// the super, charged by kills; and the ability's cooldown. Crates and spikes may change the charges directly.
    /// </summary>
    public sealed class KitMeters
    {
        /// <summary>Charges of the special. Refills one at a time up to <see cref="SpecialMax"/>; crates can add more.</summary>
        public int Specials;
        public readonly int SpecialMax;
        /// <summary>Seconds to refill one charge; 0 for none.</summary>
        public readonly float SpecialRefill;
        /// <summary>Kills that fill the super.</summary>
        public readonly int SuperKills;
        /// <summary>Seconds between uses of the ability; 0 when the soldier has none.</summary>
        public readonly float AbilityCooldown;

        float m_Refill;
        int m_Kills;
        float m_AbilityWait;

        public KitMeters(int startSpecials, int specialMax, float specialRefill, int superKills, float abilityCooldown)
        {
            Specials = startSpecials;
            SpecialMax = specialMax;
            SpecialRefill = specialRefill;
            SuperKills = Mathf.Max(1, superKills);
            AbilityCooldown = abilityCooldown;
        }

        /// <summary>How far the next special charge has refilled, 0 to 1 (0 when full or not refilling).</summary>
        public float SpecialRefillProgress => SpecialRefill > 0f && Specials < SpecialMax ? m_Refill / SpecialRefill : 0f;
        /// <summary>How charged the super is, 0 to 1.</summary>
        public float SuperCharge => Mathf.Clamp01((float)m_Kills / SuperKills);
        public bool SuperReady => m_Kills >= SuperKills;
        public bool HasAbility => AbilityCooldown > 0f;
        public bool AbilityReady => HasAbility && m_AbilityWait <= 0f;
        /// <summary>How far the ability's cooldown has run, 0 to 1 (1 when ready).</summary>
        public float AbilityProgress => !HasAbility ? 0f : 1f - Mathf.Clamp01(m_AbilityWait / AbilityCooldown);

        public void Tick(float dt)
        {
            if (SpecialRefill > 0f && Specials < SpecialMax)
            {
                m_Refill += dt;
                if (m_Refill >= SpecialRefill)
                {
                    m_Refill -= SpecialRefill;
                    Specials++;
                }
            }
            else m_Refill = 0f;
            if (m_AbilityWait > 0f) m_AbilityWait -= dt;
        }

        /// <summary>Spends a charge of the special. False if there's none.</summary>
        public bool UseSpecial()
        {
            if (Specials <= 0) return false;
            Specials--;
            return true;
        }

        /// <summary>A kill by this soldier adds to the super.</summary>
        public void AddKill()
        {
            if (m_Kills < SuperKills) m_Kills++;
        }

        /// <summary>Charges the super all the way (a spike starts with it ready, to try it at once).</summary>
        public void FillSuper() => m_Kills = SuperKills;

        /// <summary>Fires the super if it's charged, emptying it.</summary>
        public bool UseSuper()
        {
            if (!SuperReady) return false;
            m_Kills = 0;
            return true;
        }

        public bool UseAbility()
        {
            if (!AbilityReady) return false;
            m_AbilityWait = AbilityCooldown;
            return true;
        }
    }

    /// <summary>
    /// The SP button, and calling in a tank with it (<c>docs/design/tanks.md</c>, Calling in a tank). With no
    /// tank to call, SP fires the special when pressed. With one, holding the stick down and holding SP charges
    /// the call-in: let go before it's charged and it's cancelled, hold on and the tank comes. A tap of SP still
    /// fires the special, even crouching; while crouching, it fires as the tap ends, since a tap can't be told
    /// from the start of a hold any sooner.
    /// </summary>
    public sealed class CallInGesture
    {
        public enum Result { None, FireSpecial, Charging, Cancelled, Called }

        enum Phase { Idle, Pending, Charging }

        /// <summary>Holding SP longer than this is a hold, not a tap.</summary>
        public readonly float TapTime;
        /// <summary>Seconds of holding, after the tap time, before the tank comes.</summary>
        public readonly float ChargeTime;

        Phase m_Phase;
        float m_Held;

        public CallInGesture(float tapTime, float chargeTime)
        {
            TapTime = tapTime;
            ChargeTime = chargeTime;
        }

        /// <summary>How charged the call-in is, 0 to 1, while it's charging; otherwise 0.</summary>
        public float Charge => m_Phase == Phase.Charging ? Mathf.Clamp01((m_Held - TapTime) / ChargeTime) : 0f;
        public bool Charging => m_Phase == Phase.Charging;

        /// <summary>
        /// One frame. <paramref name="canCall"/>: a tank is available to call this mission. Returns what happened.
        /// </summary>
        public Result Tick(float dt, bool down, bool special, bool specialPressed, bool canCall)
        {
            switch (m_Phase)
            {
                case Phase.Idle:
                    if (!specialPressed) return Result.None;
                    if (!canCall || !down) return Result.FireSpecial;
                    m_Phase = Phase.Pending;
                    m_Held = 0f;
                    return Result.None;

                case Phase.Pending:
                    if (!special)
                    {
                        m_Phase = Phase.Idle;
                        return Result.FireSpecial; // a tap
                    }
                    m_Held += dt;
                    if (m_Held < TapTime) return Result.None;
                    if (!down || !canCall)
                    {
                        m_Phase = Phase.Idle; // a hold that isn't a call-in does nothing
                        return Result.Cancelled;
                    }
                    m_Phase = Phase.Charging;
                    return Result.Charging;

                default: // charging
                    if (!special || !down || !canCall)
                    {
                        m_Phase = Phase.Idle;
                        return Result.Cancelled;
                    }
                    m_Held += dt;
                    if (m_Held >= TapTime + ChargeTime)
                    {
                        m_Phase = Phase.Idle;
                        return Result.Called;
                    }
                    return Result.Charging;
            }
        }

        public void Reset() => m_Phase = Phase.Idle;
    }

    /// <summary>Where a soldier aims (<c>docs/design/player.md</c>, Aiming): forward, up, down in the air, and diagonals.</summary>
    public static class Aiming
    {
        static readonly float Diag = Mathf.Sqrt(0.5f);

        /// <summary>
        /// Up aims up, and up while moving aims up and forward. In the air, down aims down, and down while moving
        /// aims down and forward. Otherwise the soldier aims the way they face.
        /// </summary>
        public static Vector2 Direction(bool up, bool down, float moveX, int facing, bool grounded)
        {
            bool moving = moveX != 0f;
            if (up) return moving ? new Vector2(facing * Diag, Diag) : Vector2.up;
            if (down && !grounded) return moving ? new Vector2(facing * Diag, -Diag) : Vector2.down;
            return new Vector2(facing, 0f);
        }

        /// <summary>The aim's vertical part for drawing: 1 up, -1 down, 0 level.</summary>
        public static int AimY(Vector2 aim) => aim.y > 0.1f ? 1 : aim.y < -0.1f ? -1 : 0;

        /// <summary>The aim is a diagonal.</summary>
        public static bool IsDiagonal(Vector2 aim) => Mathf.Abs(aim.x) > 0.1f && Mathf.Abs(aim.y) > 0.1f;

        /// <summary>The aim back from what a pose keeps of it (a partner's soldier): the inverse of <see cref="AimY"/> and <see cref="IsDiagonal"/>.</summary>
        public static Vector2 FromPose(int facing, int aimY, bool diagonal)
        {
            if (aimY == 0) return new Vector2(facing, 0f);
            return diagonal ? new Vector2(facing * Diag, aimY * Diag) : new Vector2(0f, aimY);
        }
    }
}
