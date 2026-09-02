using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Android2D.ReportDemo
{
    /// <summary>
    /// A tiny input abstraction that supports mouse, keyboard and multi-touch.
    /// </summary>
    public static class ReportInput
    {
        public static bool WasPressed(RectTransform target)
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame &&
                Contains(target, Mouse.current.position.ReadValue()))
            {
                return true;
            }

            if (Touchscreen.current != null)
            {
                foreach (var touch in Touchscreen.current.touches)
                {
                    if (touch.press.wasPressedThisFrame && Contains(target, touch.position.ReadValue()))
                    {
                        return true;
                    }
                }
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButtonDown(0) && Contains(target, Input.mousePosition))
            {
                return true;
            }
#endif
            return false;
        }

        public static bool IsHeld(RectTransform target)
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.isPressed &&
                Contains(target, Mouse.current.position.ReadValue()))
            {
                return true;
            }

            if (Touchscreen.current != null)
            {
                foreach (var touch in Touchscreen.current.touches)
                {
                    if (touch.press.isPressed && Contains(target, touch.position.ReadValue()))
                    {
                        return true;
                    }
                }
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButton(0) && Contains(target, Input.mousePosition))
            {
                return true;
            }
#endif
            return false;
        }

        public static float KeyboardHorizontal()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null)
            {
                return 0f;
            }

            float value = 0f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            {
                value -= 1f;
            }
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            {
                value += 1f;
            }
            return value;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetAxisRaw("Horizontal");
#else
            return 0f;
#endif
        }

        public static bool JumpPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null &&
                   (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame ||
                    Keyboard.current.upArrowKey.wasPressedThisFrame);
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
#else
            return false;
#endif
        }

        public static bool ConfirmPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null &&
                   (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame);
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Return);
#else
            return false;
#endif
        }

        public static bool BackPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Escape);
#else
            return false;
#endif
        }

        private static bool Contains(RectTransform target, Vector2 screenPoint)
        {
            return target != null && target.gameObject.activeInHierarchy &&
                   RectTransformUtility.RectangleContainsScreenPoint(target, screenPoint, null);
        }
    }
}
