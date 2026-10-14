namespace PooketSlug
{
    /// <summary>Everything needed to draw a soldier. Produced by the local soldier and replicated for remote ones.</summary>
    public struct AvatarPose
    {
        const int FacingRightBit = 1 << 0, AimUpBit = 1 << 1, AimDownBit = 1 << 2, GroundedBit = 1 << 3, CrouchBit = 1 << 4;
        const int RunningBit = 1 << 5, DeadBit = 1 << 6, MuzzleBit = 1 << 7, BlinkBit = 1 << 8, FlashBit = 1 << 9;

        public int Facing;  // 1 = right, -1 = left
        public int AimY;    // 1 = up, -1 = down, 0 = forward
        public bool Grounded, Crouching, Running, Dead, Muzzle, Blink, Flash;
        public float Anim, DeadTime;

        public int Pack() =>
            (Facing > 0 ? FacingRightBit : 0) | (AimY > 0 ? AimUpBit : 0) | (AimY < 0 ? AimDownBit : 0) |
            (Grounded ? GroundedBit : 0) | (Crouching ? CrouchBit : 0) | (Running ? RunningBit : 0) |
            (Dead ? DeadBit : 0) | (Muzzle ? MuzzleBit : 0) | (Blink ? BlinkBit : 0) | (Flash ? FlashBit : 0);

        /// <summary>Unpacks replicated flags; animation clocks are kept locally by the caller.</summary>
        public static AvatarPose Unpack(int flags, float anim, float deadTime) => new AvatarPose
        {
            Facing = (flags & FacingRightBit) != 0 ? 1 : -1,
            AimY = (flags & AimUpBit) != 0 ? 1 : (flags & AimDownBit) != 0 ? -1 : 0,
            Grounded = (flags & GroundedBit) != 0,
            Crouching = (flags & CrouchBit) != 0,
            Running = (flags & RunningBit) != 0,
            Dead = (flags & DeadBit) != 0,
            Muzzle = (flags & MuzzleBit) != 0,
            Blink = (flags & BlinkBit) != 0,
            Flash = (flags & FlashBit) != 0,
            Anim = anim,
            DeadTime = deadTime,
        };
    }
}
