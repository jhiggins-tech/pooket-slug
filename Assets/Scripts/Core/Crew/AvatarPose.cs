using UnityEngine;

namespace PooketSlug
{
    /// <summary>Everything needed to draw a soldier. Produced by the local soldier and replicated for remote ones.</summary>
    public struct AvatarPose
    {
        const int FacingRightBit = 1 << 0, AimUpBit = 1 << 1, AimDownBit = 1 << 2, GroundedBit = 1 << 3, CrouchBit = 1 << 4;
        const int RunningBit = 1 << 5, DeadBit = 1 << 6, MuzzleBit = 1 << 7, BlinkBit = 1 << 8, FlashBit = 1 << 9;
        const int DiagonalBit = 1 << 10, OutBit = 1 << 11, GoneBit = 1 << 12;
        // tones2's kit, so a partner's copy can pour out the same liquids: the jet and its pressure (3 bits), the spew,
        // and shaking before a rocket jump
        const int JetBit = 1 << 13, PressureShift = 14, PressureMask = 7 << PressureShift, SpewBit = 1 << 17, RootedBit = 1 << 18;
        // the super's charged, so a partner's pad can show TEAM (team supers)
        const int SuperReadyBit = 1 << 19;
        // presence (spike coop-camera): the page has lost focus, and a beat that ticks while the game runs
        const int AwayBit = 1 << 20, BeatShift = 21, BeatMask = 3 << BeatShift;

        public int Facing;  // 1 = right, -1 = left
        public int AimY;    // 1 = up, -1 = down, 0 = forward
        /// <summary>Aiming up or down and forward, at 45°.</summary>
        public bool Diagonal;
        /// <summary>Out of lives (offered a continue, or out): a partner's 1-up goes to them first.</summary>
        public bool Out;
        /// <summary>Out of lives and continues: waiting on a partner's 1-up.</summary>
        public bool Gone;
        public bool Grounded, Crouching, Running, Dead, Muzzle, Blink, Flash;
        /// <summary>tones2: firing his jet, at <see cref="Pressure"/> (0 to 1, in sevenths on the wire); spewing; shaking before a rocket jump.</summary>
        public bool Jetting, Spewing, Rooted;
        /// <summary>The super is charged: with the partner's too, a team super can be fired.</summary>
        public bool SuperReady;
        /// <summary>The player's page has lost focus (they've switched to another window or app).</summary>
        public bool Away;
        /// <summary>
        /// Counts 0 to 3 while the game runs (<see cref="PartnerBeacon.BeatAt"/>); a beat that stops changing means their
        /// game has stopped, as a phone's does in the background (<see cref="Presence"/>).
        /// </summary>
        public int Beat;
        public float Pressure;
        public float Anim, DeadTime;

        public int Pack() =>
            (Facing > 0 ? FacingRightBit : 0) | (AimY > 0 ? AimUpBit : 0) | (AimY < 0 ? AimDownBit : 0) |
            (Grounded ? GroundedBit : 0) | (Crouching ? CrouchBit : 0) | (Running ? RunningBit : 0) |
            (Dead ? DeadBit : 0) | (Muzzle ? MuzzleBit : 0) | (Blink ? BlinkBit : 0) | (Flash ? FlashBit : 0) |
            (Diagonal && AimY != 0 ? DiagonalBit : 0) | (Out ? OutBit : 0) | (Gone ? GoneBit : 0) |
            (Jetting ? JetBit | (Mathf.Clamp(Mathf.RoundToInt(Pressure * 7f), 0, 7) << PressureShift) : 0) |
            (Spewing ? SpewBit : 0) | (Rooted ? RootedBit : 0) | (SuperReady ? SuperReadyBit : 0) |
            (Away ? AwayBit : 0) | ((Beat & 3) << BeatShift);

        /// <summary>
        /// This pose held still, as a Trollogram holds it (kie's, <c>TrollDecoy</c>, and his pilot's,
        /// <c>PilotDecoy</c>): frozen mid-whatever, standing (never drawn dead), never firing, blinking or running.
        /// </summary>
        public AvatarPose Still()
        {
            AvatarPose p = this;
            p.Muzzle = p.Blink = p.Flash = p.Running = p.Dead = false;
            return p;
        }

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
            Diagonal = (flags & DiagonalBit) != 0 && (flags & (AimUpBit | AimDownBit)) != 0,
            Out = (flags & OutBit) != 0,
            Gone = (flags & GoneBit) != 0,
            Jetting = (flags & JetBit) != 0,
            Pressure = ((flags & PressureMask) >> PressureShift) / 7f,
            Spewing = (flags & SpewBit) != 0,
            Rooted = (flags & RootedBit) != 0,
            SuperReady = (flags & SuperReadyBit) != 0,
            Away = (flags & AwayBit) != 0,
            Beat = (flags & BeatMask) >> BeatShift,
            Anim = anim,
            DeadTime = deadTime,
        };
    }
}
