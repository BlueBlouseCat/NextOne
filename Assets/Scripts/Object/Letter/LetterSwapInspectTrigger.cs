using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class LetterSwapInspectTrigger : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string _currentScene = "House";

    [Header("References")]
    [SerializeField] private GameObject _letter1Root;
    [SerializeField] private GameObject _letter2Root;
    [SerializeField] private Transform _focusPoint;

    [Header("Options")]
    [SerializeField] private bool _forceLetter1VisibleOnStart = true;
    [SerializeField] private bool _forceLetter2HiddenOnStart = true;
    [SerializeField] private bool _autoCloseOnExit = true;
    [SerializeField] private int _interactionPriority = 10;

    [Header("Optional Ending Sequence")]
    [SerializeField] private bool _triggerEndingAfterLetter2Closed = true;
    [SerializeField] private DialogueUI _dialogueUI;
    [SerializeField] private DialogueLine _afterLetter2Dialogue;
    [SerializeField] private float _dialogueDuration = 2f;
    [SerializeField] private string _endingSceneName = "EndScene";
    [SerializeField] private bool _useFadeToEndingScene = true;
    [SerializeField] private bool _playEndingOnlyOnce = true;
    [SerializeField] private string _endingTriggeredFlag = "house.letter2.ending_triggered";

    private bool _playerInRange;
    private bool _hintShownByThisScript;
    private bool _holdsInteractionLock;
    private bool _isViewingLetter2;
    private bool _interactionEnabled;
    private bool _hasViewedLetter2Once;
    private bool _isEndingSequenceRunning;
    private bool _hasTriggeredEnding;
    private Transform _playerTransform;
    private PlayerMovement _playerMovement;
    private Coroutine _endingRoutine;

    private void Awake()
    {
        if (_focusPoint == null)
            _focusPoint = transform;
    }

    private void OnEnable()
    {
        HideHint();
        InteractionFocusService.SetCandidate(this, _focusPoint, false, _interactionPriority);

        if (_forceLetter1VisibleOnStart && _letter1Root != null)
            _letter1Root.SetActive(true);

        if (_forceLetter2HiddenOnStart && _letter2Root != null)
            _letter2Root.SetActive(false);

        _playerInRange = false;
        _playerTransform = null;
        _isViewingLetter2 = false;
        _hasViewedLetter2Once = false;
        _isEndingSequenceRunning = false;
        _hasTriggeredEnding = GameManager.Instance != null && GameManager.Instance.GetFlag(_endingTriggeredFlag);
    }

    private void OnDisable()
    {
        HideHint();
        InteractionFocusService.RemoveCandidate(this);

        _playerInRange = false;
        _playerTransform = null;
        _isViewingLetter2 = false;
        _isEndingSequenceRunning = false;

        if (_endingRoutine != null)
        {
            StopCoroutine(_endingRoutine);
            _endingRoutine = null;
        }

        if (_letter2Root != null)
            _letter2Root.SetActive(false);

        if (_letter1Root != null)
            _letter1Root.SetActive(true);

        if (_dialogueUI != null)
            _dialogueUI.Close();

        ReleaseInteractionLock();
        UnlockPlayer();
    }

    private void Update()
    {
        if (!IsInCurrentScene())
        {
            HideHint();
            InteractionFocusService.SetCandidate(this, _focusPoint, false, _interactionPriority);
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.IsLoadingScene())
        {
            HideHint();
            InteractionFocusService.SetCandidate(this, _focusPoint, false, _interactionPriority);
            return;
        }

        if (_isEndingSequenceRunning)
        {
            HideHint();
            InteractionFocusService.SetCandidate(this, _focusPoint, false, _interactionPriority);
            return;
        }

        bool canCandidateFocus = _interactionEnabled && _playerInRange;
        InteractionFocusService.SetCandidate(this, _focusPoint, canCandidateFocus, _interactionPriority);

        bool interactionAvailable = !GlobalInteractionLock.IsLocked || _holdsInteractionLock;

        bool hasFocus =
            _interactionEnabled &&
            _playerInRange &&
            _playerTransform != null &&
            interactionAvailable &&
            InteractionFocusService.HasFocus(this, _playerTransform.position);

        if (hasFocus)
            ShowHint(_isViewingLetter2 ? ProjectInteractionHints.Close : ProjectInteractionHints.Interact);
        else
            HideHint();

        if (!hasFocus) return;

        if (!_isViewingLetter2)
        {
            if (GameplayInputUtil.InteractPressedThisFrame())
                SetViewingState(true);

            return;
        }

        if (GameplayInputUtil.CancelPressedThisFrame() && GameplayInputUtil.ConsumeCancelThisFrame())
            SetViewingState(false);
    }

    private void OnTriggerEnter2D(Collider2D other) => UpdatePlayerRange(other, true);
    private void OnTriggerStay2D(Collider2D other) => UpdatePlayerRange(other, true);
    private void OnTriggerExit2D(Collider2D other) => UpdatePlayerRange(other, false);

    private void UpdatePlayerRange(Collider2D other, bool inRange)
    {
        if (other == null || !other.CompareTag("Player")) return;
        if (!IsInCurrentScene()) return;

        _playerInRange = inRange;

        if (inRange)
        {
            _playerTransform = other.transform;

            if (_playerMovement == null)
                _playerMovement = other.GetComponent<PlayerMovement>();
        }
        else
        {
            HideHint();

            if (_autoCloseOnExit && _isViewingLetter2)
                SetViewingState(false);
        }
    }

    public void SetInteractionEnabled(bool enabled)
    {
        _interactionEnabled = enabled;

        if (!enabled)
        {
            HideHint();

            if (_isViewingLetter2)
                SetViewingState(false);
        }
    }

    private void SetViewingState(bool viewingLetter2)
    {
        bool wasViewingLetter2 = _isViewingLetter2;
        _isViewingLetter2 = viewingLetter2;

        if (viewingLetter2)
            _hasViewedLetter2Once = true;

        if (_letter1Root != null)
            _letter1Root.SetActive(!viewingLetter2);

        if (_letter2Root != null)
            _letter2Root.SetActive(viewingLetter2);

        if (viewingLetter2)
            AcquireInteractionLock();
        else
            ReleaseInteractionLock();

        bool justClosedLetter2 = wasViewingLetter2 && !viewingLetter2;
        if (justClosedLetter2)
            TryStartEndingSequence();
    }

    private void AcquireInteractionLock()
    {
        if (_holdsInteractionLock) return;

        GlobalInteractionLock.Acquire();
        _holdsInteractionLock = true;
        InventoryUI.Instance?.ShowInteractHint(false);
    }

    private void ReleaseInteractionLock()
    {
        if (!_holdsInteractionLock) return;

        GlobalInteractionLock.Release();
        _holdsInteractionLock = false;
    }

    private void TryStartEndingSequence()
    {
        if (!_triggerEndingAfterLetter2Closed) return;
        if (!_hasViewedLetter2Once) return;
        if (_isEndingSequenceRunning) return;

        if (_playEndingOnlyOnce)
        {
            if (_hasTriggeredEnding) return;
            if (GameManager.Instance != null && GameManager.Instance.GetFlag(_endingTriggeredFlag)) return;
        }

        _endingRoutine = StartCoroutine(EndingSequenceRoutine());
    }

    private IEnumerator EndingSequenceRoutine()
    {
        _isEndingSequenceRunning = true;
        _interactionEnabled = false;
        HideHint();

        LockPlayer();

        if (_playEndingOnlyOnce)
        {
            _hasTriggeredEnding = true;

            if (GameManager.Instance != null && !string.IsNullOrWhiteSpace(_endingTriggeredFlag))
                GameManager.Instance.SetFlag(_endingTriggeredFlag, true);
        }

        if (_dialogueUI != null &&
            _afterLetter2Dialogue != null &&
            !string.IsNullOrWhiteSpace(_afterLetter2Dialogue.content))
        {
            _dialogueUI.ShowLine(_afterLetter2Dialogue, false);

            if (_dialogueDuration > 0f)
                yield return new WaitForSeconds(_dialogueDuration);

            _dialogueUI.Close();
        }

        if (GameManager.Instance != null && !string.IsNullOrWhiteSpace(_endingSceneName))
        {
            if (_useFadeToEndingScene)
                GameManager.Instance.LoadSceneWithFade(_endingSceneName);
            else
                GameManager.Instance.LoadScene(_endingSceneName);
        }
        else
        {
            UnlockPlayer();
            _isEndingSequenceRunning = false;
        }

        _endingRoutine = null;
    }

    private void LockPlayer()
    {
        if (_playerMovement == null && GameManager.Instance != null && GameManager.Instance.CurrentPlayer != null)
            _playerMovement = GameManager.Instance.CurrentPlayer.GetComponent<PlayerMovement>();

        if (_playerMovement != null)
            _playerMovement.SetExternalInputLocked(true);
    }

    private void UnlockPlayer()
    {
        if (_playerMovement != null)
            _playerMovement.SetExternalInputLocked(false);
    }

    private bool IsInCurrentScene()
    {
        return string.IsNullOrWhiteSpace(_currentScene) ||
               SceneManager.GetActiveScene().name == _currentScene;
    }

    private void ShowHint(string hintText)
    {
        if (InventoryUI.Instance == null) return;

        InventoryUI.Instance.ShowInteractHint(true, hintText);
        _hintShownByThisScript = true;
    }

    private void HideHint()
    {
        if (!_hintShownByThisScript) return;
        if (InventoryUI.Instance == null) return;

        InventoryUI.Instance.ShowInteractHint(false);
        _hintShownByThisScript = false;
    }
}
