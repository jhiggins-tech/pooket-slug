// The snacks' little shows on the stage, for the skits (docs/design/cutscenes/skits.md): the crisp packet turned round
// to its label (Five-second rule), the label's close-up coming up, and a soft-serve cone splatting on whoever it hits
// (Does it do cones?). Plain timings; CutsceneSnacks draws them.
using UnityEngine;

namespace PooketSlug
{
    /// <summary>The snacks' shows: the packet turned round, the label's close-up, the soft-serve splat.</summary>
    public static class SnackShow
    {
        /// <summary>How narrow the packet gets edge on, as it turns (never quite nothing, so it doesn't vanish).</summary>
        public const float EdgeOn = 0.1f;

        /// <summary>
        /// How wide the packet is <paramref name="k"/> of the way through turning round (0 to 1): its front narrowing to
        /// its edge, then its back widening out.
        /// </summary>
        public static float BagWidth(float k) => Mathf.Max(EdgeOn, Mathf.Abs(Mathf.Cos(Mathf.Clamp01(k) * Mathf.PI)));

        /// <summary>Is its back (the label) out, <paramref name="k"/> of the way through turning round? From halfway, and once it's turned.</summary>
        public static bool LabelOut(float k) => k >= 0.5f;

        /// <summary>How long the label's close-up takes to come up into view.</summary>
        public const float LabelRise = 0.35f;

        /// <summary>How far up the close-up is, <paramref name="since"/> seconds after it's shown (0 to 1, slowing as it arrives).</summary>
        public static float LabelUp(float since)
        {
            float k = Mathf.Clamp01(since / LabelRise);
            return 1f - (1f - k) * (1f - k);
        }

        /// <summary>The splat's burst: three frames, quick, then held while the drips take over.</summary>
        public static readonly Clip SplatBurst = new Clip("splat", 3, 12f, loop: false);

        /// <summary>The soft serve left on them, dripping: four frames, round and round.</summary>
        public static readonly Clip SplatDrip = new Clip("drip", 4, 5f);

        /// <summary>The splat's frames in all: the burst's, then the drips'.</summary>
        public static int SplatFrames => SplatBurst.Frames + SplatDrip.Frames;

        /// <summary>The splat's frame <paramref name="since"/> seconds after it hit: through the burst once, then dripping for as long as it's shown.</summary>
        public static int SplatFrame(float since)
        {
            if (since < SplatBurst.Length) return SplatBurst.Frame(since);
            return SplatBurst.Frames + SplatDrip.Frame(since - SplatBurst.Length);
        }
    }
}
