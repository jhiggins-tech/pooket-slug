using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Mission two's goons (<c>docs/design/levels.md</c>, Mission two): the shield soldier, blocked from the front, so
    /// you jump over him or get behind; and the grenadier, who lobs from cover, so you move off your spot. Their numbers
    /// and rules; <c>Enemies/CreameryGoons.cs</c> plays them.
    /// </summary>
    public static class ShieldSpec
    {
        /// <summary>How fast he plods at you, and how close he comes before he stops to shoot.</summary>
        public const float WalkSpeed = 1.2f, StopAt = 5.5f;
        /// <summary>How long he takes to swing his shield round once you're behind him: the time to hit his back.</summary>
        public const float TurnDelay = 0.7f;
        /// <summary>Seconds between his pistol shots, fired past the shield's edge.</summary>
        public const float FireEvery = 2.4f;
        /// <summary>How high the shield covers him, from his feet: a shot over it (or from straight above) gets through.</summary>
        public const float ShieldTop = 1.2f;

        /// <summary>
        /// A shot moving at <paramref name="shotVel"/> that meets him at <paramref name="hitAt"/> stops on the shield: it
        /// comes at the side he faces, lower than the shield's top, and not falling on him from above.
        /// </summary>
        public static bool Blocks(int facing, Vector2 feet, Vector2 hitAt, Vector2 shotVel)
        {
            if (shotVel.x * facing >= 0f) return false; // from behind, or straight down
            if (Mathf.Abs(shotVel.y) > Mathf.Abs(shotVel.x) * 1.5f && shotVel.y < 0f) return false; // dropped on him
            if (hitAt.y - feet.y > ShieldTop) return false; // over the top
            return (hitAt.x - feet.x) * facing > -0.1f; // on the shield's side of him
        }
    }

    /// <summary>The grenadier: stays behind his cover and lobs grenades that land where you're standing.</summary>
    public static class GrenadierSpec
    {
        /// <summary>He throws at soldiers between these ranges, every so often, with a wind-up first (the tell).</summary>
        public const float MinRange = 3f, MaxRange = 11f, ThrowEvery = 2.8f, WindUp = 0.55f;
        /// <summary>How long a grenade's in the air: long enough to see it coming and step aside.</summary>
        public const float Flight = 1.05f;

        /// <summary>The throw that lands a lob from <paramref name="from"/> on <paramref name="to"/> after <paramref name="flight"/> seconds.</summary>
        public static Vector2 LobTo(Vector2 from, Vector2 to, float gravity, float flight)
        {
            float t = Mathf.Max(0.1f, flight);
            return new Vector2((to.x - from.x) / t, (to.y - from.y) / t + 0.5f * gravity * t);
        }

        /// <summary>A soldier at <paramref name="dx"/> (along the floor) is in range for a throw.</summary>
        public static bool InRange(float dx) => Mathf.Abs(dx) >= MinRange && Mathf.Abs(dx) <= MaxRange;
    }
}
