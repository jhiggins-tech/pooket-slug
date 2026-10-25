namespace PooketSlug
{
    /// <summary>
    /// The settings screen's three CONTROLS lines, for touch or keys (<c>docs/design/player.md</c>, Controls). Each kit
    /// button shows once the player can use it, as the touch pad shows it: SU once the super's been met, AB for a soldier
    /// with an ability (the HUD's <see cref="KitMeters.HasAbility"/>), and TEAM once co-op is open. The keys are
    /// <c>Input/InputSources.cs</c>'s.
    /// </summary>
    public static class ControlsLines
    {
        /// <summary>The soldier has an ability (AB), as the HUD and the touch pad decide it: a cooldown to wait out.</summary>
        public static bool HasAbility(SoldierDef soldier) => soldier != null && soldier.Ability != null && soldier.AbilityCooldown > 0f;

        /// <summary>The three lines, top first.</summary>
        public static string[] For(bool touch, bool leftHanded, bool super, bool ability, bool coop)
        {
            if (touch)
            {
                string kit = (ability ? "AB  ABILITY      " : "") + (coop ? "TEAM  BOTH SUPERS AT ONCE      " : "");
                return new[]
                {
                    (leftHanded ? "RIGHT" : "LEFT") + " THUMB  RUN, AIM UP, CROUCH (AIM DOWN IN THE AIR)",
                    (leftHanded ? "LEFT" : "RIGHT") + " THUMB  FIRE (HOLD IT), JUMP, SP" + (super ? ", SU (SUPER, FROM KILLS)" : ""),
                    kit + "DROP  DOWN + JUMP      II  PAUSE",
                };
            }
            return new[]
            {
                "MOVE  Arrows / WASD      AIM  Up / Down (in air), + run for 45°      DROP  Down + Jump",
                "JUMP  Z / Space      FIRE  X / J      SP  C / L" + (super ? "      SUPER  V / U" : "") + (ability ? "      AB  B / I" : "") + (coop ? "      TEAM  T / Y" : ""),
                "PAUSE  P / Enter      RESTART  R      MUTE  M      Gamepad supported",
            };
        }
    }
}
