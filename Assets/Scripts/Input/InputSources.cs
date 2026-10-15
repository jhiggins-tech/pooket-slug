using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace PooketSlug
{
    /// <summary>One way of playing: a keyboard, a gamepad or the touch pad.</summary>
    public interface IInputSource
    {
        /// <summary>Adds this frame's input. True if the player pressed something on this source this frame.</summary>
        bool Read(ref SoldierInput soldier, ref MenuInput menu);
    }

    /// <summary>Arrows or WASD to move, Z/Space/K to jump, X/J to fire, C/L for the special, V/U for the super, B/I for the ability.</summary>
    public sealed class KeyboardSource : IInputSource
    {
        public bool Read(ref SoldierInput soldier, ref MenuInput menu)
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb == null) return false;
            soldier.Left |= kb.leftArrowKey.isPressed || kb.aKey.isPressed;
            soldier.Right |= kb.rightArrowKey.isPressed || kb.dKey.isPressed;
            soldier.Up |= kb.upArrowKey.isPressed || kb.wKey.isPressed;
            soldier.Down |= kb.downArrowKey.isPressed || kb.sKey.isPressed;
            soldier.Jump |= kb.zKey.isPressed || kb.spaceKey.isPressed || kb.kKey.isPressed;
            soldier.JumpPressed |= kb.zKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.kKey.wasPressedThisFrame;
            soldier.Fire |= kb.xKey.isPressed || kb.jKey.isPressed;
            soldier.FirePressed |= kb.xKey.wasPressedThisFrame || kb.jKey.wasPressedThisFrame;
            soldier.Special |= kb.cKey.isPressed || kb.lKey.isPressed;
            soldier.SpecialPressed |= kb.cKey.wasPressedThisFrame || kb.lKey.wasPressedThisFrame;
            soldier.Super |= kb.vKey.isPressed || kb.uKey.isPressed;
            soldier.SuperPressed |= kb.vKey.wasPressedThisFrame || kb.uKey.wasPressedThisFrame;
            soldier.Ability |= kb.bKey.isPressed || kb.iKey.isPressed;
            soldier.AbilityPressed |= kb.bKey.wasPressedThisFrame || kb.iKey.wasPressedThisFrame;
            menu.Start |= kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame;
            menu.Restart |= kb.rKey.wasPressedThisFrame;
            menu.Pause |= kb.pKey.wasPressedThisFrame;
            menu.Mute |= kb.mKey.wasPressedThisFrame;
            menu.Host |= kb.hKey.wasPressedThisFrame;
            menu.Join |= kb.jKey.wasPressedThisFrame;
            menu.Quick |= kb.qKey.wasPressedThisFrame;
            menu.Back |= kb.escapeKey.wasPressedThisFrame;
            return kb.anyKey.wasPressedThisFrame;
#else
            soldier.Left |= Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A);
            soldier.Right |= Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D);
            soldier.Up |= Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W);
            soldier.Down |= Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);
            soldier.Jump |= Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.K);
            soldier.JumpPressed |= Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.K);
            soldier.Fire |= Input.GetKey(KeyCode.X) || Input.GetKey(KeyCode.J);
            soldier.FirePressed |= Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.J);
            soldier.Special |= Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.L);
            soldier.SpecialPressed |= Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.L);
            soldier.Super |= Input.GetKey(KeyCode.V) || Input.GetKey(KeyCode.U);
            soldier.SuperPressed |= Input.GetKeyDown(KeyCode.V) || Input.GetKeyDown(KeyCode.U);
            soldier.Ability |= Input.GetKey(KeyCode.B) || Input.GetKey(KeyCode.I);
            soldier.AbilityPressed |= Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.I);
            menu.Start |= Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
            menu.Restart |= Input.GetKeyDown(KeyCode.R);
            menu.Pause |= Input.GetKeyDown(KeyCode.P);
            menu.Mute |= Input.GetKeyDown(KeyCode.M);
            menu.Host |= Input.GetKeyDown(KeyCode.H);
            menu.Join |= Input.GetKeyDown(KeyCode.J);
            menu.Quick |= Input.GetKeyDown(KeyCode.Q);
            menu.Back |= Input.GetKeyDown(KeyCode.Escape);
            // a touch also counts as the mouse here, and isn't the keyboard
            return Input.anyKeyDown && !Input.GetMouseButtonDown(0) && !Input.GetMouseButtonDown(1) && !Input.GetMouseButtonDown(2);
#endif
        }
    }

    /// <summary>The d-pad or left stick to move, A to jump, X or the right trigger to fire, B or RB for the special, Y for the super, LB for the ability.</summary>
    public sealed class GamepadSource : IInputSource
    {
        public bool Read(ref SoldierInput soldier, ref MenuInput menu)
        {
#if ENABLE_INPUT_SYSTEM
            Gamepad pad = Gamepad.current;
            if (pad == null) return false;
            Vector2 stick = pad.leftStick.ReadValue();
            soldier.AddStick(stick);
            soldier.Left |= pad.dpad.left.isPressed;
            soldier.Right |= pad.dpad.right.isPressed;
            soldier.Up |= pad.dpad.up.isPressed;
            soldier.Down |= pad.dpad.down.isPressed;
            soldier.Jump |= pad.buttonSouth.isPressed;
            soldier.JumpPressed |= pad.buttonSouth.wasPressedThisFrame;
            soldier.Fire |= pad.buttonWest.isPressed || pad.rightTrigger.isPressed;
            soldier.FirePressed |= pad.buttonWest.wasPressedThisFrame || pad.rightTrigger.wasPressedThisFrame;
            soldier.Special |= pad.buttonEast.isPressed || pad.rightShoulder.isPressed;
            soldier.SpecialPressed |= pad.buttonEast.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame;
            soldier.Super |= pad.buttonNorth.isPressed;
            soldier.SuperPressed |= pad.buttonNorth.wasPressedThisFrame;
            soldier.Ability |= pad.leftShoulder.isPressed;
            soldier.AbilityPressed |= pad.leftShoulder.wasPressedThisFrame;
            menu.Start |= pad.startButton.wasPressedThisFrame;
            menu.Restart |= pad.selectButton.wasPressedThisFrame;
            return pad.buttonSouth.wasPressedThisFrame || pad.buttonWest.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame
                || pad.buttonNorth.wasPressedThisFrame || pad.rightTrigger.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame
                || pad.leftShoulder.wasPressedThisFrame
                || pad.startButton.wasPressedThisFrame || pad.selectButton.wasPressedThisFrame || pad.dpad.ReadValue() != Vector2.zero
                || stick.sqrMagnitude > SoldierInput.AimThreshold * SoldierInput.AimThreshold;
#else
            return false; // gamepads need the Input System package
#endif
        }
    }

    /// <summary>Touch screens: the arcade pad while playing. The menus' buttons take taps themselves (uGUI).</summary>
    public sealed class TouchSource : IInputSource
    {
        readonly ArcadePad m_Pad;
        readonly List<ArcadePad.Finger> m_Fingers = new List<ArcadePad.Finger>(10);

        public TouchSource(ArcadePad pad) => m_Pad = pad;

        public bool Read(ref SoldierInput soldier, ref MenuInput menu)
        {
            m_Fingers.Clear();
            float scale = Screen.height / Tuning.Hud.Height; // screen pixels per HUD unit; HUD y goes down
#if ENABLE_INPUT_SYSTEM
            Touchscreen screen = Touchscreen.current;
            if (screen != null)
            {
                for (int i = 0; i < screen.touches.Count; i++)
                {
                    TouchControl t = screen.touches[i];
                    bool down = t.press.wasPressedThisFrame;
                    if (!t.press.isPressed && !down) continue; // a tap can begin and end within one frame
                    Vector2 p = t.position.ReadValue();
                    m_Fingers.Add(new ArcadePad.Finger(t.touchId.ReadValue(), new Vector2(p.x, Screen.height - p.y) / scale, down));
                }
            }
#else
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) continue;
                m_Fingers.Add(new ArcadePad.Finger(t.fingerId, new Vector2(t.position.x, Screen.height - t.position.y) / scale, t.phase == TouchPhase.Began));
            }
#endif
            bool began = m_Pad.Read(m_Fingers, ref soldier, out bool pause);
            menu.PadPause |= pause;
            return began;
        }
    }
}
