using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// The kinds of message a phone sends its partner, besides its replicated state. Nothing in the game counts a
    /// Burst: kcaj's pills go as a kit move (<see cref="KitMove.Pills"/>), counted as Kit.
    /// </summary>
    public enum WireKind { Shot, EnemyShot, Burst, Hit, Claim, Grant, Hurt, Kit, Count }

    /// <summary>
    /// What goes over Photon, so liquid and particle weapons don't flood it (it began as spike coop-weapons, and is in the
    /// game now: every phone logs it). The replicated state
    /// is a fixed size whatever is on screen (a soldier's pose is one int: tones2's jet, its pressure and his spew are
    /// bits in it, <see cref="AvatarPose"/>), so only events can grow with the action. This counts them as they're sent,
    /// with an estimate of their size, and <see cref="Tick"/> sums up each stretch for the log.
    /// </summary>
    public sealed class WireMeter
    {
        /// <summary>What this phone sends (<c>NetReplication</c> counts every event).</summary>
        public static readonly WireMeter Out = new WireMeter();

        /// <summary>
        /// Bytes an RPC costs beyond its arguments: an estimate of Fusion's per-message header (the object, the
        /// behaviour, the RPC and its target). Fusion doesn't report it in a WebGL build, so the totals are estimates.
        /// </summary>
        public const int Header = 12;
        /// <summary>Seconds summed up in each log line.</summary>
        public const float Window = 30f;

        readonly int[] m_Counts = new int[(int)WireKind.Count];
        float m_Clock;

        /// <summary>The arguments of each kind, in bytes, as the RPCs declare them.</summary>
        public static int Payload(WireKind kind)
        {
            switch (kind)
            {
                case WireKind.Shot:
                case WireKind.EnemyShot: return 1 + 8 + 8; // weapon byte, origin, velocity
                case WireKind.Kit: return 1 + 8 + 8;       // the move, where, and its data
                case WireKind.Burst: return BurstEvent.Bytes;
                case WireKind.Hit: return 2 + 1;           // enemy id, damage
                case WireKind.Claim: return 2;             // pickup id
                case WireKind.Grant: return 2 + 1 + 4;     // pickup id, kind, player
                case WireKind.Hurt: return 1;
                default: return 0;
            }
        }

        public static int Bytes(WireKind kind) => Header + Payload(kind);

        public void Count(WireKind kind) => m_Counts[(int)kind]++;

        public int this[WireKind kind] => m_Counts[(int)kind];

        /// <summary>Estimated bytes counted so far this stretch.</summary>
        public int TotalBytes
        {
            get
            {
                int sum = 0;
                for (int i = 0; i < m_Counts.Length; i++) sum += m_Counts[i] * Bytes((WireKind)i);
                return sum;
            }
        }

        /// <summary>
        /// Moves the clock on. At the end of each <see cref="Window"/> it returns a line for the log (null when nothing was
        /// sent) and starts counting again.
        /// </summary>
        public string Tick(float dt)
        {
            m_Clock += dt;
            if (m_Clock < Window) return null;
            string line = Summary(m_Clock);
            m_Clock = 0f;
            System.Array.Clear(m_Counts, 0, m_Counts.Length);
            return line;
        }

        /// <summary>The stretch so far, over <paramref name="seconds"/>: each kind sent, and the estimated rate.</summary>
        public string Summary(float seconds)
        {
            var parts = new System.Text.StringBuilder();
            int events = 0;
            for (int i = 0; i < m_Counts.Length; i++)
            {
                if (m_Counts[i] == 0) continue;
                events += m_Counts[i];
                if (parts.Length > 0) parts.Append(", ");
                parts.Append(m_Counts[i]).Append(' ').Append(((WireKind)i).ToString().ToLowerInvariant());
            }
            if (events == 0) return null;
            float rate = TotalBytes / Mathf.Max(0.001f, seconds);
            return $"Events out in {seconds:0} s: {parts}, about {rate:0} bytes a second";
        }
    }

    /// <summary>
    /// One message for a whole burst of particles (kcaj's Unmedicated, 120 pills): the seed and the screen it fell
    /// across. A partner's screen starts its own <see cref="PillRain"/> from it, harmless, and gets the same pills in
    /// the same places, instead of 120 shots in one go. This is the spike's shape, kept for its tests: the game sends
    /// the same thing as a <see cref="KitEvent"/> (<see cref="KitMove.Pills"/>: the screen's left and top, its right and
    /// the seed), and <c>KcajCopy</c> starts the rain from that.
    /// </summary>
    public readonly struct BurstEvent
    {
        /// <summary>Its size on the wire: what it is, the seed, the screen's left and top, and its width.</summary>
        public const int Bytes = 1 + 4 + 4 + 4 + 4;

        public readonly byte Kind;
        public readonly uint Seed;
        public readonly float Left, Top, Width;

        public BurstEvent(byte kind, uint seed, float left, float top, float width)
        {
            Kind = kind;
            Seed = seed;
            Left = left;
            Top = top;
            Width = width;
        }

        /// <summary>Starts <paramref name="rain"/> the way this burst says; the same burst always rains the same.</summary>
        public void Start(PillRain rain) => rain.Start(new Rng(Seed), Left, Left + Width, Top);
    }
}
