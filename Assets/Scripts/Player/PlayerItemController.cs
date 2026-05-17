using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerItemController : MonoBehaviour
{
    [SerializeField] private PlayerMovement _movement;
    [SerializeField] private InventoryManager _inventory;
    [SerializeField] private InventoryUI _inventoryUI;
    [SerializeField] private ItemPreviewViewerUI _itemPreviewViewer;
    [SerializeField] private PauseMenuController _pauseMenuController;

    private WorldItemInteractable _focusedItem;
    private WorldInspectable _focusedInspectable;
    private SettingsCanvas _settingsCanvas;
    private DialogueUI _dialogueUI;
    private CoatInfoPopupUI _coatInfoPopupUI;
    private int _lastCancelHandledFrame = -1;

    private void Awake()
    {
        if (_movement == null)
            _movement = GetComponent<PlayerMovement>();

        ResolveReferences();
    }

    private void ResolveReferences()
    {
        if (_inventory == null)
            _inventory = InventoryManager.Instance;

        if (_inventoryUI == null)
            _inventoryUI = InventoryUI.Instance;

        if (_itemPreviewViewer == null)
            _itemPreviewViewer = ItemPreviewViewerUI.Instance;

        if (_pauseMenuController == null)
            _pauseMenuController = FindObjectOfType<PauseMenuController>();

        if (_settingsCanvas == null)
            _settingsCanvas = FindSettingsCanvas();

        if (_dialogueUI == null)
            _dialogueUI = FindObjectOfType<DialogueUI>();

        if (_coatInfoPopupUI == null)
            _coatInfoPopupUI = FindObjectOfType<CoatInfoPopupUI>();
    }

    private void Update()
    {
        ResolveReferences();

        if (GameplayInputUtil.CancelPressedThisFrame())
            HandleCancelPressedThisFrame();
    }

    private SettingsCanvas FindSettingsCanvas()
    {
        SettingsCanvas[] canvases = Resources.FindObjectsOfTypeAll<SettingsCanvas>();

        for (int i = 0; i < canvases.Length; i++)
        {
            SettingsCanvas canvas = canvases[i];
            if (canvas == null || canvas.gameObject == null) continue;
            if (!canvas.gameObject.scene.IsValid()) continue;

            return canvas;
        }

        return null;
    }

    private bool IsDialogueOpen()
    {
        if (_dialogueUI == null)
            _dialogueUI = FindObjectOfType<DialogueUI>();

        return _dialogueUI != null && _dialogueUI.IsOpen;
    }

    private bool IsSettingsOpen()
    {
        return (_pauseMenuController != null && _pauseMenuController.IsOpen)
            || (_settingsCanvas != null && _settingsCanvas.gameObject.activeSelf);
    }

    private bool IsAnyPopupOpen()
    {
        return (_inventoryUI != null && _inventoryUI.IsPopupOpen)
            || (_itemPreviewViewer != null && _itemPreviewViewer.IsOpen)
            || IsDialogueOpen()
            || IsSettingsOpen();
    }

    private string GetWorldHintText()
    {
        if (_focusedItem != null)
            return ProjectInteractionHints.PickupInspect;

        if (_focusedInspectable != null)
            return ProjectInteractionHints.Inspect;

        return string.Empty;
    }

    private void RefreshInteractHint()
    {
        if (_inventoryUI == null) return;

        bool shouldShow = !GlobalInteractionLock.IsLocked
            && !IsAnyPopupOpen()
            && (_focusedItem != null || _focusedInspectable != null);

        _inventoryUI.ShowInteractHint(shouldShow, GetWorldHintText());
    }

    private void ToggleSettingsPanel()
    {
        if (IsSettingsOpen())
            CloseSettingsPanel();
        else
            OpenSettingsPanel();
    }

    private void OpenSettingsPanel()
    {
        ResolveReferences();

        if (_pauseMenuController != null)
        {
            _pauseMenuController.Open();
            return;
        }

        if (_settingsCanvas == null)
            return;

        _settingsCanvas.OpenCanvas();
    }

    private void CloseSettingsPanel()
    {
        ResolveReferences();

        if (_pauseMenuController != null && _pauseMenuController.IsOpen)
        {
            _pauseMenuController.Close();
            return;
        }

        if (_settingsCanvas == null)
            return;

        _settingsCanvas.ClosedCanvas();
    }

    public void SetFocusedItem(WorldItemInteractable item)
    {
        ResolveReferences();
        _focusedItem = item;
        RefreshInteractHint();
    }

    public void ClearFocusedItem(WorldItemInteractable item)
    {
        if (_focusedItem != item) return;

        _focusedItem = null;
        RefreshInteractHint();
    }

    public void SetFocusedInspectable(WorldInspectable inspectable)
    {
        ResolveReferences();
        _focusedInspectable = inspectable;
        RefreshInteractHint();
    }

    public void ClearFocusedInspectable(WorldInspectable inspectable)
    {
        if (_focusedInspectable != inspectable) return;

        _focusedInspectable = null;
        RefreshInteractHint();
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        ResolveReferences();

        if (!context.performed) return;
        if (IsSettingsOpen()) return;
        if (GlobalInteractionLock.IsLocked) return;
        if (_itemPreviewViewer != null && _itemPreviewViewer.IsOpen) return;
        if (_inventoryUI != null && _inventoryUI.IsPopupOpen) return;

        if (_focusedItem != null)
        {
            ItemDefinition item = _focusedItem.Item;
            if (item == null || _inventoryUI == null) return;

            _movement?.SetExternalInputLocked(true);
            _inventoryUI.OpenInspectPopup(item.displayName, item.description, this, _focusedItem);
            return;
        }

        if (_focusedInspectable != null)
        {
            if (_inventoryUI == null) return;

            _movement?.SetExternalInputLocked(true);
            _inventoryUI.OpenInspectPopup(
                _focusedInspectable.Title,
                _focusedInspectable.Description,
                this
            );
        }
    }

    public void OnPickup(InputAction.CallbackContext context)
    {
        ResolveReferences();

        if (!context.performed) return;
        if (IsSettingsOpen()) return;
        if (_itemPreviewViewer != null && _itemPreviewViewer.IsOpen) return;

        if (_inventoryUI != null && _inventoryUI.IsPopupOpen)
        {
            _inventoryUI.ConfirmPendingPickup();
            return;
        }

        if (GlobalInteractionLock.IsLocked) return;
        if (_focusedItem == null) return;

        ConfirmPickup(_focusedItem);
    }

    public void OnCancel(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        HandleCancelPressedThisFrame();
    }

    private void HandleCancelPressedThisFrame()
    {
        ResolveReferences();

        if (_lastCancelHandledFrame == Time.frameCount)
            return;

        _lastCancelHandledFrame = Time.frameCount;

        if (_itemPreviewViewer != null && _itemPreviewViewer.IsOpen)
        {
            _itemPreviewViewer.Close();
            RefreshInteractHint();
            return;
        }

        if (_inventoryUI != null && _inventoryUI.IsPopupOpen)
        {
            CancelPopup();
            return;
        }

        if (_coatInfoPopupUI != null && _coatInfoPopupUI.IsOpen)
        {
            _coatInfoPopupUI.Close();
            RefreshInteractHint();
            return;
        }

        if (IsDialogueOpen())
            return;

        if (IsSettingsOpen())
        {
            CloseSettingsPanel();
            RefreshInteractHint();
            return;
        }

        if (GlobalInteractionLock.IsLocked)
            return;

        OpenSettingsPanel();
        RefreshInteractHint();
    }

    public void ConfirmPickup(WorldItemInteractable item)
    {
        ResolveReferences();

        if (item == null) return;
        if (_inventory == null) return;
        if (!_inventory.TryAdd(item.Item)) return;

        item.Pickup();
        _focusedItem = null;

        _inventoryUI?.Refresh();
        _inventoryUI?.ClosePickupPopup();

        if (_movement != null)
            _movement.SetExternalInputLocked(false);

        RefreshInteractHint();
    }

    public void CancelPopup()
    {
        ResolveReferences();

        _inventoryUI?.ClosePickupPopup();

        if (_movement != null)
            _movement.SetExternalInputLocked(false);

        RefreshInteractHint();
    }

    public void OnUseSlot1(InputAction.CallbackContext context)
    {
        ResolveReferences();

        if (!context.performed) return;
        if (GlobalInteractionLock.IsLocked) return;
        if (IsAnyPopupOpen()) return;

        _inventory?.TryUseSlot(0);
    }

    public void OnUseSlot2(InputAction.CallbackContext context)
    {
        ResolveReferences();

        if (!context.performed) return;
        if (GlobalInteractionLock.IsLocked) return;
        if (IsAnyPopupOpen()) return;

        _inventory?.TryUseSlot(1);
    }

    public void OnUseSlot3(InputAction.CallbackContext context)
    {
        ResolveReferences();

        if (!context.performed) return;
        if (GlobalInteractionLock.IsLocked) return;
        if (IsAnyPopupOpen()) return;

        _inventory?.TryUseSlot(2);
    }
}
