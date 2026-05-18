using UnityEngine;
using UnityEngine.SceneManagement;

public class LiangyilouDialogueTrigger : MonoBehaviour
{
    private enum SequenceState
    {
        Idle,
        WaitingBounceStart,
        WaitingBounceLanding,
        Appearing,
        WaitingForInteract,
        DialogueRunning,
        Disappearing,
        WaitingForExit,
        Finished
    }

    [Header("Scene")]
    [SerializeField] private string _currentScene = SceneName.Scene1; // OutsideOfHouse

    [Header("Trigger Colliders")]
    [SerializeField] private Collider2D _liangyiganTrigger;
    [SerializeField] private Collider2D _liangyilouTrigger;

    [Header("References")]
    [SerializeField] private LiangyilouSpineController _liangyilouSpine;
    [SerializeField] private DialogueUI _dialogueUI;

    [Header("Dialogue")]
    [SerializeField] private DialogueLine[] _lines;

    [Header("Bounce Detection")]
    [SerializeField] private float _launchDetectVelocityY = 0.5f;
    [SerializeField] private float _launchDetectHeight = 0.15f;
    [SerializeField] private float _landingVelocityThreshold = 0.05f;
    [SerializeField] private float _landingHeightTolerance = 0.35f;
    [SerializeField] private float _appearDelayAfterBounce = 0.1f;

    [Header("Optional")]
    [SerializeField] private bool _playOnlyOnce = true;
    [SerializeField] private string _playedFlag = "liangyilou_dialogue_done";

    private Transform _player;
    private PlayerMovement _playerMovement;
    private Collider2D _playerCollider;
    private Rigidbody2D _playerRigidbody;

    private SequenceState _state;
    private bool _hasPlayed;
    private bool _hintShownByThisScript;
    private bool _ignoreInteractUntilRelease;
    private int _currentLineIndex;

    private float _bounceStartY;
    private float _bounceStartedTime;
    private float _readyToAppearTime = -1f;

    private void Awake()
    {
        if (_liangyilouSpine == null)
            _liangyilouSpine = GetComponentInChildren<LiangyilouSpineController>();

        if (_dialogueUI == null)
            _dialogueUI = FindObjectOfType<DialogueUI>();
    }

    private void OnEnable()
    {
        _hasPlayed = GameManager.Instance != null && GameManager.Instance.GetFlag(_playedFlag);
        _state = (_playOnlyOnce && _hasPlayed) ? SequenceState.Finished : SequenceState.Idle;

        _ignoreInteractUntilRelease = false;
        _currentLineIndex = 0;
        _readyToAppearTime = -1f;

        HideHint();
        CloseDialogueUI();
        UnlockPlayer();

        _liangyilouSpine?.ResetToIdleLou();
    }

    private void OnDisable()
    {
        HideHint();
        CloseDialogueUI();
        UnlockPlayer();
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name != _currentScene)
        {
            HideHint();
            return;
        }

        if (GameManager.Instance == null || GameManager.Instance.IsLoadingScene())
        {
            HideHint();
            return;
        }

        if (_playOnlyOnce && !_hasPlayed)
            _hasPlayed = GameManager.Instance.GetFlag(_playedFlag);

        if (_playOnlyOnce && _hasPlayed)
        {
            _state = SequenceState.Finished;
            HideHint();
            return;
        }

        ResolvePlayerReferences();
        if (_player == null || _playerCollider == null)
        {
            HideHint();
            return;
        }

        switch (_state)
        {
            case SequenceState.Idle:
                UpdateIdle();
                break;

            case SequenceState.WaitingBounceStart:
                UpdateWaitingBounceStart();
                break;

            case SequenceState.WaitingBounceLanding:
                UpdateWaitingBounceLanding();
                break;

            case SequenceState.Appearing:
            case SequenceState.Disappearing:
                HideHint();
                break;

            case SequenceState.WaitingForInteract:
                UpdateWaitingForInteract();
                break;

            case SequenceState.DialogueRunning:
                UpdateDialogueRunning();
                break;

            case SequenceState.WaitingForExit:
                HideHint();

                if (!IsTouchingPlayer(_liangyiganTrigger) && !IsTouchingPlayer(_liangyilouTrigger))
                    _state = SequenceState.Idle;
                break;

            case SequenceState.Finished:
                HideHint();
                break;
        }
    }

    private void UpdateIdle()
    {
        if (IsTouchingPlayer(_liangyiganTrigger))
        {
            BeginAppearSequence();
            return;
        }

        if (IsTouchingPlayer(_liangyilouTrigger))
            BeginBounceSequence();
    }

    private void BeginBounceSequence()
    {
        _state = SequenceState.WaitingBounceStart;
        _bounceStartY = _player.position.y;
        _bounceStartedTime = 0f;
        _readyToAppearTime = -1f;

        HideHint();
    }

    private void UpdateWaitingBounceStart()
    {
        if (HasBounceStarted())
        {
            _state = SequenceState.WaitingBounceLanding;
            _bounceStartedTime = Time.time;
            return;
        }

        if (!IsTouchingPlayer(_liangyilouTrigger))
            _state = SequenceState.Idle;
    }

    private void UpdateWaitingBounceLanding()
    {
        if (_player == null || _playerRigidbody == null)
            return;

        if (Time.time - _bounceStartedTime < 0.05f)
            return;

        if (IsPlayerSettledAfterBounce())
        {
            if (_readyToAppearTime < 0f)
                _readyToAppearTime = Time.time + _appearDelayAfterBounce;

            if (Time.time >= _readyToAppearTime)
                BeginAppearSequence();

            return;
        }

        _readyToAppearTime = -1f;
    }

    private bool HasBounceStarted()
    {
        if (_player == null || _playerRigidbody == null)
            return false;

        if (_playerRigidbody.velocity.y > _launchDetectVelocityY)
            return true;

        if (_player.position.y >= _bounceStartY + _launchDetectHeight)
            return true;

        return false;
    }

    private bool IsPlayerSettledAfterBounce()
    {
        if (_player == null || _playerRigidbody == null)
            return false;

        float velocityY = Mathf.Abs(_playerRigidbody.velocity.y);
        bool lowEnough = _player.position.y <= _bounceStartY + _landingHeightTolerance;
        bool slowEnough = velocityY <= _landingVelocityThreshold;

        return lowEnough && slowEnough;
    }

    private void BeginAppearSequence()
    {
        if (_state == SequenceState.Appearing ||
            _state == SequenceState.WaitingForInteract ||
            _state == SequenceState.DialogueRunning ||
            _state == SequenceState.Disappearing ||
            _state == SequenceState.Finished)
        {
            return;
        }

        _state = SequenceState.Appearing;
        _ignoreInteractUntilRelease = false;
        _readyToAppearTime = -1f;

        HideHint();
        LockPlayer();

        if (_liangyilouSpine != null)
            _liangyilouSpine.PlayAppearThenIdleCat(OnAppearFinished);
        else
            OnAppearFinished();
    }

    private void OnAppearFinished()
    {
        if (_state != SequenceState.Appearing)
            return;

        _state = SequenceState.WaitingForInteract;
        _ignoreInteractUntilRelease = GameplayInputUtil.InteractHeld();

        ShowHint();
    }

    private void UpdateWaitingForInteract()
    {
        ShowHint();

        if (_ignoreInteractUntilRelease)
        {
            if (!GameplayInputUtil.InteractHeld())
                _ignoreInteractUntilRelease = false;

            return;
        }

        if (!GameplayInputUtil.InteractPressedThisFrame())
            return;

        StartDialogue();
    }

    private void StartDialogue()
    {
        if (_dialogueUI == null)
        {
            ShowHint();
            return;
        }

        if (_lines == null || _lines.Length == 0)
        {
            ShowHint();
            return;
        }

        HideHint();

        _state = SequenceState.DialogueRunning;
        _currentLineIndex = 0;
        _ignoreInteractUntilRelease = true;

        _liangyilouSpine?.PlayTalk();
        ShowCurrentLine();
    }

    private void UpdateDialogueRunning()
    {
        HideHint();

        if (_ignoreInteractUntilRelease)
        {
            if (!GameplayInputUtil.InteractHeld())
                _ignoreInteractUntilRelease = false;

            return;
        }

        if (!GameplayInputUtil.InteractPressedThisFrame())
            return;

        AdvanceDialogue();
    }

    private void AdvanceDialogue()
    {
        _currentLineIndex++;

        if (_lines == null || _currentLineIndex >= _lines.Length)
        {
            StartDisappearSequence();
            return;
        }

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        if (_dialogueUI == null || _lines == null)
            return;

        if (_currentLineIndex < 0 || _currentLineIndex >= _lines.Length)
            return;

        _liangyilouSpine?.PlayTalk();
        _dialogueUI.ShowLine(_lines[_currentLineIndex]);
    }

    private void StartDisappearSequence()
    {
        _currentLineIndex = 0;
        _ignoreInteractUntilRelease = false;

        CloseDialogueUI();
        HideHint();

        _state = SequenceState.Disappearing;

        if (_liangyilouSpine != null)
            _liangyilouSpine.PlayDisappearThenIdleLou(OnDisappearFinished);
        else
            OnDisappearFinished();
    }

    private void OnDisappearFinished()
    {
        CloseDialogueUI();
        HideHint();
        UnlockPlayer();

        if (_playOnlyOnce)
        {
            _hasPlayed = true;
            _state = SequenceState.Finished;

            if (GameManager.Instance != null && !string.IsNullOrWhiteSpace(_playedFlag))
                GameManager.Instance.SetFlag(_playedFlag, true);
        }
        else
        {
            _state = SequenceState.WaitingForExit;
        }
    }

    private void ResolvePlayerReferences()
    {
        Transform currentPlayer = GameManager.Instance != null ? GameManager.Instance.CurrentPlayer : null;

        if (currentPlayer == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                currentPlayer = playerObject.transform;
        }

        if (currentPlayer == null)
        {
            _player = null;
            _playerMovement = null;
            _playerCollider = null;
            _playerRigidbody = null;
            return;
        }

        if (_player == currentPlayer && _playerMovement != null && _playerCollider != null && _playerRigidbody != null)
            return;

        _player = currentPlayer;

        _playerMovement = _player.GetComponent<PlayerMovement>();
        if (_playerMovement == null)
            _playerMovement = _player.GetComponentInChildren<PlayerMovement>();
        if (_playerMovement == null)
            _playerMovement = _player.GetComponentInParent<PlayerMovement>();

        _playerCollider = _player.GetComponent<Collider2D>();
        if (_playerCollider == null)
            _playerCollider = _player.GetComponentInChildren<Collider2D>();

        _playerRigidbody = _player.GetComponent<Rigidbody2D>();
        if (_playerRigidbody == null)
            _playerRigidbody = _player.GetComponentInChildren<Rigidbody2D>();
    }

    private bool IsTouchingPlayer(Collider2D triggerCollider)
    {
        if (triggerCollider == null || _playerCollider == null)
            return false;

        if (!triggerCollider.enabled || !_playerCollider.enabled)
            return false;

        if (!triggerCollider.gameObject.activeInHierarchy || !_playerCollider.gameObject.activeInHierarchy)
            return false;

        ColliderDistance2D distance = triggerCollider.Distance(_playerCollider);
        if (distance.isOverlapped)
            return true;

        return triggerCollider.bounds.Intersects(_playerCollider.bounds);
    }

    private void ShowHint()
    {
        if (InventoryUI.Instance == null)
            return;

        InventoryUI.Instance.ShowInteractHint(true, ProjectInteractionHints.Interact);
        _hintShownByThisScript = true;
    }

    private void HideHint()
    {
        if (!_hintShownByThisScript)
            return;

        if (InventoryUI.Instance != null)
            InventoryUI.Instance.ShowInteractHint(false);

        _hintShownByThisScript = false;
    }

    private void LockPlayer()
    {
        if (_playerMovement == null)
            return;

        _playerMovement.SetExternalInputLocked(true);
        _playerMovement.SetJumpInputLocked(true);
    }

    private void UnlockPlayer()
    {
        if (_playerMovement == null)
            return;

        _playerMovement.SetExternalInputLocked(false);
        _playerMovement.SetJumpInputLocked(false);
    }

    private void CloseDialogueUI()
    {
        if (_dialogueUI != null)
            _dialogueUI.Close();
    }
}
