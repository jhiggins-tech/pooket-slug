using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PooketSlug
{
    /// <summary>
    /// Polled input snapshot. Works with either Active Input Handling setting: the Input System package
    /// (keyboard + gamepad) when it is enabled, otherwise the legacy Input Manager (keyboard only).
    /// </summary>
    public static class GameInput
    {
        public static float MoveX;
        public static bool Up, Down;
        public static bool JumpHeld, JumpPressed, FireHeld, FirePressed, GrenadePressed;
        public static bool StartPressed, RestartPressed, PausePressed, MutePressed;

        public static void Poll()
        {
            bool left = false, right = false, up = false, down = false;
            bool jump = false, jumpDown = false, fire = false, fireDown = false, grenadeDown = false;
            bool start = false, restart = false, pause = false, mute = false;

#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                left = kb.leftArrowKey.isPressed || kb.aKey.isPressed;
                right = kb.rightArrowKey.isPressed || kb.dKey.isPressed;
                up = kb.upArrowKey.isPressed || kb.wKey.isPressed;
                down = kb.downArrowKey.isPressed || kb.sKey.isPressed;
                jump = kb.zKey.isPressed || kb.spaceKey.isPressed || kb.kKey.isPressed;
                jumpDown = kb.zKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.kKey.wasPressedThisFrame;
                fire = kb.xKey.isPressed || kb.jKey.isPressed;
                fireDown = kb.xKey.wasPressedThisFrame || kb.jKey.wasPressedThisFrame;
                grenadeDown = kb.cKey.wasPressedThisFrame || kb.lKey.wasPressedThisFrame;
                start = kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame;
                restart = kb.rKey.wasPressedThisFrame;
                pause = kb.pKey.wasPressedThisFrame;
                mute = kb.mKey.wasPressedThisFrame;
            }

            Gamepad pad = Gamepad.current;
            if (pad != null)
            {
                Vector2 stick = pad.leftStick.ReadValue();
                left |= pad.dpad.left.isPressed || stick.x < -0.4f;
                right |= pad.dpad.right.isPressed || stick.x > 0.4f;
                up |= pad.dpad.up.isPressed || stick.y > 0.5f;
                down |= pad.dpad.down.isPressed || stick.y < -0.5f;
                jump |= pad.buttonSouth.isPressed;
                jumpDown |= pad.buttonSouth.wasPressedThisFrame;
                fire |= pad.buttonWest.isPressed || pad.rightTrigger.isPressed;
                fireDown |= pad.buttonWest.wasPressedThisFrame || pad.rightTrigger.wasPressedThisFrame;
                grenadeDown |= pad.buttonEast.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame;
                start |= pad.startButton.wasPressedThisFrame;
                restart |= pad.selectButton.wasPressedThisFrame;
            }
#else
            left = Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A);
            right = Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D);
            up = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W);
            down = Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);
            jump = Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.K);
            jumpDown = Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.K);
            fire = Input.GetKey(KeyCode.X) || Input.GetKey(KeyCode.J);
            fireDown = Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.J);
            grenadeDown = Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.L);
            start = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
            restart = Input.GetKeyDown(KeyCode.R);
            pause = Input.GetKeyDown(KeyCode.P);
            mute = Input.GetKeyDown(KeyCode.M);
#endif

            MoveX = (right ? 1f : 0f) - (left ? 1f : 0f);
            Up = up;
            Down = down;
            JumpHeld = jump;
            JumpPressed = jumpDown;
            FireHeld = fire;
            FirePressed = fireDown;
            GrenadePressed = grenadeDown;
            StartPressed = start;
            RestartPressed = restart;
            PausePressed = pause;
            MutePressed = mute;
        }
    }
}
