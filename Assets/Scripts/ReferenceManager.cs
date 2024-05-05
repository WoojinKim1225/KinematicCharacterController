using UnityEngine.InputSystem;


namespace ReferenceManager // Assuming your ReferenceManager class is in this namespace
{
    public static class ReferenceManagerExtensions
    {
        public static void EnableReference(InputActionReference reference, System.Action<InputAction.CallbackContext> actionHandler, uint handlerAddParams = 7)
        {
            if (!reference.asset.enabled) reference.asset.Enable();
            if (((handlerAddParams >> 0) & 1) == 1) reference.action.started += actionHandler;
            if (((handlerAddParams >> 1) & 1) == 1) reference.action.canceled += actionHandler;
            if (((handlerAddParams >> 2) & 1) == 1) reference.action.performed += actionHandler;
        }

        public static void DisableReference(InputActionReference reference, System.Action<InputAction.CallbackContext> actionHandler)
        {
            if (reference.asset.enabled) reference.asset.Disable();
            reference.action.started -= actionHandler;
            reference.action.canceled -= actionHandler;
            reference.action.performed -= actionHandler;
        }
    }
}
