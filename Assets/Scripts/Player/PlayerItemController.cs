using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerItemController : MonoBehaviour
{
    [SerializeField] private PlayerMovement _movement;
    [SerializeField] private InventoryManager _inventory;
    [SerializeField] private InventoryUI _inventoryUI;
    [SerializeField] private ItemPreviewViewerUI _itemPreviewViewer;

    private WorldItemInteractable _focusedItem;
    private WorldInspectable _focusedInspectable;
    private bool _subscribedHouseViewerClosed;

    private void Awake()
    {
        if (_movement == null)
            _movement = GetComponent<PlayerMovement>();

        ResolveReferences();
    }

    private void OnDestroy()
    {
        UnbindHouseViewer();
    }

    private void ResolveReferences()
    {
        if (_inventory == null)
            _inventory = InventoryManager.Instance;

        if (_inventoryUI == null)
            _inventoryUI = InventoryUI.Instance;

        if (_itemPreviewViewer == null)
            _itemPreviewViewer = ItemPreviewViewerUI.Instance;
    }

    private void UnbindHouseViewer()
    {
        if (!_subscribedHouseViewerClosed) return;

        _subscribedHouseViewerClosed = false;
    }

    private bool IsAnyPopupOpen()
    {
        return (_inventoryUI != null && _inventoryUI.IsPopupOpen)
            || (_itemPreviewViewer != null && _itemPreviewViewer.IsOpen);
    }

    private void RefreshInteractHint()
    {
        if (_inventoryUI == null) return;

        bool shouldShow = !GlobalInteractionLock.IsLocked
            && !IsAnyPopupOpen()
            && (_focusedItem != null || _focusedInspectable != null);

        _inventoryUI.ShowInteractHint(shouldShow);
    }

    private void HandleHouseViewerClosed()
    {
        if (_movement != null)
            _movement.SetExternalInputLocked(false);

        RefreshInteractHint();
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
        if (GlobalInteractionLock.IsLocked) return;

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

        if (_focusedItem != null)
        {
            if (_inventoryUI == null) return;

            _movement?.SetExternalInputLocked(true);
            _inventoryUI.OpenPickupPopup(this, _focusedItem);
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
        if (_itemPreviewViewer != null && _itemPreviewViewer.IsOpen) return;
        if (_inventoryUI != null && _inventoryUI.IsPopupOpen) return;

        _inventory?.TryUseSlot(0);
    }

    public void OnUseSlot2(InputAction.CallbackContext context)
    {
        ResolveReferences();

        if (!context.performed) return;
        if (GlobalInteractionLock.IsLocked) return;
        if (_itemPreviewViewer != null && _itemPreviewViewer.IsOpen) return;
        if (_inventoryUI != null && _inventoryUI.IsPopupOpen) return;

        _inventory?.TryUseSlot(1);
    }

    public void OnUseSlot3(InputAction.CallbackContext context)
    {
        ResolveReferences();

        if (!context.performed) return;
        if (GlobalInteractionLock.IsLocked) return;
        if (_itemPreviewViewer != null && _itemPreviewViewer.IsOpen) return;
        if (_inventoryUI != null && _inventoryUI.IsPopupOpen) return;

        _inventory?.TryUseSlot(2);
    }
}
