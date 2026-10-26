namespace PooketSlug
{
    /// <summary>What the den shows besides the <see cref="Den"/> itself.</summary>
    public struct DenInfo
    {
        /// <summary>The mission on the TV and the board (null in DenMode.Crew, which has none).</summary>
        public string Mission;
        /// <summary>The co-op room's name for the TV (MATES, see CoopRoom), or null.</summary>
        public string Room;
        /// <summary>The room's invite link (tapping the TV shares it), or null.</summary>
        public string InviteLink;
        /// <summary>People watching the room (the TV says so).</summary>
        public int Watchers;
        /// <summary>Tapping the empty second controller hosts a room (solo, once co-op's open).</summary>
        public bool CanHost;
        /// <summary>The roller door to the tabk bay: how far it's rolled up, and how far through it you are (<see cref="RollerDoor"/>).</summary>
        public float DoorLift, DoorThrough;
        /// <summary>The co-op button's size and glow as Two of us now pops it up and pulses it once (1 and 0 the rest of the time).</summary>
        public float HostScale, HostGlow;
        /// <summary>The couch's loose controllers picked up in a skit, by seat: bit 0 the left seat's, bit 1 the right's.</summary>
        public int PadsTaken;
        /// <summary>The TV's off (Special delivery: tones2 turns it back on at the end).</summary>
        public bool TvOff;
    }
}
