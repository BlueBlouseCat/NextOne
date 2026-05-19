using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class HankyTrigger : MonoBehaviour
{
    private enum SequenceState
    {
        None,
        PlayerBubblePlaying,
        WaitingForDialogueStart,
        DialogueRunning,
        Completed
    }

    [Header("Scene")]
    [SerializeField] private string _currentScene = "MouseHole7";

    [Header("References")]
    [SerializeField] private DialogueUI _dialogueUI;
    [SerializeField] private MouseIdleController _mouseController;
    [SerializeField] private GameObject _hankySceneObject;
    [SerializeField] private ItemDefinition _hankyItem;
    [SerializeField] private GameObject _txtMouseThinking;

    [Header("Player Bubble")]
    [SerializeField, TextArea(2, 5)] private string _playerBubbleText = "哦！阿瑟的手帕！";
    [SerializeField] private float _playerBubbleDuration = 1.5f;

    [Header("Dialogue")]
    [SerializeField] private DialogueLine[] _dialogueLines;

    [Header("Flags")]
    [SerializeField] private string _sequenceCompleteFlag = "mousehole7_hanky_sequence_done";

    private SequenceState _state;
    private PlayerMovement _playerMovement;
    private PlayerItemController _playerItemController;
    private bool _playerInRange;
    private bool _hintShownByThisScript;
    private bool _ignoreAdvanceUntilKeyRelease;
    private int _currentDialogueIndex;
    private Coroutine _playerBubbleRoutine;

    private void Awake()
    {
        if (_dialogueUI == null)
            _dialogueUI = FindObjectOfType<DialogueUI>();

        if (_mouseController == null)
            _mouseController = FindObjectOfType<MouseIdleController>();

        if (_hankySceneObject == null)
            _hankySceneObject = transform.parent != null ? transform.parent.gameObject : null;

        if (_txtMouseThinking == null)
        {
            GameObject thinking = GameObject.Find("TxtMouseThinking");
            if (thinking != null)
                _txtMouseThinking = thinking;
        }
    }

    private void Start()
    {
        RefreshState();
    }

    private void OnEnable()
    {
        RefreshState();
    }

    private void OnDisable()
    {
        if (_playerBubbleRoutine != null)
        {
            StopCoroutine(_playerBubbleRoutine);
            _playerBubbleRoutine = null;
        }

        HideHint();
        CloseDialogueUI();
        UnlockPlayer();
        HideMouseThinking();

        _playerInRange = false;
        _ignoreAdvanceUntilKeyRelease = false;
        _currentDialogueIndex = 0;
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name != _currentScene)
        {
            HideHint();
            return;
        }

        if (GameManager.Instance == null) return;
        if (GameManager.Instance.IsLoadingScene()) return;

        if (_state == SequenceState.Completed)
        {
            HideHint();
            return;
        }

        if (_state == SequenceState.PlayerBubblePlaying)
        {
            HideHint();
            return;
        }

        if (_state == SequenceState.WaitingForDialogueStart)
        {
            if (_playerInRange)
                ShowHint();
            else
                HideHint();

            if (!_playerInRange) return;
            if (!GameplayInputUtil.InteractPressedThisFrame()) return;

            StartDialogue();
            return;
        }

        if (_state == SequenceState.DialogueRunning)
        {
            HideHint();

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

        HideHint();
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
        if (_state == SequenceState.Completed) return;

        _playerInRange = inRange;

        if (!inRange)
        {
            HideHint();
            return;
        }

        _playerMovement = other.GetComponent<PlayerMovement>();
        if (_playerMovement == null)
            _playerMovement = other.GetComponentInParent<PlayerMovement>();

        _playerItemController = other.GetComponent<PlayerItemController>();
        if (_playerItemController == null)
            _playerItemController = other.GetComponentInParent<PlayerItemController>();

        if (_state == SequenceState.None)
            StartPlayerBubble();
    }

    private void StartPlayerBubble()
    {
        if (_state != SequenceState.None) return;
        if (_dialogueUI == null) return;
        if (string.IsNullOrWhiteSpace(_playerBubbleText)) return;

        _state = SequenceState.PlayerBubblePlaying;

        if (_playerBubbleRoutine != null)
            StopCoroutine(_playerBubbleRoutine);

        _playerBubbleRoutine = StartCoroutine(PlayerBubbleRoutine());
    }

    private IEnumerator PlayerBubbleRoutine()
    {
        LockPlayer();
        HideHint();
        HideMouseThinking();

        DialogueLine playerLine = new DialogueLine
        {
            speaker = DialogueSpeaker.Player,
            content = _playerBubbleText
        };

        _dialogueUI.ShowLine(playerLine, false);
        yield return new WaitForSeconds(_playerBubbleDuration);

        CloseDialogueUI();
        UnlockPlayer();

        _playerBubbleRoutine = null;

        if (_state == SequenceState.Completed)
            yield break;

        _state = _playerInRange
            ? SequenceState.WaitingForDialogueStart
            : SequenceState.None;
    }

    private void StartDialogue()
    {
        if (_dialogueUI == null) return;
        if (_dialogueLines == null || _dialogueLines.Length == 0) return;

        _state = SequenceState.DialogueRunning;
        _ignoreAdvanceUntilKeyRelease = true;
        _currentDialogueIndex = 0;

        HideHint();
        LockPlayer();
        ShowCurrentDialogueLine();
    }

    private void AdvanceDialogue()
    {
        _currentDialogueIndex++;

        if (_dialogueLines == null || _currentDialogueIndex >= _dialogueLines.Length)
        {
            EndDialogueAndCollectHanky();
            return;
        }

        ShowCurrentDialogueLine();
    }

    private void ShowCurrentDialogueLine()
    {
        if (_dialogueUI == null) return;
        if (_dialogueLines == null) return;
        if (_currentDialogueIndex < 0 || _currentDialogueIndex >= _dialogueLines.Length) return;

        _dialogueUI.ShowLine(_dialogueLines[_currentDialogueIndex]);
        _mouseController?.PlayIdle();
        UpdateMouseThinkingState();
    }

    private void UpdateMouseThinkingState()
    {
        if (_txtMouseThinking == null) return;

        bool shouldShow = false;

        if (_dialogueLines != null &&
            _dialogueLines.Length > 0 &&
            _currentDialogueIndex == _dialogueLines.Length - 1)
        {
            DialogueLine currentLine = _dialogueLines[_currentDialogueIndex];
            shouldShow = currentLine != null && currentLine.speaker == DialogueSpeaker.Other;
        }

        _txtMouseThinking.SetActive(shouldShow);
    }

    private void HideMouseThinking()
    {
        if (_txtMouseThinking != null)
            _txtMouseThinking.SetActive(false);
    }

    private void EndDialogueAndCollectHanky()
    {
        CloseDialogueUI();
        UnlockPlayer();

        bool added = TryCollectHanky();

        _state = SequenceState.Completed;
        _playerInRange = false;
        _ignoreAdvanceUntilKeyRelease = false;
        _currentDialogueIndex = 0;
        HideHint();

        if (!added) return;

        if (GameManager.Instance != null && !string.IsNullOrWhiteSpace(_sequenceCompleteFlag))
            GameManager.Instance.SetFlag(_sequenceCompleteFlag, true);

        if (InventoryUI.Instance != null)
            InventoryUI.Instance.Refresh();

        OpenHankyInspectPopup();
    }

    private bool TryCollectHanky()
    {
        if (_hankyItem == null)
        {
            Debug.LogWarning("HankyTrigger: _hankyItem is null.");
            return false;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("HankyTrigger: InventoryManager not found.");
            return false;
        }

        if (!InventoryManager.Instance.HasItem(_hankyItem.itemId))
        {
            bool added = InventoryManager.Instance.TryAdd(_hankyItem);
            if (!added)
            {
                Debug.LogWarning("HankyTrigger: failed to add hanky into inventory.");
                return false;
            }
        }

        if (GameManager.Instance != null)
            GameManager.Instance.SetFlag(_hankyItem.CollectedFlag, true);

        if (_hankySceneObject != null)
            _hankySceneObject.SetActive(false);

        return true;
    }

    private void OpenHankyInspectPopup()
    {
        if (InventoryUI.Instance == null) return;
        if (_hankyItem == null) return;

        if (_playerMovement != null)
            _playerMovement.SetExternalInputLocked(true);

        InventoryUI.Instance.OpenInspectPopup(
            _hankyItem.displayName,
            _hankyItem.description,
            _playerItemController
        );
    }

    private void RefreshState()
    {
        bool completed = GameManager.Instance != null &&
                         GameManager.Instance.GetFlag(_sequenceCompleteFlag);

        _playerInRange = false;
        _hintShownByThisScript = false;
        _ignoreAdvanceUntilKeyRelease = false;
        _currentDialogueIndex = 0;

        HideHint();
        CloseDialogueUI();
        HideMouseThinking();

        if (completed)
        {
            _state = SequenceState.Completed;

            if (_hankySceneObject != null)
                _hankySceneObject.SetActive(false);

            return;
        }

        _state = SequenceState.None;

        if (_hankySceneObject != null)
            _hankySceneObject.SetActive(true);
    }

    private void ShowHint()
    {
        if (_hintShownByThisScript) return;
        if (InventoryUI.Instance == null) return;

        InventoryUI.Instance.ShowInteractHint(true, ProjectInteractionHints.Interact);
        _hintShownByThisScript = true;
    }

    private void HideHint()
    {
        if (!_hintShownByThisScript) return;
        if (InventoryUI.Instance == null) return;

        InventoryUI.Instance.ShowInteractHint(false);
        _hintShownByThisScript = false;
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
}
