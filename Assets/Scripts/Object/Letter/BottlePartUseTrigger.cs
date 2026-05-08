using UnityEngine;
using UnityEngine.SceneManagement;

public class BottlePartUseTrigger : MonoBehaviour
{
    [Header("Scene Limit")]
    [SerializeField] private string _currentScene = "House";

    [Header("Required Item")]
    [SerializeField] private ItemDefinition _requiredPartItem;

    [Header("One-time State")]
    [SerializeField] private string _interactionCompleteFlag = "house.bottle_part_used";

    [Header("Bottle Swap")]
    [SerializeField] private GameObject _bottle1;
    [SerializeField] private GameObject _bottle2;

    [Header("Key")]
    [SerializeField] private GameObject _keyRoot;
    [SerializeField] private GameObject _keyInspectTrigger;
    [SerializeField] private MonoBehaviour _keyPickupInteractable;
    [SerializeField] private Collider2D _keyPickupCollider;

    [Header("Key Exposed Position")]
    [SerializeField] private Transform _keyExposedAnchor;
    [SerializeField] private bool _moveKeyToExposedAnchorAfterUse = true;
    [SerializeField] private bool _restoreKeyToOriginalPoseWhenNotCompleted = true;

    [Header("Optional")]
    [SerializeField] private bool _forceBottle1VisibleBeforeUse = true;
    [SerializeField] private bool _forceBottle2HiddenBeforeUse = true;
    [SerializeField] private bool _disableKeyInspectBeforeUse = true;
    [SerializeField] private bool _keepKeyVisibleAfterUse = true;

    private bool _playerInRange;
    private bool _slotHintShownByThisScript;
    private bool _hasCompleted;

    private bool _cachedKeyOriginalPose;
    private Vector3 _keyOriginalLocalPosition;
    private Quaternion _keyOriginalLocalRotation;
    private Vector3 _keyOriginalLocalScale;
    private Transform _keyOriginalParent;

    private void Awake()
    {
        ResolveKeyRoot();
        CacheKeyOriginalPose();
    }

    private void OnEnable()
    {
        ResolveKeyRoot();
        CacheKeyOriginalPose();

        _hasCompleted = GameManager.Instance != null && GameManager.Instance.GetFlag(_interactionCompleteFlag);
        HideSlotHint();
        RefreshSceneState();
    }

    private void OnDisable()
    {
        HideSlotHint();
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name != _currentScene)
        {
            HideSlotHint();
            return;
        }

        if (GameManager.Instance == null) return;
        if (GameManager.Instance.IsLoadingScene()) return;

        if (!_hasCompleted)
            _hasCompleted = GameManager.Instance.GetFlag(_interactionCompleteFlag);

        RefreshSceneState();

        if (_hasCompleted)
        {
            HideSlotHint();
            return;
        }

        if (!_playerInRange)
        {
            HideSlotHint();
            return;
        }

        if (_requiredPartItem == null || InventoryManager.Instance == null)
        {
            HideSlotHint();
            return;
        }

        bool hasPart = InventoryManager.Instance.HasItem(_requiredPartItem.itemId);

        if (hasPart)
            ShowSlotHint();
        else
            HideSlotHint();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        _playerInRange = false;
        HideSlotHint();
    }

    public bool CanHandleItem(ItemDefinition item)
    {
        if (item == null) return false;
        if (_requiredPartItem == null) return false;

        return item.itemId == _requiredPartItem.itemId;
    }

    public bool TryUseItem(ItemDefinition item)
    {
        if (!CanHandleItem(item)) return false;
        if (SceneManager.GetActiveScene().name != _currentScene) return false;
        if (GameManager.Instance == null || GameManager.Instance.IsLoadingScene()) return false;
        if (_hasCompleted || GameManager.Instance.GetFlag(_interactionCompleteFlag)) return false;
        if (!_playerInRange) return false;

        ExecuteInteraction();
        return true;
    }

    private void ExecuteInteraction()
    {
        _hasCompleted = true;

        if (GameManager.Instance != null && !string.IsNullOrWhiteSpace(_interactionCompleteFlag))
            GameManager.Instance.SetFlag(_interactionCompleteFlag, true);

        HideSlotHint();
        RefreshSceneState();
    }

    private void RefreshSceneState()
    {
        ResolveKeyRoot();

        if (_hasCompleted)
        {
            if (_bottle1 != null)
                _bottle1.SetActive(false);

            if (_bottle2 != null)
                _bottle2.SetActive(true);

            if (_keyInspectTrigger != null)
                _keyInspectTrigger.SetActive(false);

            if (_keyPickupInteractable != null)
                _keyPickupInteractable.enabled = true;

            if (_keyPickupCollider != null)
                _keyPickupCollider.enabled = true;

            if (_keepKeyVisibleAfterUse && !IsKeyAlreadyCollected())
            {
                if (_keyRoot != null)
                    _keyRoot.SetActive(true);

                if (_moveKeyToExposedAnchorAfterUse)
                    MoveKeyToExposedAnchor();
            }
        }
        else
        {
            if (_bottle1 != null && _forceBottle1VisibleBeforeUse)
                _bottle1.SetActive(true);

            if (_bottle2 != null && _forceBottle2HiddenBeforeUse)
                _bottle2.SetActive(false);

            if (_keyInspectTrigger != null && _disableKeyInspectBeforeUse)
                _keyInspectTrigger.SetActive(false);

            if (_keyPickupInteractable != null)
                _keyPickupInteractable.enabled = false;

            if (_keyPickupCollider != null)
                _keyPickupCollider.enabled = false;

            if (_restoreKeyToOriginalPoseWhenNotCompleted)
                RestoreKeyOriginalPose();
        }
    }

    private void ResolveKeyRoot()
    {
        if (_keyRoot != null)
            return;

        if (_keyPickupInteractable != null)
        {
            _keyRoot = _keyPickupInteractable.gameObject;
            return;
        }

        if (_keyPickupCollider != null)
        {
            _keyRoot = _keyPickupCollider.gameObject;
            return;
        }

        if (_keyInspectTrigger != null)
        {
            Transform parent = _keyInspectTrigger.transform.parent;
            if (parent != null)
                _keyRoot = parent.gameObject;
        }
    }

    private void CacheKeyOriginalPose()
    {
        if (_cachedKeyOriginalPose) return;
        if (_keyRoot == null) return;

        Transform t = _keyRoot.transform;
        _keyOriginalParent = t.parent;
        _keyOriginalLocalPosition = t.localPosition;
        _keyOriginalLocalRotation = t.localRotation;
        _keyOriginalLocalScale = t.localScale;
        _cachedKeyOriginalPose = true;
    }

    private void RestoreKeyOriginalPose()
    {
        if (!_cachedKeyOriginalPose) return;
        if (_keyRoot == null) return;

        Transform t = _keyRoot.transform;

        if (t.parent != _keyOriginalParent)
            t.SetParent(_keyOriginalParent, false);

        t.localPosition = _keyOriginalLocalPosition;
        t.localRotation = _keyOriginalLocalRotation;
        t.localScale = _keyOriginalLocalScale;
    }

    private void MoveKeyToExposedAnchor()
    {
        if (_keyRoot == null) return;
        if (_keyExposedAnchor == null) return;

        Transform t = _keyRoot.transform;
        t.position = _keyExposedAnchor.position;
    }

    private bool IsKeyAlreadyCollected()
    {
        WorldItemInteractable itemInteractable = _keyPickupInteractable as WorldItemInteractable;
        if (itemInteractable == null) return false;
        if (itemInteractable.Item == null) return false;
        if (GameManager.Instance == null) return false;

        return GameManager.Instance.GetFlag(itemInteractable.Item.CollectedFlag);
    }

    private int GetCurrentRequiredItemSlot()
    {
        if (_requiredPartItem == null) return -1;
        if (InventoryManager.Instance == null) return -1;

        return InventoryManager.Instance.FindSlotIndexByItemId(_requiredPartItem.itemId);
    }

    private void ShowSlotHint()
    {
        int slot = GetCurrentRequiredItemSlot();
        if (slot < 0) return;

        if (_slotHintShownByThisScript) return;
        if (InventoryUI.Instance == null) return;

        InventoryUI.Instance.PlaySlotHint(slot);
        _slotHintShownByThisScript = true;
    }

    private void HideSlotHint()
    {
        if (!_slotHintShownByThisScript) return;
        if (InventoryUI.Instance == null) return;

        InventoryUI.Instance.StopSlotHint();
        _slotHintShownByThisScript = false;
    }
}
