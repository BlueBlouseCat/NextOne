using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PaperBallToggleInteractable : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string _currentScene = "House";

    [Header("Targets")]
    [SerializeField] private GameObject _paperBallRoot;
    [SerializeField] private GameObject _paperRoot;

    [Header("Focus")]
    [SerializeField] private Transform _focusPoint;
    [SerializeField] private int _interactionPriority = 20;

    [Header("Optional")]
    [SerializeField] private bool _forceHidePaperOnStart = true;
    [SerializeField] private bool _autoClosePaperOnExit = false;
    [SerializeField] private bool _lockOtherInteractionsWhilePaperOpen = true;

    private bool _playerInRange;
    private bool _hintShownByThisScript;
    private Transform _playerTransform;
    private bool _isFocusedByThisScript;
    private bool _paperShown;
    private bool _holdsInteractionLock;

    private void Awake()
    {
        if (_focusPoint == null)
            _focusPoint = transform;
    }

    private void OnEnable()
    {
        InteractionFocusService.SetCandidate(this, _focusPoint, false, _interactionPriority);
        HideHint();
    }

    private void Start()
    {
        if (_forceHidePaperOnStart)
        {
            _paperShown = false;
            ApplyVisualState();
        }
        else
        {
            _paperShown = _paperRoot != null && _paperRoot.activeSelf;
            ApplyVisualState();
        }
    }

    private void OnDisable()
    {
        HideHint();
        ReleaseInteractionLock();
        InteractionFocusService.RemoveCandidate(this);

        _playerInRange = false;
        _playerTransform = null;
        _isFocusedByThisScript = false;
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name != _currentScene)
        {
            HideHint();
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.IsLoadingScene())
        {
            HideHint();
            return;
        }

        InteractionFocusService.SetCandidate(this, _focusPoint, _playerInRange, _interactionPriority);

        bool interactionAvailable = !GlobalInteractionLock.IsLocked || _holdsInteractionLock;

        bool hasFocus =
            _playerInRange &&
            _playerTransform != null &&
            interactionAvailable &&
            InteractionFocusService.HasFocus(this, _playerTransform.position);

        ApplyFocus(hasFocus);

        if (!hasFocus) return;
        if (Keyboard.current == null) return;
        if (!Keyboard.current.fKey.wasPressedThisFrame) return;

        if (InventoryUI.Instance != null &&
            InventoryUI.Instance.IsPopupOpen &&
            !_paperShown)
        {
            return;
        }

        TogglePaper();
    }

    private void OnTriggerEnter2D(Collider2D other) => SetPlayerInRange(other, true);
    private void OnTriggerStay2D(Collider2D other) => SetPlayerInRange(other, true);
    private void OnTriggerExit2D(Collider2D other) => SetPlayerInRange(other, false);

    private void OnCollisionEnter2D(Collision2D collision) => SetPlayerInRange(collision.collider, true);
    private void OnCollisionStay2D(Collision2D collision) => SetPlayerInRange(collision.collider, true);
    private void OnCollisionExit2D(Collision2D collision) => SetPlayerInRange(collision.collider, false);

    private void SetPlayerInRange(Collider2D other, bool inRange)
    {
        if (other == null || !other.CompareTag("Player")) return;
        if (SceneManager.GetActiveScene().name != _currentScene) return;

        _playerInRange = inRange;

        if (inRange)
        {
            _playerTransform = other.transform;
        }
        else
        {
            HideHint();

            if (_autoClosePaperOnExit && _paperShown)
            {
                _paperShown = false;
                ApplyVisualState();
                ReleaseInteractionLock();
            }
        }
    }

    private void ApplyFocus(bool focused)
    {
        if (_isFocusedByThisScript == focused) return;

        _isFocusedByThisScript = focused;

        if (focused)
            ShowHint();
        else
            HideHint();
    }

    private void TogglePaper()
    {
        _paperShown = !_paperShown;

        if (_paperShown)
            AcquireInteractionLock();
        else
            ReleaseInteractionLock();

        ApplyVisualState();
    }

    private void ApplyVisualState()
    {
        if (_paperBallRoot != null)
            _paperBallRoot.SetActive(!_paperShown);

        if (_paperRoot != null)
            _paperRoot.SetActive(_paperShown);
    }

    private void AcquireInteractionLock()
    {
        if (!_lockOtherInteractionsWhilePaperOpen) return;
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
