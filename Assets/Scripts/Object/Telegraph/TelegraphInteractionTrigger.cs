using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TelegraphInteractionTrigger : MonoBehaviour
{
    [System.Serializable]
    private class TelegraphPasswordCanvasContent
    {
        [TextArea(2, 5)]
        public string password;

        public string title;

        [TextArea(2, 8)]
        public string info;
    }

    [Header("Scene")]
    [SerializeField] private string _currentScene = "House";

    [Header("Unlock By Spawn")]
    [SerializeField] private bool _requireUnlockBySpawnPoint = true;
    [SerializeField] private string _unlockWhenSpawnPointId = "hole6_to_house";
    [SerializeField] private string _spawnUnlockedFlag = "house.telegraph.unlocked_from_hole6";
    [SerializeField] private bool _unlockPermanentlyAfterFirstMatch = true;

    [Header("First Interaction")]
    [SerializeField] private WorldInspectable _inspectData;
    [SerializeField] private bool _rememberFirstInspectWithFlag = true;
    [SerializeField] private string _firstInspectFlag = "house.telegraph.first_inspected";

    [Header("Telegraph2")]
    [SerializeField] private GameObject _telegraph2;
    [SerializeField] private TMP_Text _txtCode;
    [SerializeField] private string _idleDisplayText = "";
    [SerializeField] private bool _forceHideTelegraph2OnStart = true;
    [SerializeField] private bool _autoCloseTelegraph2OnExit = true;
    [SerializeField] private bool _clearCodeOnOpen = true;
    [SerializeField] private bool _clearCodeOnClose = true;

    [Header("Result Canvas On Telegraph")]
    [SerializeField] private GameObject _telegraphResultCanvasRoot;
    [SerializeField] private TMP_Text _telegraphResultTitleText;
    [SerializeField] private TMP_Text _telegraphResultInfoText;
    [SerializeField] private bool _forceHideResultCanvasOnStart = true;

    [Header("Code Input")]
    [SerializeField] private int _maxCodeLength = 64;
    [SerializeField] private string _submitActionName = "Submit";
    [SerializeField] private TelegraphPasswordCanvasContent[] _passwordContents = new TelegraphPasswordCanvasContent[0];

    [Header("Disable After Ring Complete")]
    [SerializeField] private bool _disableAfterRingCompleted = true;
    [SerializeField] private string _completedFlag = "house.telegraph.completed";

    [Header("Reveal On Decode")]
    [SerializeField] private GameObject _wineGlass1;
    [SerializeField] private GameObject _wineGlass2;
    [SerializeField] private GameObject _partRevealRoot;
    [SerializeField] private bool _forceWineGlass1VisibleBeforeReveal = true;
    [SerializeField] private bool _forceWineGlass2HiddenBeforeReveal = true;
    [SerializeField] private bool _forcePartHiddenBeforeReveal = true;
    [SerializeField] private bool _rememberRevealState = true;
    [SerializeField] private string _revealDoneFlag = "house.telegraph.decode_revealed";

    [Header("Focus")]
    [SerializeField] private Transform _focusPoint;
    [SerializeField] private int _interactionPriority = 15;

    [Header("Optional")]
    [SerializeField] private bool _lockOtherInteractionsWhileTelegraph2Open = true;
    [SerializeField] private bool _lockPlayerMovementWhileTelegraph2Open = true;

    private readonly Dictionary<string, TelegraphPasswordCanvasContent> _contentLookup =
        new Dictionary<string, TelegraphPasswordCanvasContent>();

    private bool _playerInRange;
    private bool _hintShownByThisScript;
    private bool _holdsInteractionLock;
    private bool _hasSeenFirstInspect;
    private bool _waitingRingCompleteResult;
    private bool _hasAppliedDecodeReveal;
    private bool _isUnlockedBySpawn;
    private bool _hasCompletedInteraction;
    private string _currentCode = string.Empty;
    private string _pendingResultPassword = string.Empty;

    private Transform _playerTransform;
    private PlayerMovement _playerMovement;
    private PlayerItemController _playerItemController;
    private PlayerInput _playerInput;
    private InputAction _submitAction;
    private List<LandLineRinger> _pendingRingers = new List<LandLineRinger>();
    private int _remainingRingCallbacks;

    private void Awake()
    {
        if (_focusPoint == null)
            _focusPoint = transform;

        CacheContentLookup();
    }

    private void OnEnable()
    {
        RefreshPersistentStates();
        HideHint();
        InteractionFocusService.SetCandidate(this, _focusPoint, false, _interactionPriority);
    }

    private void Start()
    {
        RefreshPersistentStates();
        _hasSeenFirstInspect = LoadFirstInspectState();

        if (_forceHideTelegraph2OnStart)
            SetTelegraph2Active(false);

        if (_forceHideResultCanvasOnStart)
            SetResultCanvasActive(false);

        _currentCode = string.Empty;
        UpdateCodeDisplay();
        RefreshRevealVisualState();
        HideHint();
    }

    private void OnDisable()
    {
        UnsubscribePendingRingers();

        HideHint();
        ForceCloseTelegraph2();
        InteractionFocusService.RemoveCandidate(this);

        _playerInRange = false;
        _playerTransform = null;
        _playerMovement = null;
        _playerItemController = null;
        _playerInput = null;
        _submitAction = null;
        _waitingRingCompleteResult = false;
        _pendingResultPassword = string.Empty;
        _remainingRingCallbacks = 0;
    }

    private void Update()
    {
        if (HandleResultCanvasCloseInput())
            return;

        bool inCurrentScene = IsInCurrentScene();
        bool isLoadingScene = GameManager.Instance != null && GameManager.Instance.IsLoadingScene();

        if (!inCurrentScene || isLoadingScene)
        {
            HideHint();

            if (_autoCloseTelegraph2OnExit && IsTelegraph2Active())
                ForceCloseTelegraph2();

            InteractionFocusService.SetCandidate(this, _focusPoint, false, _interactionPriority);
            return;
        }

        RefreshPersistentStates();

        if (!CanInteractTelegraph())
        {
            HideHint();

            if (_autoCloseTelegraph2OnExit && IsTelegraph2Active())
                ForceCloseTelegraph2();

            InteractionFocusService.SetCandidate(this, _focusPoint, false, _interactionPriority);
            return;
        }

        InteractionFocusService.SetCandidate(this, _focusPoint, _playerInRange, _interactionPriority);

        bool popupOpen = InventoryUI.Instance != null && InventoryUI.Instance.IsPopupOpen;
        bool resultCanvasOpen = IsResultCanvasActive();
        bool interactionAvailable = !GlobalInteractionLock.IsLocked || _holdsInteractionLock;

        bool hasFocus =
            _playerInRange &&
            _playerTransform != null &&
            interactionAvailable &&
            InteractionFocusService.HasFocus(this, _playerTransform.position);

        RefreshHint(hasFocus, popupOpen, resultCanvasOpen);
        HandleFInput(hasFocus, popupOpen, resultCanvasOpen);

        if (IsTelegraph2Active() && !_waitingRingCompleteResult)
            HandleCodeInput();
    }

    private void RefreshPersistentStates()
    {
        _isUnlockedBySpawn = ResolveSpawnUnlockState();
        _hasCompletedInteraction = LoadCompletedState();
    }

    private bool CanInteractTelegraph()
    {
        if (_disableAfterRingCompleted && _hasCompletedInteraction)
            return false;

        if (_requireUnlockBySpawnPoint && !_isUnlockedBySpawn)
            return false;

        return true;
    }

    private bool HandleResultCanvasCloseInput()
    {
        if (!IsResultCanvasActive())
            return false;

        if (Keyboard.current == null)
            return false;

        if (!Keyboard.current.fKey.wasPressedThisFrame)
            return false;

        SetResultCanvasActive(false);
        return true;
    }

    private void HandleFInput(bool hasFocus, bool popupOpen, bool resultCanvasOpen)
    {
        if (!hasFocus) return;
        if (Keyboard.current == null) return;
        if (!Keyboard.current.fKey.wasPressedThisFrame) return;
        if (popupOpen) return;
        if (resultCanvasOpen) return;
        if (_waitingRingCompleteResult) return;

        if (!IsTelegraph2Active())
        {
            if (!_hasSeenFirstInspect)
                OpenFirstInspect();
            else
                OpenTelegraph2();
        }
        else
        {
            CloseTelegraph2();
        }
    }

    private void HandleCodeInput()
    {
        if (Keyboard.current == null) return;

        bool changed = false;

        if (WasDotPressed())
        {
            AppendCharacter('.');
            changed = true;
        }

        if (WasDashPressed())
        {
            AppendCharacter('-');
            changed = true;
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            AppendSpace();
            changed = true;
        }

        if (Keyboard.current.backspaceKey.wasPressedThisFrame && _currentCode.Length > 0)
        {
            _currentCode = _currentCode.Substring(0, _currentCode.Length - 1);
            changed = true;
        }

        if ((Keyboard.current.deleteKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame) &&
            _currentCode.Length > 0)
        {
            _currentCode = string.Empty;
            changed = true;
        }

        if (WasSubmitPressed())
        {
            SubmitCurrentCode();
            return;
        }

        if (changed)
            UpdateCodeDisplay();
    }

    private bool WasDotPressed()
    {
        return Keyboard.current.digit1Key.wasPressedThisFrame ||
               Keyboard.current.numpad1Key.wasPressedThisFrame;
    }

    private bool WasDashPressed()
    {
        return Keyboard.current.digit2Key.wasPressedThisFrame ||
               Keyboard.current.numpad2Key.wasPressedThisFrame;
    }

    private bool WasSubmitPressed()
    {
        ResolveSubmitAction();

        if (_submitAction != null && _submitAction.WasPressedThisFrame())
            return true;

        return Keyboard.current != null &&
               (Keyboard.current.enterKey.wasPressedThisFrame ||
                Keyboard.current.numpadEnterKey.wasPressedThisFrame);
    }

    private void ResolveSubmitAction()
    {
        if (_submitAction != null)
            return;

        if (_playerInput == null && _playerTransform != null)
        {
            _playerInput = _playerTransform.GetComponent<PlayerInput>();
            if (_playerInput == null)
                _playerInput = _playerTransform.GetComponentInParent<PlayerInput>();
        }

        if (_playerInput == null || _playerInput.actions == null)
            return;

        if (!string.IsNullOrWhiteSpace(_submitActionName))
            _submitAction = _playerInput.actions.FindAction(_submitActionName, false);

        if (_submitAction == null)
            _submitAction = _playerInput.actions.FindAction("UI/Submit", false);
    }

    private void AppendCharacter(char value)
    {
        _currentCode += value;

        if (_maxCodeLength > 0 && _currentCode.Length > _maxCodeLength)
            _currentCode = _currentCode.Substring(_currentCode.Length - _maxCodeLength);
    }

    private void AppendSpace()
    {
        if (string.IsNullOrEmpty(_currentCode))
            return;

        if (_currentCode[_currentCode.Length - 1] == ' ')
            return;

        AppendCharacter(' ');
    }

    private void SubmitCurrentCode()
    {
        string normalizedInput = NormalizePassword(_currentCode);
        if (string.IsNullOrEmpty(normalizedInput))
            return;

        if (!_contentLookup.ContainsKey(normalizedInput))
            return;

        List<LandLineRinger> started = LandLineRinger.TriggerAllRings();
        if (started == null || started.Count == 0)
            return;

        ApplyDecodeRevealIfNeeded();

        _pendingResultPassword = normalizedInput;
        _waitingRingCompleteResult = true;

        UnsubscribePendingRingers();
        _pendingRingers = started;
        _remainingRingCallbacks = started.Count;

        for (int i = 0; i < started.Count; i++)
        {
            if (started[i] != null)
                started[i].RingCompleted += HandleRingCompleted;
        }

        CloseTelegraph2();
    }

    private void HandleRingCompleted(LandLineRinger ringer)
    {
        if (ringer != null)
            ringer.RingCompleted -= HandleRingCompleted;

        _remainingRingCallbacks--;

        if (_remainingRingCallbacks > 0)
            return;

        _waitingRingCompleteResult = false;
        MarkCompletedIfNeeded();
        ShowResultCanvas(_pendingResultPassword);
        _pendingResultPassword = string.Empty;
        _pendingRingers.Clear();
    }

    private void MarkCompletedIfNeeded()
    {
        if (!_disableAfterRingCompleted)
            return;

        if (_hasCompletedInteraction)
            return;

        _hasCompletedInteraction = true;
        SaveCompletedState();
        HideHint();
    }

    private void ShowResultCanvas(string normalizedPassword)
    {
        if (!_contentLookup.TryGetValue(normalizedPassword, out TelegraphPasswordCanvasContent content))
            return;

        if (_telegraphResultTitleText != null)
            _telegraphResultTitleText.text = content.title;

        if (_telegraphResultInfoText != null)
            _telegraphResultInfoText.text = content.info;

        SetResultCanvasActive(true);
    }

    private void UnsubscribePendingRingers()
    {
        if (_pendingRingers == null) return;

        for (int i = 0; i < _pendingRingers.Count; i++)
        {
            if (_pendingRingers[i] != null)
                _pendingRingers[i].RingCompleted -= HandleRingCompleted;
        }

        _pendingRingers.Clear();
        _remainingRingCallbacks = 0;
    }

    private void OpenFirstInspect()
    {
        _hasSeenFirstInspect = true;
        SaveFirstInspectState();

        if (_inspectData == null || InventoryUI.Instance == null || _playerItemController == null)
        {
            OpenTelegraph2();
            return;
        }

        if (_playerMovement != null)
            _playerMovement.SetExternalInputLocked(true);

        InventoryUI.Instance.OpenInspectPopup(
            _inspectData.Title,
            _inspectData.Description,
            _playerItemController
        );

        HideHint();
    }

    private void OpenTelegraph2()
    {
        if (IsTelegraph2Active()) return;

        if (_clearCodeOnOpen)
            _currentCode = string.Empty;

        SetResultCanvasActive(false);
        UpdateCodeDisplay();
        SetTelegraph2Active(true);
        AcquireInteractionLock();
        LockPlayerMovement();
        HideHint();
    }

    private void CloseTelegraph2()
    {
        if (!IsTelegraph2Active())
        {
            ReleaseInteractionLock();
            UnlockPlayerMovement();
            return;
        }

        SetTelegraph2Active(false);
        ReleaseInteractionLock();
        UnlockPlayerMovement();

        if (_clearCodeOnClose)
        {
            _currentCode = string.Empty;
            UpdateCodeDisplay();
        }
    }

    private void ForceCloseTelegraph2()
    {
        SetTelegraph2Active(false);
        ReleaseInteractionLock();
        UnlockPlayerMovement();

        if (_clearCodeOnClose)
        {
            _currentCode = string.Empty;
            UpdateCodeDisplay();
        }
    }

    private void RefreshHint(bool hasFocus, bool popupOpen, bool resultCanvasOpen)
    {
        bool shouldShow =
            hasFocus &&
            !popupOpen &&
            !resultCanvasOpen &&
            !IsTelegraph2Active() &&
            !_waitingRingCompleteResult;

        if (shouldShow)
            ShowHint();
        else
            HideHint();
    }

    private void OnTriggerEnter2D(Collider2D other) => UpdatePlayerState(other, true);
    private void OnTriggerStay2D(Collider2D other) => UpdatePlayerState(other, true);
    private void OnTriggerExit2D(Collider2D other) => UpdatePlayerState(other, false);

    private void UpdatePlayerState(Collider2D other, bool inRange)
    {
        if (other == null || !other.CompareTag("Player")) return;
        if (!IsInCurrentScene()) return;

        _playerInRange = inRange;

        if (inRange)
        {
            _playerTransform = other.transform;

            if (_playerMovement == null)
            {
                _playerMovement = other.GetComponent<PlayerMovement>();
                if (_playerMovement == null)
                    _playerMovement = other.GetComponentInParent<PlayerMovement>();
            }

            if (_playerItemController == null)
            {
                _playerItemController = other.GetComponent<PlayerItemController>();
                if (_playerItemController == null)
                    _playerItemController = other.GetComponentInParent<PlayerItemController>();
            }

            if (_playerInput == null)
            {
                _playerInput = other.GetComponent<PlayerInput>();
                if (_playerInput == null)
                    _playerInput = other.GetComponentInParent<PlayerInput>();
            }

            if (_submitAction == null)
                ResolveSubmitAction();
        }
        else
        {
            HideHint();

            if (_autoCloseTelegraph2OnExit && IsTelegraph2Active())
                CloseTelegraph2();
        }
    }

    private void UpdateCodeDisplay()
    {
        if (_txtCode == null) return;
        _txtCode.text = string.IsNullOrEmpty(_currentCode) ? _idleDisplayText : _currentCode;
    }

    private void SetTelegraph2Active(bool active)
    {
        if (_telegraph2 != null)
            _telegraph2.SetActive(active);
    }

    private bool IsTelegraph2Active()
    {
        return _telegraph2 != null && _telegraph2.activeSelf;
    }

    private void SetResultCanvasActive(bool active)
    {
        if (_telegraphResultCanvasRoot != null)
            _telegraphResultCanvasRoot.SetActive(active);
    }

    private bool IsResultCanvasActive()
    {
        return _telegraphResultCanvasRoot != null && _telegraphResultCanvasRoot.activeSelf;
    }

    private void AcquireInteractionLock()
    {
        if (!_lockOtherInteractionsWhileTelegraph2Open) return;
        if (_holdsInteractionLock) return;

        GlobalInteractionLock.Acquire();
        _holdsInteractionLock = true;

        if (InventoryUI.Instance != null)
            InventoryUI.Instance.ShowInteractHint(false);
    }

    private void ReleaseInteractionLock()
    {
        if (!_holdsInteractionLock) return;

        GlobalInteractionLock.Release();
        _holdsInteractionLock = false;
    }

    private void LockPlayerMovement()
    {
        if (!_lockPlayerMovementWhileTelegraph2Open) return;
        if (_playerMovement == null) return;

        _playerMovement.SetExternalInputLocked(true);
    }

    private void UnlockPlayerMovement()
    {
        if (!_lockPlayerMovementWhileTelegraph2Open) return;
        if (_playerMovement == null) return;

        _playerMovement.SetExternalInputLocked(false);
    }

    private bool IsInCurrentScene()
    {
        return string.IsNullOrWhiteSpace(_currentScene) ||
               SceneManager.GetActiveScene().name == _currentScene;
    }

    private void CacheContentLookup()
    {
        _contentLookup.Clear();

        if (_passwordContents == null) return;

        for (int i = 0; i < _passwordContents.Length; i++)
        {
            TelegraphPasswordCanvasContent item = _passwordContents[i];
            if (item == null) continue;

            string normalized = NormalizePassword(item.password);
            if (string.IsNullOrEmpty(normalized)) continue;
            if (_contentLookup.ContainsKey(normalized)) continue;

            _contentLookup.Add(normalized, item);
        }
    }

    private string NormalizePassword(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return string.Empty;

        StringBuilder sb = new StringBuilder(raw.Length);

        for (int i = 0; i < raw.Length; i++)
        {
            char c = raw[i];

            if (c == '.' || c == '。' || c == '·' || c == '1')
                sb.Append('.');
            else if (c == '-' || c == '—' || c == '_' || c == '2')
                sb.Append('-');
        }

        return sb.ToString();
    }

    private bool ResolveSpawnUnlockState()
    {
        if (!_requireUnlockBySpawnPoint)
            return true;

        if (GameManager.Instance == null)
            return false;

        if (_unlockPermanentlyAfterFirstMatch &&
            !string.IsNullOrWhiteSpace(_spawnUnlockedFlag) &&
            GameManager.Instance.GetFlag(_spawnUnlockedFlag))
        {
            return true;
        }

        bool cameFromRequiredSpawn =
            !string.IsNullOrWhiteSpace(_unlockWhenSpawnPointId) &&
            GameManager.Instance.LastSpawnPointId == _unlockWhenSpawnPointId;

        if (!cameFromRequiredSpawn)
            return false;

        if (_unlockPermanentlyAfterFirstMatch && !string.IsNullOrWhiteSpace(_spawnUnlockedFlag))
            GameManager.Instance.SetFlag(_spawnUnlockedFlag, true);

        return true;
    }

    private bool LoadFirstInspectState()
    {
        if (!_rememberFirstInspectWithFlag) return false;
        if (GameManager.Instance == null) return false;
        if (string.IsNullOrWhiteSpace(_firstInspectFlag)) return false;

        return GameManager.Instance.GetFlag(_firstInspectFlag);
    }

    private void SaveFirstInspectState()
    {
        if (!_rememberFirstInspectWithFlag) return;
        if (GameManager.Instance == null) return;
        if (string.IsNullOrWhiteSpace(_firstInspectFlag)) return;

        GameManager.Instance.SetFlag(_firstInspectFlag, true);
    }

    private bool LoadCompletedState()
    {
        if (!_disableAfterRingCompleted) return false;
        if (GameManager.Instance == null) return false;
        if (string.IsNullOrWhiteSpace(_completedFlag)) return false;

        return GameManager.Instance.GetFlag(_completedFlag);
    }

    private void SaveCompletedState()
    {
        if (!_disableAfterRingCompleted) return;
        if (GameManager.Instance == null) return;
        if (string.IsNullOrWhiteSpace(_completedFlag)) return;

        GameManager.Instance.SetFlag(_completedFlag, true);
    }

    private void RefreshRevealVisualState()
    {
        bool alreadyRevealed = LoadRevealState();

        if (alreadyRevealed)
        {
            ApplyRevealStateImmediate();
            _hasAppliedDecodeReveal = true;
            return;
        }

        _hasAppliedDecodeReveal = false;

        if (_forceWineGlass1VisibleBeforeReveal && _wineGlass1 != null)
            _wineGlass1.SetActive(true);

        if (_forceWineGlass2HiddenBeforeReveal && _wineGlass2 != null)
            _wineGlass2.SetActive(false);

        if (_forcePartHiddenBeforeReveal && _partRevealRoot != null)
            _partRevealRoot.SetActive(false);
    }

    private void ApplyDecodeRevealIfNeeded()
    {
        if (_hasAppliedDecodeReveal)
            return;

        ApplyRevealStateImmediate();
        SaveRevealState();
        _hasAppliedDecodeReveal = true;
    }

    private void ApplyRevealStateImmediate()
    {
        if (_wineGlass1 != null)
            _wineGlass1.SetActive(false);

        if (_wineGlass2 != null)
            _wineGlass2.SetActive(true);

        if (_partRevealRoot != null)
            _partRevealRoot.SetActive(true);
    }

    private bool LoadRevealState()
    {
        if (!_rememberRevealState) return false;
        if (GameManager.Instance == null) return false;
        if (string.IsNullOrWhiteSpace(_revealDoneFlag)) return false;

        return GameManager.Instance.GetFlag(_revealDoneFlag);
    }

    private void SaveRevealState()
    {
        if (!_rememberRevealState) return;
        if (GameManager.Instance == null) return;
        if (string.IsNullOrWhiteSpace(_revealDoneFlag)) return;

        GameManager.Instance.SetFlag(_revealDoneFlag, true);
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
}
