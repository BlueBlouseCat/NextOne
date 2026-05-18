using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MouseBlockedTrigger : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string _currentScene = "MouseHole2";

    [Header("References")]
    [SerializeField] private DialogueUI _dialogueUI;
    [SerializeField] private MouseSpineController2 _mouseController;

    [Header("Dialogue Lines")]
    [SerializeField] private DialogueLine[] _lines;

    [Header("Optional")]
    [SerializeField] private bool _playOnlyOnce = true;
    [SerializeField] private string _playedFlag = "mouse_blocked_dialogue_done";

    private PlayerMovement _playerMovement;
    private bool _playerInRange;
    private bool _isDialogueRunning;
    private bool _ignoreAdvanceUntilKeyRelease;
    private bool _hasPlayed;
    private bool _hintShownByThisScript;
    private int _currentLineIndex;

    private void Awake()
    {
        if (_dialogueUI == null)
            _dialogueUI = FindObjectOfType<DialogueUI>();

        if (_mouseController == null)
            _mouseController = FindObjectOfType<MouseSpineController2>();
    }

    private void Start()
    {
        if (SceneManager.GetActiveScene().name != _currentScene) return;
        _mouseController?.PlayIdle();
    }

    private void OnEnable()
    {
        _hasPlayed = GameManager.Instance != null && GameManager.Instance.GetFlag(_playedFlag);

        _playerInRange = false;
        _isDialogueRunning = false;
        _ignoreAdvanceUntilKeyRelease = false;
        _currentLineIndex = 0;

        HideHint();
        CloseDialogueUI();
        UnlockPlayer();
    }

    private void OnDisable()
    {
        HideHint();
        CloseDialogueUI();
        UnlockPlayer();

        _playerInRange = false;
        _isDialogueRunning = false;
        _ignoreAdvanceUntilKeyRelease = false;
        _currentLineIndex = 0;
        _playerMovement = null;
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

        if (_playOnlyOnce && !_hasPlayed)
            _hasPlayed = GameManager.Instance.GetFlag(_playedFlag);

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

        if (_playOnlyOnce && _hasPlayed)
        {
            HideHint();
            return;
        }

        if (_playerInRange)
            ShowHint();
        else
            HideHint();

        if (!_playerInRange) return;
        if (!GameplayInputUtil.InteractPressedThisFrame()) return;

        StartDialogue();
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

        if (_playOnlyOnce && _hasPlayed)
        {
            _playerInRange = false;
            HideHint();
            return;
        }

        _playerInRange = inRange;

        if (!inRange)
        {
            HideHint();

            if (!_isDialogueRunning)
                _playerMovement = null;

            return;
        }

        _playerMovement = other.GetComponent<PlayerMovement>();
        if (_playerMovement == null)
            _playerMovement = other.GetComponentInParent<PlayerMovement>();
    }

    private void StartDialogue()
    {
        if (_playOnlyOnce && _hasPlayed) return;
        if (_lines == null || _lines.Length == 0) return;
        if (_dialogueUI == null) return;

        _isDialogueRunning = true;
        _ignoreAdvanceUntilKeyRelease = true;
        _currentLineIndex = 0;

        HideHint();
        LockPlayer();

        _dialogueUI.Open();
        ShowCurrentLine();
    }

    private void AdvanceDialogue()
    {
        _currentLineIndex++;

        if (_currentLineIndex >= _lines.Length)
        {
            EndDialogue();
            return;
        }

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        if (_dialogueUI == null) return;
        if (_lines == null) return;
        if (_currentLineIndex < 0 || _currentLineIndex >= _lines.Length) return;

        _dialogueUI.Open();
        _dialogueUI.ShowLine(_lines[_currentLineIndex]);
        _mouseController?.PlayIdle();
    }

    private void EndDialogue()
    {
        _isDialogueRunning = false;
        _ignoreAdvanceUntilKeyRelease = false;
        _currentLineIndex = 0;

        CloseDialogueUI();
        UnlockPlayer();
        HideHint();

        _hasPlayed = true;
        _playerInRange = false;

        if (GameManager.Instance != null && !string.IsNullOrWhiteSpace(_playedFlag))
            GameManager.Instance.SetFlag(_playedFlag, true);
    }

    private void ShowHint()
    {
        if (_hintShownByThisScript) return;
        if (InventoryUI.Instance == null) return;

        InventoryUI.Instance.ShowInteractHint(true);
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
