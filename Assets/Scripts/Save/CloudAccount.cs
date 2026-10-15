using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// The player's Google account, as the page's <c>pooket-auth.js</c> reports it. The page owns the sign-in
    /// button and talks to Firebase; the game hears about it here, through <c>SendMessage</c> to this object,
    /// and sends saves back through <c>PooketAuth.jslib</c> (docs/research/accounts/README.md).
    /// Outside a WebGL player there's no page, so it stays signed out.
    /// </summary>
    public sealed class CloudAccount : MonoBehaviour
    {
        /// <summary>The page sends to the game object with this name.</summary>
        public const string ObjectName = "PooketAuth";

        [Serializable]
        sealed class State
        {
            public bool available = false, signedIn = false; // JsonUtility fills these in
            public string name = "";
        }

        /// <summary>The build has sign-in (the page has its settings).</summary>
        public bool Available { get; private set; }
        public bool SignedIn { get; private set; }
        /// <summary>The Google first name, or empty.</summary>
        public string Name { get; private set; } = "";

        /// <summary>The cloud's copy of the profile, read after signing in, or again after a save was refused.</summary>
        public event Action<Profile> CloudProfile;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void PS_AuthInit();
        [DllImport("__Internal")] static extern void PS_Save(string json);
        [DllImport("__Internal")] static extern void PS_SignOut();
        [DllImport("__Internal")] static extern void PS_ShowAccount(int show);
#else
        static void PS_AuthInit() { }
        static void PS_Save(string json) { }
        static void PS_SignOut() { }
        static void PS_ShowAccount(int show) { }
#endif

        /// <summary>Makes the receiver the page sends to.</summary>
        public static CloudAccount Create()
        {
            var go = new GameObject(ObjectName);
            DontDestroyOnLoad(go);
            return go.AddComponent<CloudAccount>();
        }

        void Start() => PS_AuthInit(); // the page answers with OnAccount, then anything it was holding

        /// <summary>Saves to the cloud record, if signed in. The page holds it until the record has been read.</summary>
        public void Save(Profile profile)
        {
            if (SignedIn) PS_Save(JsonUtility.ToJson(profile));
        }

        public void SignOut() => PS_SignOut();

        /// <summary>Shows or hides the page's sign-in button, which sits over the top right of the game.</summary>
        public void Show(bool show) => PS_ShowAccount(show ? 1 : 0);

        // ------------------------------------------------------------------ from the page (SendMessage)

        void OnAccount(string json)
        {
            try
            {
                State s = JsonUtility.FromJson<State>(json);
                Available = s.available;
                SignedIn = s.signedIn;
                Name = s.name ?? "";
                Log.Game.Info(SignedIn ? "Signed in" : Available ? "Signed out" : "Sign-in is off in this build");
            }
            catch (Exception e)
            {
                Log.Game.Warn("Couldn't read the account state: " + e.Message);
            }
        }

        void OnCloudProfile(string json)
        {
            Profile profile;
            try { profile = JsonUtility.FromJson<Profile>(json); }
            catch (Exception e)
            {
                Log.Game.Warn("Couldn't read the cloud profile: " + e.Message);
                return;
            }
            if (profile != null) CloudProfile?.Invoke(profile.Repair());
        }
    }

    /// <summary>
    /// Keeps the profile in this browser and, while the player is signed in, in their cloud record too. On
    /// signing in, the cloud's copy is merged in and the result saved to both.
    /// </summary>
    public sealed class CloudProfileStore : IProfileStore
    {
        readonly IProfileStore m_Local;
        readonly CloudAccount m_Account;

        public CloudProfileStore(IProfileStore local, CloudAccount account)
        {
            m_Local = local;
            m_Account = account;
        }

        public Profile Load() => m_Local.Load();

        public void Save(Profile profile)
        {
            m_Local.Save(profile);
            m_Account.Save(profile);
        }
    }
}
