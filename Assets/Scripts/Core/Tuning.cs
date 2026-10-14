namespace PooketSlug
{
    /// <summary>
    /// The game's feel in one place: physics, the soldier's movement, the camera, and co-op timings.
    /// Numbers that belong to one weapon, enemy or pickup stay with it until content moves into data
    /// (refactor plan, phase 2).
    /// </summary>
    public static class Tuning
    {
        public static class Physics
        {
            /// <summary>World units per second squared; one unit is one 16 px tile.</summary>
            public const float Gravity = 40f;
            public const float MaxFall = 18f;
        }

        public static class Soldier
        {
            public const float RunSpeed = 5.5f, CrawlSpeed = 2f, JumpVelocity = 17f;
            /// <summary>Gravity multiplier while rising with jump released, for a short hop.</summary>
            public const float ShortHopGravity = 2.2f;
            public const float CoyoteTime = 0.1f, JumpBuffer = 0.12f, DropThroughTime = 0.25f;
            public const float HalfWidth = 0.3f, StandHeight = 1.35f, CrouchHeight = 0.8f;
            public const int MaxHp = 5, StartGrenades = 10;
            public const float HurtInvulnerability = 1.4f;
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
            public const float RespawnDelay = 2.5f, RespawnInvulnerability = 2.5f;
            public const int RespawnMinGrenades = 5;
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

        public static class Hud
        {
            public const float PopupRise = 0.8f, PopupLife = 1.4f;
        }
    }
}
