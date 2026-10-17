using System.Runtime.InteropServices;

namespace PooketSlug
{
    /// <summary>How sharing an invite went (<see cref="Invite.Result"/>).</summary>
    public enum InviteResult { None, Shared, Copied, Failed }

    /// <summary>
    /// Rooms: the den's invite button shares the room's link on a phone (the share sheet) or copies it elsewhere,
    /// through <c>PooketInvite.jslib</c>. Browsers only share or copy inside a tap's own event, so it's armed as the
    /// finger goes down and the page does it as the finger comes up. Outside a WebGL player there's no page: it fails,
    /// and the den shows the link instead.
    /// </summary>
    public static class Invite
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void PS_InviteArm(string url, string text);
        [DllImport("__Internal")] static extern int PS_InviteResult();
#else
        static int s_Result;
        static void PS_InviteArm(string url, string text) => s_Result = (int)InviteResult.Failed;
        static int PS_InviteResult()
        {
            int r = s_Result;
            s_Result = 0;
            return r;
        }
#endif

        /// <summary>Shares or copies <paramref name="link"/> when the tap that armed it lifts.</summary>
        public static void Arm(string link, string text) => PS_InviteArm(link, text);

        /// <summary>How the last invite went, once; then <see cref="InviteResult.None"/> until the next.</summary>
        public static InviteResult Result() => (InviteResult)PS_InviteResult();
    }
}
