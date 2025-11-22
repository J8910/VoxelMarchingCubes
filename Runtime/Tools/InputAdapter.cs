using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

// Adapter to bridge Legacy Input and the new Input System without throwing in projects
// configured to use the new Input System only.
// Usage: replace direct calls to UnityEngine.Input with InputAdapter methods.
namespace VoxelMarchingCubes.Tools
{
    internal static class InputAdapter
    {
        public static bool GetKey(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            return GetKey_InputSystem(key);
#else
            return Input.GetKey(key);
#endif
        }

        public static bool GetKeyDown(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            return GetKeyDown_InputSystem(key);
#else
            return Input.GetKeyDown(key);
#endif
        }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        private static bool GetKey_InputSystem(KeyCode key)
        {
            var btn = ResolveButtonControl(key);
            return btn != null && btn.isPressed;
        }

        private static bool GetKeyDown_InputSystem(KeyCode key)
        {
            var btn = ResolveButtonControl(key);
            return btn != null && btn.wasPressedThisFrame;
        }

        private static ButtonControl ResolveButtonControl(KeyCode key)
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;

            // Mouse buttons
            if (mouse != null)
            {
                switch (key)
                {
                    case KeyCode.Mouse0: return mouse.leftButton;
                    case KeyCode.Mouse1: return mouse.rightButton;
                    case KeyCode.Mouse2: return mouse.middleButton;
                    case KeyCode.Mouse3: return mouse.backButton;
                    case KeyCode.Mouse4: return mouse.forwardButton;
                }
            }

            if (kb == null) return null;

            // Common keys and modifiers
            switch (key)
            {
                case KeyCode.Space: return kb.spaceKey;
                case KeyCode.Return:
                case KeyCode.KeypadEnter: return kb.enterKey;
                case KeyCode.Escape: return kb.escapeKey;
                case KeyCode.Backspace: return kb.backspaceKey;
                case KeyCode.Tab: return kb.tabKey;

                case KeyCode.LeftShift: return kb.leftShiftKey;
                case KeyCode.RightShift: return kb.rightShiftKey;
                case KeyCode.LeftControl: return kb.leftCtrlKey;
                case KeyCode.RightControl: return kb.rightCtrlKey;
                case KeyCode.LeftAlt: return kb.leftAltKey;
                case KeyCode.RightAlt: return kb.rightAltKey;

                case KeyCode.LeftArrow: return kb.leftArrowKey;
                case KeyCode.RightArrow: return kb.rightArrowKey;
                case KeyCode.UpArrow: return kb.upArrowKey;
                case KeyCode.DownArrow: return kb.downArrowKey;

                case KeyCode.CapsLock: return kb.capsLockKey;
                case KeyCode.Numlock: return kb.numLockKey;
                case KeyCode.ScrollLock: return kb.scrollLockKey;

                case KeyCode.Insert: return kb.insertKey;
                case KeyCode.Delete: return kb.deleteKey;
                case KeyCode.Home: return kb.homeKey;
                case KeyCode.End: return kb.endKey;
                case KeyCode.PageUp: return kb.pageUpKey;
                case KeyCode.PageDown: return kb.pageDownKey;
                case KeyCode.Print: return kb.printScreenKey;
                case KeyCode.Pause: return kb.pauseKey;

                // Function keys
                case KeyCode.F1: return kb.f1Key;
                case KeyCode.F2: return kb.f2Key;
                case KeyCode.F3: return kb.f3Key;
                case KeyCode.F4: return kb.f4Key;
                case KeyCode.F5: return kb.f5Key;
                case KeyCode.F6: return kb.f6Key;
                case KeyCode.F7: return kb.f7Key;
                case KeyCode.F8: return kb.f8Key;
                case KeyCode.F9: return kb.f9Key;
                case KeyCode.F10: return kb.f10Key;
                case KeyCode.F11: return kb.f11Key;
                case KeyCode.F12: return kb.f12Key;
            }

            // Letters A-Z
            switch (key)
            {
                case KeyCode.A: return kb.aKey;
                case KeyCode.B: return kb.bKey;
                case KeyCode.C: return kb.cKey;
                case KeyCode.D: return kb.dKey;
                case KeyCode.E: return kb.eKey;
                case KeyCode.F: return kb.fKey;
                case KeyCode.G: return kb.gKey;
                case KeyCode.H: return kb.hKey;
                case KeyCode.I: return kb.iKey;
                case KeyCode.J: return kb.jKey;
                case KeyCode.K: return kb.kKey;
                case KeyCode.L: return kb.lKey;
                case KeyCode.M: return kb.mKey;
                case KeyCode.N: return kb.nKey;
                case KeyCode.O: return kb.oKey;
                case KeyCode.P: return kb.pKey;
                case KeyCode.Q: return kb.qKey;
                case KeyCode.R: return kb.rKey;
                case KeyCode.S: return kb.sKey;
                case KeyCode.T: return kb.tKey;
                case KeyCode.U: return kb.uKey;
                case KeyCode.V: return kb.vKey;
                case KeyCode.W: return kb.wKey;
                case KeyCode.X: return kb.xKey;
                case KeyCode.Y: return kb.yKey;
                case KeyCode.Z: return kb.zKey;
            }

            // Top-row digits 0-9
            switch (key)
            {
                case KeyCode.Alpha0: return kb.digit0Key;
                case KeyCode.Alpha1: return kb.digit1Key;
                case KeyCode.Alpha2: return kb.digit2Key;
                case KeyCode.Alpha3: return kb.digit3Key;
                case KeyCode.Alpha4: return kb.digit4Key;
                case KeyCode.Alpha5: return kb.digit5Key;
                case KeyCode.Alpha6: return kb.digit6Key;
                case KeyCode.Alpha7: return kb.digit7Key;
                case KeyCode.Alpha8: return kb.digit8Key;
                case KeyCode.Alpha9: return kb.digit9Key;
            }

            // Numpad digits
            switch (key)
            {
                case KeyCode.Keypad0: return kb.numpad0Key;
                case KeyCode.Keypad1: return kb.numpad1Key;
                case KeyCode.Keypad2: return kb.numpad2Key;
                case KeyCode.Keypad3: return kb.numpad3Key;
                case KeyCode.Keypad4: return kb.numpad4Key;
                case KeyCode.Keypad5: return kb.numpad5Key;
                case KeyCode.Keypad6: return kb.numpad6Key;
                case KeyCode.Keypad7: return kb.numpad7Key;
                case KeyCode.Keypad8: return kb.numpad8Key;
                case KeyCode.Keypad9: return kb.numpad9Key;
            }

            // If unmapped, return null (callers treat as not pressed)
            return null;
        }
#endif
    }
}
