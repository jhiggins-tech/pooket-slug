using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace PooketSlug
{
    /// <summary>
    /// Plays a move's demo clip on the ⓘ kit screen: <c>demos/&lt;id&gt;.mp4</c> beside the page, recorded by CI from the
    /// game itself (<c>?demo=</c>, <c>tools/demos.mjs</c>). It loops with no sound, drawn through a render texture so it
    /// shows in fullscreen too. A build without its clips (or the editor) says so instead.
    /// </summary>
    public sealed class DemoClip
    {
        readonly VideoPlayer m_Player;
        readonly RenderTexture m_Texture;
        readonly RawImage m_Picture;
        readonly Text m_Note;
        string m_Url; // the clip asked for, or null
        bool m_Asked; // Play was called since the last Stop

        public DemoClip(GameObject host, RawImage picture, Text note)
        {
            m_Picture = picture;
            m_Note = note;
            m_Texture = new RenderTexture(640, 360, 0) { name = "Demo clip" };
            picture.texture = m_Texture;
            m_Player = host.AddComponent<VideoPlayer>();
            m_Player.playOnAwake = false;
            m_Player.source = VideoSource.Url;
            m_Player.renderMode = VideoRenderMode.RenderTexture;
            m_Player.targetTexture = m_Texture;
            m_Player.audioOutputMode = VideoAudioOutputMode.None;
            m_Player.isLooping = true;
            m_Player.skipOnDrop = true;
            m_Player.waitForFirstFrame = true;
            m_Player.prepareCompleted += OnPrepared;
            m_Player.errorReceived += OnError;
            Show(false, null);
        }

        /// <summary>Plays the clip at <paramref name="url"/> (null for none); asking again for the one playing does nothing.</summary>
        public void Play(string url)
        {
            if (m_Asked && url == m_Url) return;
            m_Asked = true;
            m_Url = url;
            m_Player.Stop();
            if (url == null)
            {
                Show(false, "NO DEMO IN THIS BUILD");
                return;
            }
            Show(false, "LOADING THE DEMO...");
            m_Player.url = url;
            m_Player.Prepare();
        }

        public void Stop()
        {
            if (!m_Asked) return;
            m_Asked = false;
            m_Url = null;
            m_Player.Stop();
            Show(false, null);
        }

        void OnPrepared(VideoPlayer source)
        {
            if (m_Url == null || source.url != m_Url) return; // a clip asked for before this one
            source.Play();
            Show(true, null);
        }

        void OnError(VideoPlayer source, string message)
        {
            Log.Game.Warn("Demo clip " + source.url + ": " + message);
            if (m_Url != null && source.url == m_Url) Show(false, "NO DEMO FOR THIS MOVE YET");
        }

        void Show(bool picture, string note)
        {
            m_Picture.enabled = picture;
            Ui.Show(m_Note, note != null);
            if (note != null) m_Note.text = note;
        }
    }
}
