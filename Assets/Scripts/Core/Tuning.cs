namespace PooketSlug
{
    /// <summary>
    /// The game's feel in one place: physics, what every soldier shares, the camera, and co-op timings.
    /// Numbers that belong to one soldier, weapon, enemy or pickup are in its definition (<see cref="Content"/>).
    /// </summary>
    public static class Tuning
    {
        public static class Physics
        {
            /// <summary>World units per second squared; one unit is one 16 px tile.</summary>
            public const float Gravity = 40f;
            public const float MaxFall = 18f;
        }

        /// <summary>What every soldier shares. Each one's speed, jump, size and health are in its <see cref="SoldierDef"/>.</summary>
        public static class Soldier
        {
            /// <summary>Every soldier's body and moves, unless their definition says otherwise.</summary>
            public const float RunSpeed = 5.5f, CrawlSpeed = 2f, JumpVelocity = 17f;
            public const float HalfWidth = 0.3f, StandHeight = 1.35f, CrouchHeight = 0.8f;
            public const int MaxHp = 5;
            /// <summary>Gravity multiplier while rising with jump released, for a short hop.</summary>
            public const float ShortHopGravity = 2.2f;
            public const float CoyoteTime = 0.1f, JumpBuffer = 0.12f, DropThroughTime = 0.25f;
            public const float HurtInvulnerability = 1.4f;
        }

        /// <summary>What crates give. Placeholders, to tune in play.</summary>
        public static class Crates
        {
            /// <summary>Charges a bomb crate adds for a soldier whose def doesn't say (a weak special's amount).</summary>
            public const int DefaultSpecials = 10;
            /// <summary>Hearts of health a medkit heals.</summary>
            public const int MedkitHearts = 2;
        }

        /// <summary>Lives and continues, each player's own (<see cref="LifeBank"/>). Placeholders, to tune in play.</summary>
        public static class Lives
        {
            /// <summary>Spare lives at the start of a mission, and after a continue (so three lives in all).</summary>
            public const int Spare = 2;
            public const int MaxSpare = 9;
            public const int Continues = 2;
            /// <summary>Seconds to take a continue before it's let go.</summary>
            public const float ContinueSeconds = 9f;
            /// <summary>Seconds down before coming back, and of invulnerability after.</summary>
            public const float RespawnDelay = 2.5f, RespawnInvulnerability = 2.5f;
            /// <summary>Charges of the special a soldier comes back with, at least.</summary>
            public const int RespawnMinSpecials = 5;
            /// <summary>Seconds after a solo player is out before the game-over screen.</summary>
            public const float GameOverDelay = 1f;
        }

        /// <summary>The kit's meters and the tank call-in. Placeholders, to tune in play.</summary>
        public static class Kit
        {
            /// <summary>The special's charges: 3, each refilling over this many seconds.</summary>
            public const int SpecialCharges = 3;
            public const float SpecialRefill = 6f;
            /// <summary>Kills that fill the super.</summary>
            public const int SuperKills = 12;
            /// <summary>Holding SP longer than this is a hold; then the call-in charges for this long.</summary>
            public const float CallInTapTime = 0.2f, CallInChargeTime = 0.8f;
        }

        /// <summary>How hits feel: knockback and the brief pause on big hits.</summary>
        public static class Impact
        {
            /// <summary>A hit pushes the soldier back this fast, for this long, with a little hop.</summary>
            public const float KnockSpeed = 4f, KnockTime = 0.15f, KnockHop = 3f;
            /// <summary>Solo only: the game pauses this long when a goon dies, longer for a big one.</summary>
            public const float KillStop = 0.035f, BigKillStop = 0.09f;
            /// <summary>Enemies with at least this much health count as big.</summary>
            public const int BigHp = 10;
        }

        public static class Camera
        {
            /// <summary>180 px at 16 px per unit.</summary>
            public const float ViewHeight = 11.25f;
            public const float ScrollSpeed = 12f, LookAhead = 2f;
            public const float ShakeDecay = 0.6f;
            /// <summary>Things spawn when they're this far beyond the right edge of the screen.</summary>
            public const float SpawnAhead = 1.5f;
        }

        public static class Coop
        {
            /// <summary>Both clients scroll and spawn by this half-width, whatever their screen's shape.</summary>
            public const float HalfWidth = 10f;
            public const int MaxPlayers = 2;
            /// <summary>The camera never scrolls the trailing soldier closer than this to the left edge.</summary>
            public const float TrailMargin = 0.5f;
            /// <summary>Followers scroll this many times faster to catch up with the authority's camera.</summary>
            public const float FollowerCatchUp = 2f;
            /// <summary>Minimum gap between contact hits on a partner's soldier.</summary>
            public const float RemoteHurtCooldown = 0.5f;
            /// <summary>A replicated soldier or enemy further than this from where it's drawn jumps there.</summary>
            public const float RemoteSoldierSnap = 3f, RemoteEnemySnap = 4f;
            /// <summary>How quickly replicated things glide toward their latest position (per second).</summary>
            public const float RemoteSoldierSmoothing = 18f, RemoteEnemySmoothing = 15f;
            /// <summary>Seconds of velocity added to a remote soldier's position, to hide a little latency.</summary>
            public const float RemoteSoldierLead = 0.05f;
        }

        public static class Pixels
        {
            /// <summary>The art's pixels in a world unit (a tile): sprites, the crater mask and anything sized in pixels.</summary>
            public const int PerUnit = 16;
        }

        public static class Hud
        {
            /// <summary>The HUD is laid out in units of a screen this tall, whatever its pixels.</summary>
            public const float Height = 360f;
            /// <summary>The HUD's layout is a 16:9 screen; wider or narrower screens stretch or squeeze the gaps.</summary>
            public const float Width = Height * 16f / 9f;
            public const float PopupRise = 0.8f, PopupLife = 1.4f;
        }
    }
}
