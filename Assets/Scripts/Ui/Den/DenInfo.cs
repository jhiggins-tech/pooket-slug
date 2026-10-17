namespace PooketSlug
{
    /// <summary>What the den shows besides the <see cref="Den"/> itself.</summary>
    public struct DenInfo
    {
        /// <summary>The mission on the TV and the board (null in CREW, which has none).</summary>
        public string Mission;
        /// <summary>The co-op room's code for the TV, or null.</summary>
        public string Room;
        /// <summary>The room's invite link (tapping the TV shares it), or null.</summary>
        public string InviteLink;
        /// <summary>The host can list the room in the room browser or keep it invite only, and which it is.</summary>
        public bool CanList, Listed;
        /// <summary>People watching the room (the TV says so).</summary>
        public int Watchers;
        /// <summary>Tapping the empty second controller hosts a room (solo, once co-op's open).</summary>
        public bool CanHost;
        /// <summary>Our colours (<see cref="Art.SkinFor"/>): 0 player one's, 1 player two's.</summary>
        public int Skin;
    }
}
