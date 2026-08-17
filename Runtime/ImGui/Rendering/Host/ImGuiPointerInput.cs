using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Basic.ImGui.Rendering
{
    static class ImGuiPointerInput
    {
        public static Vector2 MousePosition
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var mouse = Mouse.current;
                if (mouse != null)
                {
                    return mouse.position.ReadValue();
                }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                return Input.mousePosition;
#else
                return Vector2.zero;
#endif
            }
        }

        public static bool IsPrimaryButtonDown
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var mouse = Mouse.current;
                if (mouse != null)
                {
                    return mouse.leftButton.isPressed;
                }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetMouseButton(0);
#else
                return false;
#endif
            }
        }

        public static Vector2 ScrollDelta
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var mouse = Mouse.current;
                if (mouse != null)
                {
                    return mouse.scroll.ReadValue();
                }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                return Input.mouseScrollDelta;
#else
                return Vector2.zero;
#endif
            }
        }
    }
}
