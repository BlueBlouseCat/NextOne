using UnityEngine;
using UnityEngine.SceneManagement;

public class LockUnlockLetterTrigger : MonoBehaviour
{
    [Header("Scene Limit")]
    [SerializeField] private string _currentScene = "House";

    [Header("Required Item")]
    [SerializeField] private ItemDefinition _requiredKeyItem;
    [SerializeField] private bool _consumeKeyOnUse = false;

    [Header("One-time State")]
    [SerializeField] private string _unlockFlag = "house.lock_unlocked";

    [Header("Scene Objects")]
    [SerializeField] private GameObject _lockRoot;
    [SerializeField] private LetterSwapInspectTrigger _letterInteractTarget;

    [Header("Optional")]
    [SerializeField] private bool _forceLockVisibleBeforeUnlock = true;
    [SerializeField] private bool _logMissingReferences = true;

    private bool _playerInRange;
    private bool _slotHintShownByThisScript;
    private bool _hasUnlocked;
    private bool _loggedMissingItemWarning;

    private void OnEnable()
    {
        _hasUnlocked = GameManager.Instance != null && GameManager.Instance.GetFlag(_unlockFlag);
        _loggedMissingItemWarning = false;

        HideSlotHint();
        RefreshSceneState();
    }

    private void OnDisable()
    {
        _playerInRange = false;
        HideSlotHint();
    }

    private void Update()
    {
        if (!IsInCurrentScene())
        {
            HideSlotHint();
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.IsLoadingScene())
        {
            HideSlotHint();
            return;
        }

        if (!_hasUnlocked && GameManager.Instance != null)
            _hasUnlocked = GameManager.Instance.GetFlag(_unlockFlag);

        RefreshSceneState();

        if (_hasUnlocked)
        {
            HideSlotHint();
            return;
        }

        if (!_playerInRange)
        {
            HideSlotHint();
            return;
        }

        if (_requiredKeyItem == null)
        {
            HideSlotHint();
            LogMissingItemWarningOnce();
            return;
        }

        if (InventoryManager.Instance == null)
        {
            HideSlotHint();
            return;
        }

        bool hasKey = InventoryManager.Instance.HasItem(_requiredKeyItem.itemId);

        if (hasKey)
            ShowSlotHint();
        else
            HideSlotHint();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (!IsInCurrentScene()) return;

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
        if (_requiredKeyItem == null) return false;

        return item.itemId == _requiredKeyItem.itemId;
    }

    public bool TryUseItem(ItemDefinition item)
    {
        if (!CanHandleItem(item)) return false;
        if (!IsInCurrentScene()) return false;
        if (GameManager.Instance == null || GameManager.Instance.IsLoadingScene()) return false;
        if (_hasUnlocked || GameManager.Instance.GetFlag(_unlockFlag)) return false;
        if (!_playerInRange) return false;

        ExecuteUnlock(item);
        return true;
    }

    private void ExecuteUnlock(ItemDefinition item)
    {
        if (_consumeKeyOnUse && InventoryManager.Instance != null)
        {
            bool consumed = InventoryManager.Instance.TryConsumeItem(item);
            if (!consumed)
                return;
        }

        _hasUnlocked = true;

        if (GameManager.Instance != null && !string.IsNullOrWhiteSpace(_unlockFlag))
            GameManager.Instance.SetFlag(_unlockFlag, true);

        HideSlotHint();
        RefreshSceneState();
    }

    private void RefreshSceneState()
    {
        bool unlocked = _hasUnlocked || (GameManager.Instance != null && GameManager.Instance.GetFlag(_unlockFlag));

        if (_lockRoot != null)
        {
            if (unlocked)
                _lockRoot.SetActive(false);
            else if (_forceLockVisibleBeforeUnlock)
                _lockRoot.SetActive(true);
        }

        if (_letterInteractTarget != null)
            _letterInteractTarget.SetInteractionEnabled(unlocked);
    }

    private bool IsInCurrentScene()
    {
        return string.IsNullOrWhiteSpace(_currentScene) ||
               SceneManager.GetActiveScene().name == _currentScene;
    }

    private int GetCurrentRequiredItemSlot()
    {
        if (_requiredKeyItem == null) return -1;
        if (InventoryManager.Instance == null) return -1;

        return InventoryManager.Instance.FindSlotIndexByItemId(_requiredKeyItem.itemId);
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

    private void LogMissingItemWarningOnce()
    {
        if (!_logMissingReferences) return;
        if (_loggedMissingItemWarning) return;

        _loggedMissingItemWarning = true;
        Debug.LogWarning(
            $"LockUnlockLetterTrigger on '{name}' is missing Required Key Item.",
            this
        );
    }
}
