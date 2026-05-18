using UnityEngine;
using UnityEngine.SceneManagement;

public class MouseBreadDeliveryTrigger : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string _currentScene = "MouseHole2";

    [Header("References")]
    [SerializeField] private DialogueUI _dialogueUI;
    [SerializeField] private MouseSpineController2 _mouseController;

    [Header("Required Item")]
    [SerializeField] private ItemDefinition _requiredItem;

    [Header("Flags")]
    [SerializeField] private string _requiredDialogueFlag = "mouse_blocked_dialogue_done";
    [SerializeField] private string _deliveryDoneFlag = "mousehole2_bread_delivered";
    [SerializeField] private string _mouseDisappearedFlag = "mousehole2_mouse_disappeared";

    [Header("After Delivery Dialogue")]
    [SerializeField] private DialogueLine[] _deliveryLines;

    [Header("After Dialogue")]
    [SerializeField] private bool _playDisappearAfterDialogue = true;
    [SerializeField] private bool _disableMouseCollidersAfterDialogue = true;
    [SerializeField] private Collider2D[] _mouseCollidersToDisable;
    [SerializeField] private GameObject _triggerRootToDisable;

    private PlayerMovement _playerMovement;
    private bool _playerInRange;
    private bool _slotHintShownByThisScript;
    private bool _deliveryDone;
    private bool _mouseDisappeared;
    private bool _isSequenceRunning;
    private bool _isDialogueRunning;
    private bool _ignoreAdvanceUntilKeyRelease;
    private bool _holdsGlobalLock;
    private bool _mouseCollidersDisabled;
    private int _currentLineIndex;

    private void Awake()
    {
        if (_dialogueUI == null)
            _dialogueUI = FindObjectOfType<DialogueUI>();

        if (_mouseController == null)
            _mouseController = FindObjectOfType<MouseSpineController2>();

        if (_triggerRootToDisable == null)
            _triggerRootToDisable = gameObject;
    }

    private void OnEnable()
    {
        _deliveryDone = GameManager.Instance != null && GameManager.Instance.GetFlag(_deliveryDoneFlag);
        _mouseDisappeared = GameManager.Instance != null && GameManager.Instance.GetFlag(_mouseDisappearedFlag);

        _playerInRange = false;
        _slotHintShownByThisScript = false;
        _isSequenceRunning = false;
        _isDialogueRunning = false;
        _ignoreAdvanceUntilKeyRelease = false;
        _holdsGlobalLock = false;
        _currentLineIndex = 0;
        _mouseCollidersDisabled = false;

        HideSlotHint();
        CloseDialogueUI();
        UnlockPlayer();
        ReleaseGlobalLock();
        ResolvePlayerMovement();

        if (_mouseDisappeared)
        {
            DisableMouseCollidersIfNeeded();
            DisableTriggerRootIfNeeded();
            return;
        }

        if (_deliveryDone)
            DisableMouseCollidersIfNeeded();
    }

    private void OnDisable()
    {
        HideSlotHint();
        CloseDialogueUI();
        UnlockPlayer();
        ReleaseGlobalLock();

        _playerInRange = false;
        _isSequenceRunning = false;
        _isDialogueRunning = false;
        _ignoreAdvanceUntilKeyRelease = false;
        _currentLineIndex = 0;
        _playerMovement = null;
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

        if (!_deliveryDone)
            _deliveryDone = GameManager.Instance.GetFlag(_deliveryDoneFlag);

        if (!_mouseDisappeared && !string.IsNullOrWhiteSpace(_mouseDisappearedFlag))
            _mouseDisappeared = GameManager.Instance.GetFlag(_mouseDisappearedFlag);

        if (_mouseDisappeared)
        {
            HideSlotHint();
            return;
        }

        if (_isDialogueRunning)
        {
            if (_ignoreAdvanceUntilKeyRelease)
            {
                if (!GameplayInputUtil.InteractHeld())
                    _ignoreAdvanceUntilKeyRelease = false;

                return;
            }

            if (!GameplayInputUtil.InteractPressedThisFrame()) return;

            AdvanceDialogue();
            return;
        }

        if (_isSequenceRunning)
        {
            HideSlotHint();
            return;
        }

        if (_deliveryDone)
        {
            HideSlotHint();
            DisableMouseCollidersIfNeeded();
            return;
        }

        bool prerequisiteDone =
            string.IsNullOrWhiteSpace(_requiredDialogueFlag) ||
            GameManager.Instance.GetFlag(_requiredDialogueFlag);

        if (!prerequisiteDone)
        {
            HideSlotHint();
            return;
        }

        if (!_playerInRange)
        {
            HideSlotHint();
            return;
        }

        int slotIndex = GetCurrentRequiredItemSlot();
        bool hasRequiredItem = slotIndex >= 0;

        if (hasRequiredItem)
            ShowSlotHint(slotIndex);
        else
            HideSlotHint();

        if (!hasRequiredItem) return;
        if (!GameplayInputUtil.SlotPressedThisFrame(slotIndex)) return;

        TryDeliverBread();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        UpdatePlayerRange(other, true);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        UpdatePlayerRange(other, true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        UpdatePlayerRange(other, false);
    }

    private void UpdatePlayerRange(Collider2D other, bool inRange)
    {
        if (other == null || !other.CompareTag("Player")) return;
        if (SceneManager.GetActiveScene().name != _currentScene) return;
        if (_mouseDisappeared) return;

        _playerInRange = inRange;

        if (inRange)
        {
            _playerMovement = other.GetComponent<PlayerMovement>();
            if (_playerMovement == null)
                _playerMovement = other.GetComponentInParent<PlayerMovement>();
        }
        else
        {
            HideSlotHint();

            if (!_isDialogueRunning && !_isSequenceRunning)
                _playerMovement = null;
        }
    }

    private int GetCurrentRequiredItemSlot()
    {
        if (_requiredItem == null) return -1;
        if (InventoryManager.Instance == null) return -1;

        return InventoryManager.Instance.FindSlotIndexByItemId(_requiredItem.itemId);
    }

    private void TryDeliverBread()
    {
        if (_requiredItem == null) return;
        if (InventoryManager.Instance == null) return;

        bool consumed = InventoryManager.Instance.TryConsumeItem(_requiredItem);
        if (!consumed) return;

        _deliveryDone = true;

        if (GameManager.Instance != null && !string.IsNullOrWhiteSpace(_deliveryDoneFlag))
            GameManager.Instance.SetFlag(_deliveryDoneFlag, true);

        HideSlotHint();
        ResolvePlayerMovement();
        LockPlayer();
        AcquireGlobalLock();

        _isSequenceRunning = true;

        if (InventoryUI.Instance != null)
            InventoryUI.Instance.ShowInteractHint(false);

        if (_mouseController != null)
            _mouseController.PlayEatThenFedIdle(OnEatFinished);
        else
            OnEatFinished();
    }

    private void OnEatFinished()
    {
        if (!isActiveAndEnabled) return;

        _isSequenceRunning = false;

        if (_deliveryLines == null || _deliveryLines.Length == 0 || _dialogueUI == null)
        {
            if (_playDisappearAfterDialogue)
                StartDisappearSequence();
            else
                FinishVisibleSequence();

            return;
        }

        _isDialogueRunning = true;
        _ignoreAdvanceUntilKeyRelease = false;
        _currentLineIndex = 0;

        ShowCurrentLine();
    }

    private void AdvanceDialogue()
    {
        _currentLineIndex++;

        if (_currentLineIndex >= _deliveryLines.Length)
        {
            EndDialogue();
            return;
        }

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        if (_dialogueUI == null) return;
        if (_deliveryLines == null) return;
        if (_currentLineIndex < 0 || _currentLineIndex >= _deliveryLines.Length) return;

        _dialogueUI.Open();
        _dialogueUI.ShowLine(_deliveryLines[_currentLineIndex]);
        _mouseController?.PlayFedIdle();
    }

    private void EndDialogue()
    {
        _isDialogueRunning = false;
        _ignoreAdvanceUntilKeyRelease = false;
        _currentLineIndex = 0;

        if (_playDisappearAfterDialogue)
        {
            StartDisappearSequence();
            return;
        }

        FinishVisibleSequence();
    }

    private void StartDisappearSequence()
    {
        CloseDialogueUI();
        HideSlotHint();
        _isSequenceRunning = true;

        if (_mouseController != null)
            _mouseController.PlayDisappear(OnDisappearFinished);
        else
            OnDisappearFinished();
    }

    private void OnDisappearFinished()
    {
        _mouseDisappeared = true;
        _isSequenceRunning = false;

        CloseDialogueUI();
        UnlockPlayer();
        ReleaseGlobalLock();
        DisableMouseCollidersIfNeeded();
        DisableTriggerRootIfNeeded();
    }

    private void FinishVisibleSequence()
    {
        CloseDialogueUI();
        UnlockPlayer();
        ReleaseGlobalLock();
        _mouseController?.PlayFedIdle();
        DisableMouseCollidersIfNeeded();
    }

    private void DisableMouseCollidersIfNeeded()
    {
        if (!_disableMouseCollidersAfterDialogue) return;
        if (_mouseCollidersDisabled) return;

        if (_mouseCollidersToDisable != null && _mouseCollidersToDisable.Length > 0)
        {
            for (int i = 0; i < _mouseCollidersToDisable.Length; i++)
            {
                if (_mouseCollidersToDisable[i] != null)
                    _mouseCollidersToDisable[i].enabled = false;
            }

            _mouseCollidersDisabled = true;
            return;
        }

        if (_mouseController != null)
        {
            Collider2D[] colliders = _mouseController.GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                    colliders[i].enabled = false;
            }

            _mouseCollidersDisabled = true;
        }
    }

    private void DisableTriggerRootIfNeeded()
    {
        if (_triggerRootToDisable != null && _triggerRootToDisable.activeSelf)
            _triggerRootToDisable.SetActive(false);
    }

    private void ResolvePlayerMovement()
    {
        if (_playerMovement != null) return;

        if (GameManager.Instance != null && GameManager.Instance.CurrentPlayer != null)
            _playerMovement = GameManager.Instance.CurrentPlayer.GetComponent<PlayerMovement>();

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

    private void AcquireGlobalLock()
    {
        if (_holdsGlobalLock) return;

        GlobalInteractionLock.Acquire();
        _holdsGlobalLock = true;
    }

    private void ReleaseGlobalLock()
    {
        if (!_holdsGlobalLock) return;

        GlobalInteractionLock.Release();
        _holdsGlobalLock = false;
    }

    private void ShowSlotHint(int slotIndex)
    {
        if (_slotHintShownByThisScript) return;
        if (InventoryUI.Instance == null) return;

        InventoryUI.Instance.PlaySlotHint(slotIndex);
        _slotHintShownByThisScript = true;
    }

    private void HideSlotHint()
    {
        if (!_slotHintShownByThisScript) return;
        if (InventoryUI.Instance == null) return;

        InventoryUI.Instance.StopSlotHint();
        _slotHintShownByThisScript = false;
    }

    private void CloseDialogueUI()
    {
        if (_dialogueUI != null)
            _dialogueUI.Close();
    }
}