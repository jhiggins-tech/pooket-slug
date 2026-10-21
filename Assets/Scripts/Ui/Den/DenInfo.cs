namespace PooketSlug
{
    /// <summary>What the den shows besides the <see cref="Den"/> itself.</summary>
    public struct DenInfo
    {
        /// <summary>The mission on the TV and the board (null in CREW, which has none).</summary>
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
    }
}
