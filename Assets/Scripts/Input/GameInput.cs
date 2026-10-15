namespace PooketSlug
{
    /// <summary>The menu and system buttons this frame.</summary>
    public struct MenuInput
    {
        public bool Start, Restart, Pause, Mute;
        public bool Host, Join, Quick, Back; // online co-op menu
        public bool Settings, Shake; // the settings screen
        /// <summary>The touch pad's pause button. The game decides what it means (pause, or leave a shared game).</summary>
        public bool PadPause;

        public void Add(in MenuInput other)
        {
            Start |= other.Start;
            Restart |= other.Restart;
            Pause |= other.Pause;
            Mute |= other.Mute;
            Host |= other.Host;
            Join |= other.Join;
            Quick |= other.Quick;
            Back |= other.Back;
            Settings |= other.Settings;
            Shake |= other.Shake;
            PadPause |= other.PadPause;
        }
    }

    /// <summary>
    /// The local player's input, polled once a frame from every source: the keyboard, a gamepad and the touch
    /// pad. Sources add up, so they can be mixed. Works with either Active Input Handling setting: touch and
    /// gamepads need the Input System package, and the legacy Input Manager covers the keyboard and touch.
    /// </summary>
    public static class GameInput
    {
        /// <summary>The touch arcade pad. The game shows it, lays it out and draws it.</summary>
        public static readonly ArcadePad Pad = new ArcadePad();

        public static SoldierInput Soldier;
        public static MenuInput Menu;
        /// <summary>The last thing the player touched was the screen: show the pad and touch hints.</summary>
        public static bool TouchActive;

        static readonly IInputSource s_Keyboard = new KeyboardSource();
        static readonly IInputSource s_Gamepad = new GamepadSource();
        static readonly IInputSource s_Touch = new TouchSource(Pad);

        public static void Poll()
        {
            var soldier = new SoldierInput();
            var menu = new MenuInput();
            bool keys = s_Keyboard.Read(ref soldier, ref menu);
            bool pad = s_Gamepad.Read(ref soldier, ref menu);
            if (keys || pad) TouchActive = false;
            if (s_Touch.Read(ref soldier, ref menu)) TouchActive = true;
            Soldier = soldier;
            Menu = menu;
        }
    }
}
