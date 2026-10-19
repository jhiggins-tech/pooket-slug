using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// kie's tabk's guns as numbers (<c>docs/design/tanks.md</c>, kie's tabk): his kit, tabk-sized, 5, 3 and 1 rounds as
    /// every tabk has (<see cref="PlayerTankSpec"/>). The reels on the projector spin while a shot charges.
    /// </summary>
    public sealed class KieTabkSpec
    {
        /// <summary>
        /// Weasel Pop (FIRE): a round spins the reels up for <see cref="Charge"/> seconds, then the lens tumbles out a
        /// volley of weasels along the aim, bigger and harder than his own, that hunt and pop as his do.
        /// </summary>
        public float Charge = 0.35f;
        public readonly WeaselPopSpec Weasels = new WeaselPopSpec
        {
            Count = 5, SpreadDegrees = 8f, ThrowSpeed = 11f, Lift = 7f, Carry = 0.5f, Soften = 0.1f, MaxOut = 15,
            HuntRange = 11f, Damage = 3, BlastRadius = 1.5f, BlastSize = 0.8f,
        };

        /// <summary>
        /// Trollogram (SP): the reels spin for <see cref="BeamTime"/> and the lens beams a Trollogram of the whole tabk
        /// <see cref="BeamAhead"/> in front of it, on the screen. Goons shoot it rather than you; the first hit blows it up
        /// on every goon nearby, bigger than his own. It can't be swapped with (a tabk can't glitch about).
        /// </summary>
        public float BeamTime = 0.45f, BeamAhead = 3.6f;
        public readonly TrollogramSpec Trollogram = new TrollogramSpec { Life = 10f, BlastRadius = 3.2f, BlastSize = 1.8f, Damage = 6 };

        /// <summary>
        /// Where the Trollogram stands, for a tabk at <paramref name="x"/> facing <paramref name="facing"/>: ahead of it,
        /// kept on the screen (between <paramref name="left"/> and <paramref name="right"/>) by a tabk's half width.
        /// </summary>
        public float DecoyX(float x, int facing, float left, float right, float halfWidth) =>
            Mathf.Clamp(x + (facing >= 0 ? 1f : -1f) * BeamAhead, left + halfWidth, right - halfWidth);

        /// <summary>
        /// Steal (SU): the roulette spins over the toughest enemy on screen and lifts a power-up off it, as on foot
        /// (<see cref="StealSpec"/>): with nobody to rob, the round isn't spent. The loot is kie's, and while he drives it
        /// fires from the lens: FIRE shoots it, from its own ammo, until it runs out (<see cref="FiresLoot"/>).
        /// </summary>
        public static bool FiresLoot(int lootAmmo) => lootAmmo > 0;

        /// <summary>The reels spin while a shot charges: Weasel Pop's, the beam, or the roulette.</summary>
        public static bool ReelsSpin(bool charging, bool beaming, bool stealing) => charging || beaming || stealing;

        /// <summary>Frames a second the reels turn at while they spin.</summary>
        public float ReelFrames = 14f;
    }
}
