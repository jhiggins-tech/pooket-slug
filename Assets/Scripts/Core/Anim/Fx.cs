// The effects that play on a beat: blinks, flashes, shivers and flickers, and the named rates each one runs at, so a
// Trollogram flickers the same on the boss, in a cutscene and on the HUD. Each site keeps the rate it always had.
namespace PooketSlug
{
    /// <summary>A beat: <see cref="Hz"/> steps a second, round <see cref="Of"/> steps (a blink is two: on, off).</summary>
    public readonly struct Pulse
    {
        public readonly float Hz;
        public readonly int Of;

        public Pulse(float hz, int of = 2)
        {
            Hz = hz;
            Of = of < 1 ? 1 : of;
        }

        /// <summary>The step it's on at <paramref name="t"/>, from 0; <paramref name="phase"/> (in steps) puts one a little ahead of another.</summary>
        public int Step(float t, float phase = 0f) => (int)(t * Hz + phase) % Of;

        /// <summary>On its first step: a blink's on, or a flicker's one step in <see cref="Of"/>.</summary>
        public bool On(float t, float phase = 0f) => Step(t, phase) == 0;

        /// <summary><paramref name="amount"/> one way on its first step and back the other way on the rest: a shiver.</summary>
        public float Shiver(float t, float amount) => On(t) ? amount : -amount;
    }

    /// <summary>The named beats, and helpers for a site with a rate of its own.</summary>
    public static class Fx
    {
        /// <summary>Whether a two-step blink at <paramref name="hz"/> is on at <paramref name="t"/>.</summary>
        public static bool Blink(float t, float hz) => (int)(t * hz) % 2 == 0;

        /// <summary>The step of <paramref name="of"/> a cycle at <paramref name="hz"/> is on at <paramref name="t"/>.</summary>
        public static int Step(float t, float hz, int of, float phase = 0f) => new Pulse(hz, of).Step(t, phase);

        /// <summary>A shiver at <paramref name="hz"/>: <paramref name="amount"/> one way, then the other.</summary>
        public static float Shiver(float t, float hz, float amount) => Blink(t, hz) ? amount : -amount;

        // Trollograms: a dip one step in nine, a jump sideways one step in seven
        public static readonly Pulse TrolloFlicker = new Pulse(30f, 9);
        public static readonly Pulse TrolloJitter = new Pulse(20f, 7);
        /// <summary>A Trollogram fizzing in or out.</summary>
        public static readonly Pulse TrolloFizz = new Pulse(30f);

        // soldiers
        /// <summary>Your soldier, hurt and safe for a moment (on the real clock: it blinks through hit-stop).</summary>
        public static readonly Pulse HurtBlink = new Pulse(20f);
        /// <summary>A soldier fading after dying.</summary>
        public static readonly Pulse DeathFlicker = new Pulse(12f);
        /// <summary>A pilot or a goon tensing up before a move.</summary>
        public static readonly Pulse TellFlash = new Pulse(14f);
        /// <summary>A goon flickering out as it dies.</summary>
        public static readonly Pulse DyingFlicker = new Pulse(16f);

        /// <summary>A thing shaking in a cutscene (<c>shake</c>): nudged across and back.</summary>
        public static readonly Pulse SceneShake = new Pulse(20f);
        // tabks and drones
        /// <summary>A tabk running away by itself.</summary>
        public static readonly Pulse RunawayBlink = new Pulse(12f);
        /// <summary>A drone's rotor (the cargo drone, the goons' drones).</summary>
        public static readonly Pulse Rotor = new Pulse(20f);
        /// <summary>A welder's spark.</summary>
        public static readonly Pulse Spark = new Pulse(24f);

        // the HUD
        /// <summary>A meter that's full and ready: the super, tones2's gauge.</summary>
        public static readonly Pulse SuperReady = new Pulse(6f);
        /// <summary>A warning: no fuel left, the call-in gauge full.</summary>
        public static readonly Pulse Warning = new Pulse(4f);
        /// <summary>The team super's title.</summary>
        public static readonly Pulse TeamTitle = new Pulse(12f);
    }
}
