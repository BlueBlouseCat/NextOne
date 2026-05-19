using UnityEngine;
using UnityEngine.InputSystem;

public static class GameplayInputUtil
{
    private static PlayerInput _cachedPlayerInput;
    private static InputActionAsset _cachedActionsAsset;
    private static int _lastResolveFrame = -1;
    private static int _cancelConsumedFrame = -1;

    public static bool InteractPressedThisFrame()
    {
        InputAction action = FindAction("Interact", "Player/Interact");
        if (action != null) return action.WasPressedThisFrame();

        return Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;
    }

    public static bool InteractHeld()
    {
        InputAction action = FindAction("Interact", "Player/Interact");
        if (action != null) return action.IsPressed();

        return Keyboard.current != null && Keyboard.current.fKey.isPressed;
    }

    public static bool PickupPressedThisFrame()
    {
        InputAction action = FindAction("Pickup", "Player/Pickup");
        if (action != null) return action.WasPressedThisFrame();

        return Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame;
    }

    public static bool CancelPressedThisFrame()
    {
        InputAction action = FindAction("Player/Cancel", "UI/Cancel", "Cancel");
        if (action != null) return action.WasPressedThisFrame();

        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
    }

    public static bool ConsumeCancelThisFrame()
    {
        if (_cancelConsumedFrame == Time.frameCount)
            return false;

        _cancelConsumedFrame = Time.frameCount;
        return true;
    }

    public static bool IsCancelConsumedThisFrame()
    {
        return _cancelConsumedFrame == Time.frameCount;
    }

    public static bool SubmitPressedThisFrame()
    {
        InputAction action = FindAction("Submit", "UI/Submit");
        if (action != null) return action.WasPressedThisFrame();

        return Keyboard.current != null &&
               (Keyboard.current.enterKey.wasPressedThisFrame ||
                Keyboard.current.numpadEnterKey.wasPressedThisFrame);
    }

    public static bool SlotPressedThisFrame(int slotIndex)
    {
        string actionName;

        switch (slotIndex)
        {
            case 0:
                actionName = "UseSlot1";
                break;
            case 1:
                actionName = "UseSlot2";
                break;
            case 2:
                actionName = "UseSlot3";
                break;
            default:
                return false;
        }

        InputAction action = FindAction(actionName, $"Player/{actionName}");
        if (action != null) return action.WasPressedThisFrame();

        if (Keyboard.current == null) return false;

        switch (slotIndex)
        {
            case 0:
                return Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame;
            case 1:
                return Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame;
            case 2:
                return Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame;
            default:
                return false;
        }
    }

    public static string GetInteractDisplayName()
    {
        return GetActionDisplayName("F", "Interact", "Player/Interact");
    }

    public static string GetPickupDisplayName()
    {
        return GetActionDisplayName("G", "Pickup", "Player/Pickup");
    }

    public static string GetCancelDisplayName()
    {
        return GetActionDisplayName("ESC", "Cancel", "UI/Cancel", "Player/Cancel");
    }

    public static string GetSubmitDisplayName()
    {
        return GetActionDisplayName("Enter", "Submit", "UI/Submit");
    }

    private static string GetActionDisplayName(string fallback, params string[] names)
    {
        InputAction action = FindAction(names);
        if (action == null)
            return fallback;

        string display = action.GetBindingDisplayString();
        if (!string.IsNullOrWhiteSpace(display))
            return display;

        for (int i = 0; i < action.bindings.Count; i++)
        {
            if (action.bindings[i].isComposite || action.bindings[i].isPartOfComposite)
                continue;

            display = action.GetBindingDisplayString(i);
            if (!string.IsNullOrWhiteSpace(display))
                return display;
        }

        return fallback;
    }

    private static InputAction FindAction(params string[] names)
    {
        InputActionAsset actions = ResolveActionsAsset();
        if (actions == null) return null;

        for (int i = 0; i < names.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(names[i])) continue;

            InputAction action = actions.FindAction(names[i], false);
            if (action != null) return action;
        }

        return null;
    }

    private static InputActionAsset ResolveActionsAsset()
    {
        if (_lastResolveFrame == Time.frameCount)
        {
            if (_cachedPlayerInput != null && _cachedPlayerInput.actions != null)
                return _cachedPlayerInput.actions;

            if (_cachedActionsAsset != null)
                return _cachedActionsAsset;
        }

        _lastResolveFrame = Time.frameCount;

        if (_cachedPlayerInput != null &&
            _cachedPlayerInput.gameObject != null &&
            _cachedPlayerInput.isActiveAndEnabled &&
            _cachedPlayerInput.actions != null)
        {
            return _cachedPlayerInput.actions;
        }

        Transform currentPlayer = GameManager.Instance != null ? GameManager.Instance.CurrentPlayer : null;
        if (currentPlayer != null)
        {
            _cachedPlayerInput = currentPlayer.GetComponent<PlayerInput>();
            if (_cachedPlayerInput == null)
                _cachedPlayerInput = currentPlayer.GetComponentInParent<PlayerInput>();
        }

        if (_cachedPlayerInput == null)
            _cachedPlayerInput = Object.FindObjectOfType<PlayerInput>();

        if (_cachedPlayerInput != null && _cachedPlayerInput.actions != null)
            return _cachedPlayerInput.actions;

        InputActionAsset[] loadedAssets = Resources.FindObjectsOfTypeAll<InputActionAsset>();
        for (int i = 0; i < loadedAssets.Length; i++)
        {
            InputActionAsset actions = loadedAssets[i];
            if (actions == null) continue;

            if (actions.FindAction("Player/Interact", false) != null ||
                actions.FindAction("Interact", false) != null)
            {
                _cachedActionsAsset = actions;
                return _cachedActionsAsset;
            }
        }

        return _cachedActionsAsset;
    }
}
