using System;
using System.Reflection;
using UnityEngine;

// Adapter to bridge Legacy Input and the new Input System without throwing in projects
// configured to use the new Input System only.
// Usage: replace direct calls to UnityEngine.Input with InputAdapter methods.
namespace VoxelMarchingCubes.Tools
{
    internal static class InputAdapter
    {
        public static bool GetKey(KeyCode key)
        {
            // Try new Input System via reflection so we don't require the package at compile time.
            try
            {
                var btn = ResolveButtonControlReflective(key);
                if (btn != null)
                {
                    var isPressedProp = btn.GetType().GetProperty("isPressed", BindingFlags.Public | BindingFlags.Instance);
                    if (isPressedProp != null)
                    {
                        return (bool)isPressedProp.GetValue(btn);
                    }
                }
            }
            catch
            {
                // ignore and fall back
            }

            // Fallback to legacy input
            return Input.GetKey(key);
        }

        public static bool GetKeyDown(KeyCode key)
        {
            // Try new Input System via reflection so we don't require the package at compile time.
            try
            {
                var btn = ResolveButtonControlReflective(key);
                if (btn != null)
                {
                    var wasPressedThisFrameProp = btn.GetType().GetProperty("wasPressedThisFrame", BindingFlags.Public | BindingFlags.Instance);
                    if (wasPressedThisFrameProp != null)
                    {
                        return (bool)wasPressedThisFrameProp.GetValue(btn);
                    }
                }
            }
            catch
            {
                // ignore and fall back
            }

            // Fallback to legacy input
            return Input.GetKeyDown(key);
        }

        // Reflective resolution of a ButtonControl-like object when the new Input System is present.
        // Returns null if the package/types are not available, or if the key is unmapped.
        private static object ResolveButtonControlReflective(KeyCode key)
        {
            // Get types by name without compile-time reference
            var keyboardType = Type.GetType("UnityEngine.InputSystem.Keyboard, Unity.InputSystem");
            var mouseType = Type.GetType("UnityEngine.InputSystem.Mouse, Unity.InputSystem");
            if (keyboardType == null && mouseType == null)
                return null;

            object mouse = null;
            if (mouseType != null)
            {
                var currentProp = mouseType.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
                mouse = currentProp?.GetValue(null);
                if (mouse != null)
                {
                    // Mouse buttons
                    switch (key)
                    {
                        case KeyCode.Mouse0: return mouseType.GetProperty("leftButton")?.GetValue(mouse);
                        case KeyCode.Mouse1: return mouseType.GetProperty("rightButton")?.GetValue(mouse);
                        case KeyCode.Mouse2: return mouseType.GetProperty("middleButton")?.GetValue(mouse);
                        case KeyCode.Mouse3: return mouseType.GetProperty("backButton")?.GetValue(mouse);
                        case KeyCode.Mouse4: return mouseType.GetProperty("forwardButton")?.GetValue(mouse);
                    }
                }
            }

            if (keyboardType == null)
                return null;

            var kbCurrent = keyboardType.GetProperty("current", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            if (kbCurrent == null)
                return null;

            // local helper to get a key property by name
            object Key(string name) => keyboardType.GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(kbCurrent);

            switch (key)
            {
                // Common keys and modifiers
                case KeyCode.Space: return Key("spaceKey");
                case KeyCode.Return:
                case KeyCode.KeypadEnter: return Key("enterKey");
                case KeyCode.Escape: return Key("escapeKey");
                case KeyCode.Backspace: return Key("backspaceKey");
                case KeyCode.Tab: return Key("tabKey");

                case KeyCode.LeftShift: return Key("leftShiftKey");
                case KeyCode.RightShift: return Key("rightShiftKey");
                case KeyCode.LeftControl: return Key("leftCtrlKey");
                case KeyCode.RightControl: return Key("rightCtrlKey");
                case KeyCode.LeftAlt: return Key("leftAltKey");
                case KeyCode.RightAlt: return Key("rightAltKey");

                case KeyCode.LeftArrow: return Key("leftArrowKey");
                case KeyCode.RightArrow: return Key("rightArrowKey");
                case KeyCode.UpArrow: return Key("upArrowKey");
                case KeyCode.DownArrow: return Key("downArrowKey");

                case KeyCode.CapsLock: return Key("capsLockKey");
                case KeyCode.Numlock: return Key("numLockKey");
                case KeyCode.ScrollLock: return Key("scrollLockKey");

                case KeyCode.Insert: return Key("insertKey");
                case KeyCode.Delete: return Key("deleteKey");
                case KeyCode.Home: return Key("homeKey");
                case KeyCode.End: return Key("endKey");
                case KeyCode.PageUp: return Key("pageUpKey");
                case KeyCode.PageDown: return Key("pageDownKey");
                case KeyCode.Print: return Key("printScreenKey");
                case KeyCode.Pause: return Key("pauseKey");

                // Function keys
                case KeyCode.F1: return Key("f1Key");
                case KeyCode.F2: return Key("f2Key");
                case KeyCode.F3: return Key("f3Key");
                case KeyCode.F4: return Key("f4Key");
                case KeyCode.F5: return Key("f5Key");
                case KeyCode.F6: return Key("f6Key");
                case KeyCode.F7: return Key("f7Key");
                case KeyCode.F8: return Key("f8Key");
                case KeyCode.F9: return Key("f9Key");
                case KeyCode.F10: return Key("f10Key");
                case KeyCode.F11: return Key("f11Key");
                case KeyCode.F12: return Key("f12Key");
            }

            // Letters A-Z
            switch (key)
            {
                case KeyCode.A: return Key("aKey");
                case KeyCode.B: return Key("bKey");
                case KeyCode.C: return Key("cKey");
                case KeyCode.D: return Key("dKey");
                case KeyCode.E: return Key("eKey");
                case KeyCode.F: return Key("fKey");
                case KeyCode.G: return Key("gKey");
                case KeyCode.H: return Key("hKey");
                case KeyCode.I: return Key("iKey");
                case KeyCode.J: return Key("jKey");
                case KeyCode.K: return Key("kKey");
                case KeyCode.L: return Key("lKey");
                case KeyCode.M: return Key("mKey");
                case KeyCode.N: return Key("nKey");
                case KeyCode.O: return Key("oKey");
                case KeyCode.P: return Key("pKey");
                case KeyCode.Q: return Key("qKey");
                case KeyCode.R: return Key("rKey");
                case KeyCode.S: return Key("sKey");
                case KeyCode.T: return Key("tKey");
                case KeyCode.U: return Key("uKey");
                case KeyCode.V: return Key("vKey");
                case KeyCode.W: return Key("wKey");
                case KeyCode.X: return Key("xKey");
                case KeyCode.Y: return Key("yKey");
                case KeyCode.Z: return Key("zKey");
            }

            // Top-row digits 0-9
            switch (key)
            {
                case KeyCode.Alpha0: return Key("digit0Key");
                case KeyCode.Alpha1: return Key("digit1Key");
                case KeyCode.Alpha2: return Key("digit2Key");
                case KeyCode.Alpha3: return Key("digit3Key");
                case KeyCode.Alpha4: return Key("digit4Key");
                case KeyCode.Alpha5: return Key("digit5Key");
                case KeyCode.Alpha6: return Key("digit6Key");
                case KeyCode.Alpha7: return Key("digit7Key");
                case KeyCode.Alpha8: return Key("digit8Key");
                case KeyCode.Alpha9: return Key("digit9Key");
            }

            // Numpad digits
            switch (key)
            {
                case KeyCode.Keypad0: return Key("numpad0Key");
                case KeyCode.Keypad1: return Key("numpad1Key");
                case KeyCode.Keypad2: return Key("numpad2Key");
                case KeyCode.Keypad3: return Key("numpad3Key");
                case KeyCode.Keypad4: return Key("numpad4Key");
                case KeyCode.Keypad5: return Key("numpad5Key");
                case KeyCode.Keypad6: return Key("numpad6Key");
                case KeyCode.Keypad7: return Key("numpad7Key");
                case KeyCode.Keypad8: return Key("numpad8Key");
                case KeyCode.Keypad9: return Key("numpad9Key");
            }

            return null;
        }
    }
}
