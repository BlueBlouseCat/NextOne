using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class CrowDeliveryTrigger : MonoBehaviour
{
    [Header("Scene Limit")]
    [SerializeField] private string _currentScene = SceneName.Scene1; // OutsideOfHouse

    [Header("Crow")]
    [SerializeField] private GameObject _crowRoot;
    [SerializeField] private CrowSpineController _crowSpine;

    [Header("Delivery")]
    [SerializeField] private ItemDefinition _requiredItem;
    [SerializeField] private float _triggerRadius = 1.2f;
    [SerializeField] private string _deliveryCompleteFlag = "crow_delivery_done";
    [SerializeField] private string _introDialogueCompleteFlag = "crow_intro_dialogue_done";

    [Header("UI")]
    [SerializeField] private DialogueUI _dialogueUI;

    [Header("After Delivery Dialogue")]
    [SerializeField] private DialogueLine[] _afterDeliveryLines;

    private Transform _player;
    private PlayerMovement _playerMovement;
    private bool _isInRange;
    private bool _isDialogueRunning;
    private bool _slotHintShownByThisScript;
    private bool _hasDelivered;
    private bool _introDialogueCompleted;
    private int _currentLineIndex;

    private void OnEnable()
    {
        _hasDelivered = GameManager.Instance != null && GameManager.Instance.GetFlag(_deliveryCompleteFlag);
        _introDialogueCompleted = GameManager.Instance != null && GameManager.Instance.GetFlag(_introDialogueCompleteFlag);

        HideDeliveryHint();
        CloseDialogueUI();
    }

    private void OnDisable()
    {
        HideDeliveryHint();
        CloseDialogueUI();
        UnlockPlayer();
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name != _currentScene)
        {
            UpdateRangeState(false);
            return;
        }

        if (_crowRoot != null && !_crowRoot.activeInHierarchy)
        {
            UpdateRangeState(false);
            return;
        }

        if (GameManager.Instance == null) return;
        if (GameManager.Instance.IsLoadingScene()) return;

        if (!_introDialogueCompleted)
            _introDialogueCompleted = GameManager.Instance.GetFlag(_introDialogueCompleteFlag);

        if (!_hasDelivered)
            _hasDelivered = GameManager.Instance.GetFlag(_deliveryCompleteFlag);

        if (_player == null)
            _player = GameManager.Instance.CurrentPlayer;

        if (_player == null)
        {
            UpdateRangeState(false);
            return;
        }

        if (_playerMovement == null)
            _playerMovement = _player.GetComponent<PlayerMovement>();

        float sqrDistance = ((Vector2)_player.position - (Vector2)transform.position).sqrMagnitude;
        bool inRange = sqrDistance <= _triggerRadius * _triggerRadius;

        UpdateRangeState(inRange);

        if (_isDialogueRunning)
        {
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
                AdvanceDialogue();

            return;
        }

        if (_hasDelivered)
        {
            HideDeliveryHint();
            return;
        }

        if (!_introDialogueCompleted)
        {
            HideDeliveryHint();
            return;
        }

        if (!inRange)
        {
            HideDeliveryHint();
            return;
        }

        if (_requiredItem == null || InventoryManager.Instance == null)
        {
            HideDeliveryHint();
            return;
        }

        bool hasItem = InventoryManager.Instance.HasItem(_requiredItem.itemId);

        if (hasItem)
            ShowDeliveryHint();
        else
            HideDeliveryHint();

        if (!hasItem) return;
        if (Keyboard.current == null) return;
        if (!WasRequiredSlotPressedThisFrame()) return;

        TryDeliver();
    }

    private void UpdateRangeState(bool inRange)
    {
        _isInRange = inRange;

        if (!inRange && !_isDialogueRunning)
            HideDeliveryHint();
    }

    private int GetCurrentRequiredItemSlot()
    {
        if (_requiredItem == null) return -1;
        if (InventoryManager.Instance == null) return -1;

        return InventoryManager.Instance.FindSlotIndexByItemId(_requiredItem.itemId);
    }

    private bool WasRequiredSlotPressedThisFrame()
    {
        if (Keyboard.current == null) return false;

        int slot = GetCurrentRequiredItemSlot();
        if (slot < 0) return false;

        switch (slot)
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

    private void TryDeliver()
    {
        if (_requiredItem == null) return;
        if (InventoryManager.Instance == null) return;

        bool consumed = InventoryManager.Instance.TryConsumeItem(_requiredItem);
        if (!consumed) return;

        _hasDelivered = true;

        if (GameManager.Instance != null && !string.IsNullOrWhiteSpace(_deliveryCompleteFlag))
            GameManager.Instance.SetFlag(_deliveryCompleteFlag, true);

        HideDeliveryHint();
        StartAfterDeliveryDialogue();
    }

    private void StartAfterDeliveryDialogue()
    {
        if (_afterDeliveryLines == null || _afterDeliveryLines.Length == 0)
        {
            _crowSpine?.PlayIdle();
            return;
        }

        _isDialogueRunning = true;
        _currentLineIndex = 0;

        LockPlayer();
        ShowCurrentLine();
    }

    private void AdvanceDialogue()
    {
        _currentLineIndex++;

        if (_afterDeliveryLines == null || _currentLineIndex >= _afterDeliveryLines.Length)
        {
            EndDialogue();
            return;
        }

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        if (_dialogueUI == null) return;

        DialogueLine line = _afterDeliveryLines[_currentLineIndex];
        _dialogueUI.ShowLine(line);

        if (_crowSpine == null) return;

        if (line.speaker == DialogueSpeaker.Player)
            _crowSpine.PlayIdle();
        else
            _crowSpine.PlaySpeak();
    }

    private void EndDialogue()
    {
        _isDialogueRunning = false;
        _currentLineIndex = 0;

        CloseDialogueUI();
        UnlockPlayer();

        _crowSpine?.PlayIdle();
    }

    private void ShowDeliveryHint()
    {
        if (_isDialogueRunning) return;

        int slot = GetCurrentRequiredItemSlot();
        if (slot < 0) return;

        if (!_slotHintShownByThisScript && InventoryUI.Instance != null)
        {
            InventoryUI.Instance.PlaySlotHint(slot);
            _slotHintShownByThisScript = true;
        }
    }

    private void HideDeliveryHint()
    {
        if (_slotHintShownByThisScript && InventoryUI.Instance != null)
        {
            InventoryUI.Instance.StopSlotHint();
            _slotHintShownByThisScript = false;
        }
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, _triggerRadius);
    }
}
