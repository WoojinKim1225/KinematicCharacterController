using UnityEngine.InputSystem;


namespace ReferenceManager // Assuming your ReferenceManager class is in this namespace
{
    public static class ReferenceManagerExtensions
    {
        public static void InitAssets(params InputActionReference[] references) {
            for (int i = 0; i < references.Length; i++) {
                references[i].asset.Disable();
            }
            references[0].asset.Enable();
            for (int i = 1; i < references.Length; i++) {
                if (references[i].asset.enabled) continue;
                references[i].asset.Enable();
            }
        }

        public static void EnableReference(InputActionReference reference, System.Action<InputAction.CallbackContext> actionHandler, int handlerAddParams = 7)
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
