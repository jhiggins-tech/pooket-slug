using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// How the title logo moves (title-logo-animation, Jack's picks A2, H2 and G3), as pure functions of the seconds
    /// since the logo's entrance began. In: the tabk drives in from the left along the top of the word, each letter
    /// springing up as its nose passes; it stops, recoils as if firing (a sparkle at the muzzle), and the letters past
    /// it pop up in a quick run. Then it hovers (the logo bobs gently while its shadow, which stays put, darkens as it
    /// dips and fades as it rises), and glints: a band of light sweeps over the gold every few seconds, ending in a
    /// sparkle off the tip of the barrel. Its sound: the engine turning over and revving as the drive starts, a cannon
    /// shot with the recoil, then the title music; the hover and glint are silent. Distances are the whole logo's pixels (<see cref="TitleLogo"/>), y up.
    /// </summary>
    public static class TitleMotion
    {
        /// <summary>When the entrance starts, in seconds after the splash starts fading up from black.</summary>
        public const float Start = 0.5f;

        // the drive: where the tabk starts (off the left of any phone's screen), for how long, and its rumble
        public const float DriveFrom = -1500f, Drive = 1.9f, RumbleHeight = 3f, RumbleRate = 28f;
        // a letter springing up: how long, and how far past full height it stretches
        public const float Spring = 0.3f, Stretch = 1.2f;
        // the shot: a moment after the stop; the recoil (how far back, how long) and the shake (canvas units, seconds)
        public const float FireAfter = 0.15f, Recoil = 30f, RecoilFor = 0.42f, ShakeHeight = 2.5f, ShakeFor = 0.22f;
        // the letters past the tabk: after the stop, and between each
        public const float PopAfter = 0.26f, PopGap = 0.11f;
        // the title music: just after the shot rings out
        public const float MusicAfter = 0.1f;

        // the hover: how high (canvas units), how slow, how long it takes to grow once the logo's in
        public const float BobHeight = 1.5f, BobPeriod = 3.2f, BobRamp = 0.8f;
        // the shadow at the top of the bob: how much it fades, and shrinks
        public const float ShadowFade = 0.33f, ShadowShrink = 0.015f;

        // the glint: how often, how long a sweep, the band's width, and when in a sweep the barrel pings
        public const float GlintEvery = 6f, Sweep = 0.9f, BandWidth = 90f, PingAt = 0.64f;
        // a sparkle: how long, how big (canvas units) at full size, how far it turns
        public const float Sparkle = 0.56f, SparkleSize = 22f, SparkleTurn = 90f, MuzzleSparkle = 1.8f, PingSparkle = 1.4f;

        /// <summary>The tip of the tabk's barrel, in the whole logo (from its top left, y down, as the parts are).</summary>
        public static readonly Vector2 BarrelTip = new Vector2(828f, 66f);

        /// <summary>Where a part is and how tall, this moment: its offset from its place, and its height (0 hidden, 1 full).</summary>
        public readonly struct Pose
        {
            public readonly float X, Y, Height;

            public Pose(float x, float y, float height)
            {
                X = x;
                Y = y;
                Height = height;
            }

            public static readonly Pose Rest = new Pose(0f, 0f, 1f);
        }

        /// <summary>How far through the drive the tabk is, 0 to 1 (it slows to a stop).</summary>
        static float Driven(float t) => 1f - Cube(1f - Mathf.Clamp01(t / Drive));

        static float Cube(float v) => v * v * v;

        /// <summary>The tabk's nose at rest: the front of its tracks (its part's right edge, less the padding).</summary>
        static float Nose(TitleLogo logo) => logo.TabkPart.X + logo.TabkPart.W - TitleLogo.Pad;

        /// <summary>When the tabk stops.</summary>
        public static float Stop => Drive;

        /// <summary>When the tabk fires.</summary>
        public static float Fire => Stop + FireAfter;

        /// <summary>When a letter springs up: as the nose passes its middle, or for the letters past the nose, in a run after the shot.</summary>
        public static float SpringsAt(TitleLogo logo, int index)
        {
            float centre = logo.Parts[index].CentreX, nose = Nose(logo);
            if (centre < nose)
            {
                // the nose is at nose + DriveFrom * (1 - driven): solve for when it reaches the middle
                float driven = 1f - (centre - nose) / DriveFrom;
                return Drive * (1f - Mathf.Pow(1f - Mathf.Clamp01(driven), 1f / 3f));
            }
            int after = 0;
            for (int i = 0; i < index; i++)
                if (i != logo.TabkIndex && logo.Parts[i].CentreX >= nose) after++;
            return Stop + PopAfter + after * PopGap;
        }

        /// <summary>When the entrance is over: the last letter fully up.</summary>
        public static float End(TitleLogo logo)
        {
            float end = Fire + RecoilFor;
            for (int i = 0; i < logo.Parts.Count; i++)
                if (i != logo.TabkIndex) end = Mathf.Max(end, SpringsAt(logo, i) + Spring);
            return end;
        }

        /// <summary>A part, <paramref name="t"/> seconds into the entrance (the hover is on top of this).</summary>
        public static Pose Part(TitleLogo logo, int index, float t)
        {
            if (index == logo.TabkIndex)
            {
                if (t < 0f) return new Pose(DriveFrom, 0f, 1f);
                float x = DriveFrom * (1f - Driven(t));
                float rumble = t < Stop ? Mathf.Abs(Mathf.Sin(t * RumbleRate)) * RumbleHeight * (1f - Driven(t) * 0.5f) : 0f;
                float r = (t - Fire) / RecoilFor;
                if (r > 0f && r < 1f) x -= Recoil * (r < 0.25f ? r / 0.25f : 1f - (r - 0.25f) / 0.75f);
                return new Pose(x, rumble, 1f);
            }
            float s = (t - SpringsAt(logo, index)) / Spring;
            if (s <= 0f) return new Pose(0f, 0f, 0f);
            if (s >= 1f) return Pose.Rest;
            // up past full height, then back
            float height = s < 0.6f ? Stretch * Mathf.Sin(s / 0.6f * Mathf.PI * 0.5f) : Mathf.Lerp(Stretch, 1f, Mathf.SmoothStep(0f, 1f, (s - 0.6f) / 0.4f));
            return new Pose(0f, 0f, height);
        }

        /// <summary>The shot's shake, in canvas units: a quick jolt that dies away.</summary>
        public static Vector2 Shake(float t)
        {
            float s = (t - Fire) / ShakeFor;
            if (s <= 0f || s >= 1f) return Vector2.zero;
            float a = ShakeHeight * (1f - s);
            return new Vector2(a * Mathf.Sin(s * 31f), -a * 0.6f * Mathf.Sin(s * 23f + 1f));
        }

        /// <summary>The menu's fade in (START, SETTINGS and the hints), 0 to 1, once the logo's in.</summary>
        public static float MenuIn(TitleLogo logo, float t) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - End(logo)) / 0.5f));

        /// <summary>The hover's height, in canvas units, up: a slow bob, growing in once the logo's in.</summary>
        public static float Bob(TitleLogo logo, float t)
        {
            float s = t - End(logo);
            if (s <= 0f) return 0f;
            return Mathf.Sin(s * 2f * Mathf.PI / BobPeriod) * BobHeight * Mathf.Clamp01(s / BobRamp);
        }

        /// <summary>The shadow under the bob: how dark (1 as baked, at the bottom of the bob) and how big (1 as baked). It doesn't move.</summary>
        public static float ShadowAlpha(TitleLogo logo, float t) => 1f - ShadowFade * (1f + Bob(logo, t) / BobHeight) * 0.5f;

        public static float ShadowScale(TitleLogo logo, float t) => 1f - ShadowShrink * Bob(logo, t) / BobHeight;

        /// <summary>
        /// How far across the logo the glint's band is, 0 (its left edge) to 1 (past its right), or below 0 between
        /// sweeps. The first sweeps as the logo lands, then one every <see cref="GlintEvery"/> seconds.
        /// </summary>
        public static float Glint(TitleLogo logo, float t)
        {
            float s = t - End(logo);
            if (s < 0f) return -1f;
            float into = s % GlintEvery;
            if (into >= Sweep) return -1f;
            return Mathf.SmoothStep(0f, 1f, into / Sweep);
        }

        /// <summary>Where the band's middle is, in the whole logo's pixels from its left: off one side to off the other.</summary>
        public static float BandX(TitleLogo logo, float glint) => Mathf.Lerp(-BandWidth, logo.Width + BandWidth, glint);

        /// <summary>
        /// The sparkle at the barrel's tip: its size (0 none, 1 a ping's full size; the shot's is bigger) and its turn,
        /// in degrees. It flares at the shot and as each sweep reaches the barrel.
        /// </summary>
        public static float SparkleAt(TitleLogo logo, float t, out float turn)
        {
            float big = MuzzleSparkle, since = t - Fire;
            float s = t - End(logo);
            if (s >= 0f && (since < 0f || since >= Sparkle))
            {
                since = s % GlintEvery - PingAt;
                big = PingSparkle;
            }
            turn = 0f;
            if (since < 0f || since >= Sparkle) return 0f;
            float k = since / Sparkle;
            turn = SparkleTurn * k;
            return big * (k < 0.5f ? k / 0.5f : 1f - (k - 0.5f) / 0.5f);
        }

        /// <summary>When the title music starts: just after the shot, as S-L-U-G pop up.</summary>
        public static float MusicAt => Fire + MusicAfter;

        /// <summary>The tabk fired between <paramref name="from"/> and <paramref name="to"/> (for its sound).</summary>
        public static bool Fired(float from, float to) => from < Fire && to >= Fire;

    }
}
