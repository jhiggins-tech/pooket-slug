namespace PooketSlug
{
    /// <summary>
    /// Words on the shell's screens that send the player to a button somewhere else: the co-op HUD's waiting line,
    /// the victory screen's co-op line, the mission screen's hint and the kit's keyboard key. They are kept here, by
    /// the button they name, so a button renamed is a test that fails rather than a screen that points at nothing
    /// (the stale CO-OP and PLAY lines, 25 Oct). <c>ScreenWordsTests</c> holds them to the buttons.
    /// </summary>
    public static class ScreenWords
    {
        /// <summary>The den's way into co-op, its button on the couch (<c>DenConsole</c>), as sentences name it.</summary>
        public const string CoopButton = "MULTI-PLAYER";

        /// <summary>
        /// The same button's face: MULTI over PLAYER, two short lines so it fits the button (owner, 25 Oct,
        /// multiplayer-button).
        /// </summary>
        public const string CoopButtonFace = "MULTI\nPLAYER";

        /// <summary>
        /// The keyboard key shown for the kit: the ability's key, B (I does the same), which also opens the kit
        /// screen from the den and the pause menu (<c>InputSources</c>: <c>AbilityPressed</c>).
        /// </summary>
        public const string KitKey = "B";

        /// <summary>The co-op HUD while nobody has joined yet.</summary>
        public const string CoopWaiting = "WHOEVER PRESSES " + CoopButton + " NEXT JOINS YOU";

        /// <summary>The victory screen, after the win that opened co-op.</summary>
        public const string CoopOpened = "CO-OP IS OPEN: PRESS " + CoopButton + " IN THE DEN TO PLAY WITH A FRIEND";

        /// <summary>The mission screen's go button: PICK when the den opened it (it always does), else PLAY.</summary>
        public static string MissionGo(bool fromDen) => fromDen ? "PICK" : "PLAY";

        /// <summary>The mission screen's hint, naming the go button it shows.</summary>
        public static string MissionHint(bool touch, bool fromDen) =>
            touch ? "TAP A MISSION, THEN " + MissionGo(fromDen) : "LEFT / RIGHT PICKS A MISSION";
    }
}
