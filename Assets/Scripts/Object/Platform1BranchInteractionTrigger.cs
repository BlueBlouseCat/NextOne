using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Platform1BranchInteractionTrigger : MonoBehaviour
{
    [Header("Scene Limit")]
    [SerializeField] private string _currentScene = SceneName.Scene1;

    [Header("Required Item")]
    [SerializeField] private ItemDefinition _requiredItem;

    [Header("State")]
    [SerializeField] private string _interactionCompleteFlag = "platform1_branch_used";

    [Header("Window")]
    [SerializeField] private WindowController _windowController;

    [Header("Dialogue UI")]
    [SerializeField] private DialogueUI _dialogueUI;

    [Header("Player Bubble")]
    [SerializeField, TextArea(2, 5)] private string _playerBubbleText = "钥匙就在里面，拜托你帮我拿出来。";
    [SerializeField] private float _playerBubbleDuration = 1.5f;

    [Header("Crow")]
    [SerializeField] private CrowSpineController _crowSpine;
    [SerializeField] private Transform _crowKeyPoint;
    [SerializeField] private Transform _crowPlatformPoint;
    [SerializeField] private float _crowFlyToKeySpeed = 6f;
    [SerializeField] private float _crowFlyBackSpeed = 6f;
    [SerializeField] private float _pauseAfterKeyPickup = 0.15f;

    [Header("Key Reward")]
    [SerializeField] private GameObject _keySceneObject;
    [SerializeField] private ItemDefinition _keyItem;
    [SerializeField] private string _keyCollectedFlag = "item.house_key.collected";

    private bool _playerInRange;
    private bool _slotHintShownByThisScript;
    private bool _hasCompleted;
    private bool _hasGrantedKey;
    private bool _isSequenceRunning;
    private Coroutine _sequenceRoutine;
    private PlayerMovement _playerMovement;

    private void OnEnable()
    {
        _hasCompleted = GameManager.Instance != null && GameManager.Instance.GetFlag(_interactionCompleteFlag);
        _hasGrantedKey = IsKeyAlreadyCollected();

        HideSlotHint();
        RefreshKeySceneState();
        ResolvePlayerMovement();
    }

    private void OnDisable()
    {
        HideSlotHint();
        CloseDialogueUI();
        UnlockPlayer();
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

        if (!_hasGrantedKey)
            _hasGrantedKey = IsKeyAlreadyCollected();

        RefreshKeySceneState();

        if (_hasCompleted || _isSequenceRunning)
        {
            HideSlotHint();
            return;
        }

        if (!_playerInRange)
        {
            HideSlotHint();
            return;
        }

        if (_requiredItem == null || InventoryManager.Instance == null)
        {
            HideSlotHint();
            return;
        }

        bool hasRequiredItem = InventoryManager.Instance.HasItem(_requiredItem.itemId);

        if (hasRequiredItem)
            ShowSlotHint();
        else
            HideSlotHint();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        _playerInRange = true;
        ResolvePlayerMovement(other.transform);
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
        if (_requiredItem == null) return false;

        return item.itemId == _requiredItem.itemId;
    }

    public bool TryUseItem(ItemDefinition item)
    {
        if (!CanHandleItem(item)) return false;
        if (SceneManager.GetActiveScene().name != _currentScene) return false;
        if (GameManager.Instance == null || GameManager.Instance.IsLoadingScene()) return false;
        if (_hasCompleted || GameManager.Instance.GetFlag(_interactionCompleteFlag)) return false;
        if (_isSequenceRunning) return false;
        if (!_playerInRange) return false;

        ExecuteInteraction();
        return true;
    }

    private void ExecuteInteraction()
    {
        _hasCompleted = true;
        _isSequenceRunning = true;

        if (GameManager.Instance != null && !string.IsNullOrWhiteSpace(_interactionCompleteFlag))
            GameManager.Instance.SetFlag(_interactionCompleteFlag, true);

        HideSlotHint();

        if (_sequenceRoutine != null)
            StopCoroutine(_sequenceRoutine);

        _sequenceRoutine = StartCoroutine(InteractionSequenceRoutine());
    }

    private IEnumerator InteractionSequenceRoutine()
    {
        ResolvePlayerMovement();
        LockPlayer();

        if (InventoryUI.Instance != null)
            InventoryUI.Instance.ShowInteractHint(false);

        if (_windowController != null)
            _windowController.OpenWindow();

        if (_crowSpine != null)
            _crowSpine.PlayIdle();

        if (_dialogueUI != null && !string.IsNullOrWhiteSpace(_playerBubbleText))
        {
            DialogueLine playerLine = new DialogueLine
            {
                speaker = DialogueSpeaker.Player,
                content = _playerBubbleText
            };

            _dialogueUI.ShowLine(playerLine, false);
            yield return new WaitForSeconds(_playerBubbleDuration);
            _dialogueUI.Close();
        }

        bool reachedKeyPoint = false;

        if (_crowSpine != null && _crowKeyPoint != null)
        {
            _crowSpine.FlyTo(_crowKeyPoint.position, _crowFlyToKeySpeed, () => reachedKeyPoint = true);

            while (!reachedKeyPoint)
                yield return null;
        }

        GrantKeyReward();

        if (_pauseAfterKeyPickup > 0f)
            yield return new WaitForSeconds(_pauseAfterKeyPickup);

        bool landedOnPlatform = false;

        if (_crowSpine != null && _crowPlatformPoint != null)
        {
            _crowSpine.FlyToAndLand(_crowPlatformPoint.position, _crowFlyBackSpeed, () => landedOnPlatform = true, true);

            while (!landedOnPlatform)
                yield return null;
        }

        UnlockPlayer();
        _isSequenceRunning = false;
        _sequenceRoutine = null;
    }

    private void GrantKeyReward()
    {
        if (_hasGrantedKey)
        {
            RefreshKeySceneState();
            return;
        }

        if (_keyItem == null)
        {
            Debug.LogWarning("Platform1BranchInteractionTrigger: _keyItem 没有赋值。");
            return;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("Platform1BranchInteractionTrigger: 找不到 InventoryManager。");
            return;
        }

        if (!InventoryManager.Instance.HasItem(_keyItem.itemId))
        {
            bool added = InventoryManager.Instance.TryAdd(_keyItem);
            if (!added)
            {
                Debug.LogWarning("Platform1BranchInteractionTrigger: 钥匙加入背包失败，请检查背包是否已满。");
                return;
            }
        }

        string collectedFlag = ResolveKeyCollectedFlag();
        if (GameManager.Instance != null && !string.IsNullOrWhiteSpace(collectedFlag))
            GameManager.Instance.SetFlag(collectedFlag, true);

        _hasGrantedKey = true;
        RefreshKeySceneState();

        if (InventoryUI.Instance != null)
            InventoryUI.Instance.Refresh();
    }

    private bool IsKeyAlreadyCollected()
    {
        string collectedFlag = ResolveKeyCollectedFlag();

        if (GameManager.Instance != null &&
            !string.IsNullOrWhiteSpace(collectedFlag) &&
            GameManager.Instance.GetFlag(collectedFlag))
        {
            return true;
        }

        if (_keyItem != null &&
            InventoryManager.Instance != null &&
            InventoryManager.Instance.HasItem(_keyItem.itemId))
        {
            return true;
        }

        return false;
    }

    private string ResolveKeyCollectedFlag()
    {
        if (!string.IsNullOrWhiteSpace(_keyCollectedFlag))
            return _keyCollectedFlag;

        if (_keyItem != null)
            return _keyItem.CollectedFlag;

        return string.Empty;
    }

    private void RefreshKeySceneState()
    {
        if (_keySceneObject != null)
            _keySceneObject.SetActive(!_hasGrantedKey);
    }

    private void ResolvePlayerMovement()
    {
        ResolvePlayerMovement(GameManager.Instance != null ? GameManager.Instance.CurrentPlayer : null);
    }

    private void ResolvePlayerMovement(Transform playerTransform)
    {
        if (_playerMovement != null) return;

        if (playerTransform != null)
            _playerMovement = playerTransform.GetComponent<PlayerMovement>();

        if (_playerMovement == null)
            _playerMovement = FindObjectOfType<PlayerMovement>();
    }

    private void LockPlayer()
    {
        if (_playerMovement != null)
            _playerMovement.SetExternalInputLocked(true);
    }

    private void UnlockPlayer()
    {
        if (_playerMovement != null)
            _playerMovement.SetExternalInputLocked(false);
    }

    private void CloseDialogueUI()
    {
        if (_dialogueUI != null)
            _dialogueUI.Close();
    }

    private int GetCurrentRequiredItemSlot()
    {
        if (_requiredItem == null) return -1;
        if (InventoryManager.Instance == null) return -1;

        return InventoryManager.Instance.FindSlotIndexByItemId(_requiredItem.itemId);
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
