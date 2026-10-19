using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// The partner beacon (spike coop-camera, in the game since 19 Oct): your partner is always easy to find (player.md, Co-op). An arrow at the
    /// screen's edge points at a partner who's off it, and their tag warns when their health is low or they've switched
    /// away from the game. The rules are here; <c>HudView</c> draws them and <see cref="Presence"/> works out "away".
    /// </summary>
    public static class PartnerBeacon
    {
        /// <summary>Beats a second (<see cref="AvatarPose.Beat"/>): each one changes the pose, so it's sent even when they stand still.</summary>
        public const float BeatRate = 4f;
        /// <summary>A partner whose beat hasn't changed for this long has stopped running: their page is in the background.</summary>
        public const float StaleAfter = 1.5f;
        /// <summary>Health at or under this share of the most (at least one heart) is low.</summary>
        public const float LowShare = 0.4f;
        /// <summary>How far in from the screen's edge the arrow sits, in world units.</summary>
        public const float Inset = 0.7f;

        /// <summary>The beat to send at this time.</summary>
        public static int BeatAt(float time) => Mathf.FloorToInt(time * BeatRate) & 3;

        /// <summary>Their health is low (and they're not down).</summary>
        public static bool IsLow(int hp, int maxHp) => hp > 0 && hp <= Mathf.Max(1, Mathf.FloorToInt(maxHp * LowShare));

        /// <summary>A soldier standing at <paramref name="feet"/> (this tall) can be seen in <paramref name="view"/>.</summary>
        public static bool OnScreen(Rect view, Vector2 feet, float height) =>
            feet.x > view.xMin && feet.x < view.xMax && feet.y + height > view.yMin && feet.y < view.yMax;

        /// <summary>
        /// Where the arrow goes: on the line from the view's middle to <paramref name="target"/>, where it meets the screen's
        /// edge (<see cref="Inset"/> in). <paramref name="angle"/> is the way it points, degrees up from right.
        /// </summary>
        public static Vector2 EdgePoint(Rect view, Vector2 target, out float angle)
        {
            Vector2 c = view.center, d = target - c;
            angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            float hw = Mathf.Max(0.01f, view.width * 0.5f - Inset), hh = Mathf.Max(0.01f, view.height * 0.5f - Inset);
            float tx = Mathf.Abs(d.x) > 1e-5f ? hw / Mathf.Abs(d.x) : float.MaxValue;
            float ty = Mathf.Abs(d.y) > 1e-5f ? hh / Mathf.Abs(d.y) : float.MaxValue;
            float t = Mathf.Min(tx, ty);
            return t >= float.MaxValue ? c : c + d * Mathf.Min(1f, t);
        }

        /// <summary>How far off they are, in whole metres (a world unit is a metre), for the arrow's label.</summary>
        public static int Metres(Rect view, Vector2 target)
        {
            float dx = Mathf.Max(0f, Mathf.Max(view.xMin - target.x, target.x - view.xMax));
            float dy = Mathf.Max(0f, Mathf.Max(view.yMin - target.y, target.y - view.yMax));
            return Mathf.Max(1, Mathf.RoundToInt(Mathf.Sqrt(dx * dx + dy * dy)));
        }
    }

    /// <summary>
    /// Whether a partner is away: their page lost focus (they said so, <see cref="AvatarPose.Away"/>), or their beat has
    /// stopped, which is all a phone can tell us once it's in the background, because the game stops running there.
    /// </summary>
    public sealed class Presence
    {
        int m_Beat = -1;
        float m_Still;

        public bool Away { get; private set; }

        public void Tick(float dt, AvatarPose pose)
        {
            if (pose.Beat != m_Beat)
            {
                m_Beat = pose.Beat;
                m_Still = 0f;
            }
            else
            {
                m_Still += dt;
            }
            Away = pose.Away || m_Still > PartnerBeacon.StaleAfter;
        }
    }

    /// <summary>
    /// The co-op camera's rule: scroll toward the leading soldier, never pushing the trailing one off the left edge. Only
    /// soldiers who are playing hold it: one who's down doesn't, and (spike coop-camera) nor does one who's away, or a
    /// partner switching apps would stop the scroll for the other until they came back.
    /// </summary>
    public struct ScrollHold
    {
        float m_Lead, m_Trail;
        bool m_Any;

        public bool Any => m_Any;

        public void Add(float x)
        {
            m_Lead = m_Any ? Mathf.Max(m_Lead, x) : x;
            m_Trail = m_Any ? Mathf.Min(m_Trail, x) : x;
            m_Any = true;
        }

        /// <summary>Where the scroll wants to be, for a shared view <paramref name="halfWidth"/> wide each side.</summary>
        public float Desired(float halfWidth) =>
            Mathf.Min(m_Lead + Tuning.Camera.LookAhead, m_Trail - Tuning.Coop.TrailMargin + halfWidth);
    }
}
