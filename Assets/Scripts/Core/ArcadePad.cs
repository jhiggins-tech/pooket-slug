using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// The touch arcade pad (<c>docs/design/player.md</c>, Controls): a stick under the left thumb, buttons under
    /// the right. This is the logic, fingers in and a <see cref="SoldierInput"/> out, so it can be tested; the
    /// game draws it. Positions are HUD units: <see cref="Tuning.Hud.Height"/> tall, with y going down.
    /// </summary>
    public sealed class ArcadePad
    {
        public enum Button { Fire, Jump, Special, Super, Ability, Pause, Team }
        const int ButtonCount = 7;

        /// <summary>A finger on the screen this frame.</summary>
        public readonly struct Finger
        {
            public readonly int Id;
            public readonly Vector2 Pos;
            /// <summary>It touched down this frame.</summary>
            public readonly bool Began;

            public Finger(int id, Vector2 pos, bool began)
            {
                Id = id;
                Pos = pos;
                Began = began;
            }
        }

        /// <summary>What a finger does, decided when it touches down.</summary>
        enum Role { Ignore, Stick, Buttons, Pause }

        /// <summary>How far the thumb moves for a full push. Past that, the stick follows the thumb.</summary>
        public const float StickRadius = 36f;
        /// <summary>A button answers this many times its radius from its centre, so near misses still count.</summary>
        public const float Reach = 1.45f;

        readonly Vector2[] m_Center = new Vector2[ButtonCount];
        readonly float[] m_Radius = new float[ButtonCount];
        readonly bool[] m_Held = new bool[ButtonCount], m_WasHeld = new bool[ButtonCount];
        readonly Dictionary<int, Role> m_Roles = new Dictionary<int, Role>();
        readonly List<int> m_Lifted = new List<int>();
        int m_StickFinger = -1;
        Vector2 m_Origin, m_Thumb;

        public ArcadePad() => Layout(Tuning.Hud.Height * 16f / 9f);

        /// <summary>The stick and the soldier's buttons are on screen and answer touches.</summary>
        public bool Visible { get; set; }
        /// <summary>The pause button is on screen. It can show without the rest, as on the pause screen.</summary>
        public bool PauseVisible { get; set; }
        /// <summary>The soldier has an ability, so its button (AB) shows.</summary>
        public bool AbilityVisible { get; set; }
        /// <summary>A team super can be fired (<see cref="TeamSupers"/>), so its button (TEAM) shows.</summary>
        public bool TeamVisible { get; set; }
        public float Width { get; private set; }
        /// <summary>Swapped round for the left-handed: the stick on the right, the buttons on the left (pause stays top right).</summary>
        public bool LeftHanded { get; private set; }
        /// <summary>Where the stick rests when no thumb is on it.</summary>
        public Vector2 StickHome { get; private set; }
        public bool StickActive => m_StickFinger >= 0;
        public Vector2 StickOrigin => StickActive ? m_Origin : StickHome;
        public Vector2 StickKnob => StickActive ? m_Thumb : StickHome;
        /// <summary>The stick's push this frame: x right, y up, length at most 1.</summary>
        public Vector2 Stick { get; private set; }

        public Vector2 Center(Button b) => m_Center[(int)b];
        public float Radius(Button b) => m_Radius[(int)b];
        public bool Held(Button b) => m_Held[(int)b];
        public bool Shown(Button b) => b == Button.Pause ? PauseVisible : b == Button.Ability ? Visible && AbilityVisible
            : b == Button.Team ? Visible && TeamVisible : Visible;

        /// <summary>Places the stick and the buttons on a screen this many HUD units wide, mirrored if <paramref name="leftHanded"/>.</summary>
        public void Layout(float width, bool leftHanded = false)
        {
            Width = width;
            LeftHanded = leftHanded;
            float h = Tuning.Hud.Height;
            StickHome = new Vector2(X(78f), h - 78f);
            Place(Button.Fire, width - 64f, h - 72f, 32f);
            Place(Button.Jump, width - 146f, h - 46f, 26f);
            Place(Button.Special, width - 120f, h - 132f, 22f);
            Place(Button.Super, width - 46f, h - 152f, 20f);
            Place(Button.Ability, width - 198f, h - 118f, 18f);
            m_Center[(int)Button.Pause] = new Vector2(width - 22f, 60f); // top right either way, clear of both thumbs
            m_Radius[(int)Button.Pause] = 13f;
            Place(Button.Team, width - 62f, h - 208f, 22f);
        }

        /// <summary>An x measured from the left, as it is for the right-handed, or mirrored for the left-handed.</summary>
        float X(float x) => LeftHanded ? Width - x : x;

        void Place(Button b, float x, float y, float radius)
        {
            m_Center[(int)b] = new Vector2(X(x), y);
            m_Radius[(int)b] = radius;
        }

        /// <summary>
        /// Reads this frame's fingers and adds the pad's input. <paramref name="pausePressed"/>: the pause button
        /// was touched. Returns true if any finger touched down.
        /// </summary>
        public bool Read(IReadOnlyList<Finger> fingers, ref SoldierInput input, out bool pausePressed)
        {
            pausePressed = false;
            bool began = false;
            for (int i = 0; i < ButtonCount; i++)
            {
                m_WasHeld[i] = m_Held[i];
                m_Held[i] = false;
            }

            m_Lifted.Clear();
            foreach (int id in m_Roles.Keys)
                if (IndexOf(fingers, id) < 0) m_Lifted.Add(id);
            foreach (int id in m_Lifted) m_Roles.Remove(id);
            if (m_StickFinger >= 0 && !m_Roles.ContainsKey(m_StickFinger)) m_StickFinger = -1;

            for (int i = 0; i < fingers.Count; i++)
            {
                Finger f = fingers[i];
                began |= f.Began;
                // A finger gets its role when it touches down, or when it first shows up if its touch-down was
                // missed. A finger that was already down when the pad appeared keeps Ignore until it lifts.
                if (f.Began || !m_Roles.ContainsKey(f.Id)) m_Roles[f.Id] = Assign(f, ref pausePressed);
                if (!Visible) continue;
                Role role = m_Roles[f.Id];
                if (role == Role.Stick) Follow(f.Pos);
                else if (role == Role.Buttons) Press(f.Pos);
            }

            Stick = Visible && StickActive
                ? new Vector2(m_Thumb.x - m_Origin.x, m_Origin.y - m_Thumb.y) / StickRadius
                : Vector2.zero;
            input.AddStick(Stick);
            bool fire = Held(Button.Fire), jump = Held(Button.Jump);
            input.Fire |= fire;
            input.FirePressed |= fire && !m_WasHeld[(int)Button.Fire];
            input.Jump |= jump;
            input.JumpPressed |= jump && !m_WasHeld[(int)Button.Jump];
            input.Special |= Held(Button.Special);
            input.SpecialPressed |= Held(Button.Special) && !m_WasHeld[(int)Button.Special];
            input.Super |= Held(Button.Super);
            input.SuperPressed |= Held(Button.Super) && !m_WasHeld[(int)Button.Super];
            input.Ability |= Held(Button.Ability);
            input.AbilityPressed |= Held(Button.Ability) && !m_WasHeld[(int)Button.Ability];
            input.Team |= Held(Button.Team);
            input.TeamPressed |= Held(Button.Team) && !m_WasHeld[(int)Button.Team];
            return began;
        }

        Role Assign(in Finger f, ref bool pausePressed)
        {
            if (PauseVisible && Within(Button.Pause, f.Pos))
            {
                pausePressed = true;
                return Role.Pause;
            }
            if (!Visible) return Role.Ignore; // a menu's button, say: the finger does nothing here until it lifts
            if (!StickActive && (LeftHanded ? f.Pos.x > Width * 0.5f : f.Pos.x < Width * 0.5f))
            {
                m_StickFinger = f.Id;
                m_Origin = m_Thumb = f.Pos;
                return Role.Stick;
            }
            // Any other finger presses whichever button it's on or slides to. One thumb runs the stick, so a
            // second finger on the left presses nothing.
            return Role.Buttons;
        }

        /// <summary>Moves the thumb. The stick's centre is dragged along when the thumb goes past the rim.</summary>
        void Follow(Vector2 thumb)
        {
            Vector2 d = thumb - m_Origin;
            float length = d.magnitude;
            if (length > StickRadius) m_Origin += d * ((length - StickRadius) / length);
            m_Thumb = thumb;
        }

        /// <summary>Holds the button nearest the finger, if it's within reach. Fingers can slide between buttons.</summary>
        void Press(Vector2 pos)
        {
            int best = -1;
            float bestDist = float.MaxValue;
            for (int i = 0; i < ButtonCount; i++)
            {
                if (i == (int)Button.Pause || !Shown((Button)i)) continue;
                float d = Vector2.Distance(pos, m_Center[i]);
                if (d <= m_Radius[i] * Reach && d < bestDist)
                {
                    best = i;
                    bestDist = d;
                }
            }
            if (best >= 0) m_Held[best] = true;
        }

        bool Within(Button b, Vector2 pos) => Vector2.Distance(pos, m_Center[(int)b]) <= m_Radius[(int)b] * Reach;

        static int IndexOf(IReadOnlyList<Finger> fingers, int id)
        {
            for (int i = 0; i < fingers.Count; i++)
                if (fingers[i].Id == id) return i;
            return -1;
        }
    }
}
