using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class RopeSwapInteractionTrigger : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string _currentScene = "House";

    [Header("Targets")]
    [SerializeField] private GameObject _rope1;
    [SerializeField] private GameObject _rope2;

    [Header("Focus")]
    [SerializeField] private Transform _focusPoint;
    [SerializeField] private int _interactionPriority = 20;

    [Header("Optional")]
    [SerializeField] private bool _forceRope1ActiveOnStart = true;
    [SerializeField] private bool _forceRope2InactiveOnStart = true;
    [SerializeField] private bool _disableTriggerAfterSwap = true;
    [SerializeField] private bool _swapOnlyOnce = true;
    [SerializeField] private string _swapDoneFlag = "house_rope_swapped";

    private bool _playerInRange;
    private bool _hintShownByThisScript;
    private bool _hasSwapped;
    private Transform _playerTransform;

    private void Awake()
    {
        if (_focusPoint == null)
            _focusPoint = transform;
    }

    private void OnEnable()
    {
        HideHint();
        InteractionFocusService.SetCandidate(this, _focusPoint, false, _interactionPriority);
        RefreshInitialState();
    }

    private void Start()
    {
        RefreshInitialState();
    }

    private void OnDisable()
    {
        HideHint();
        InteractionFocusService.RemoveCandidate(this);

        _playerInRange = false;
        _playerTransform = null;
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

        if (_swapOnlyOnce && !_hasSwapped && GameManager.Instance != null && GameManager.Instance.GetFlag(_swapDoneFlag))
        {
            ApplySwappedState();
        }

        InteractionFocusService.SetCandidate(this, _focusPoint, _playerInRange && !_hasSwapped, _interactionPriority);

        bool hasFocus =
            !_hasSwapped &&
            _playerInRange &&
            _playerTransform != null &&
            !GlobalInteractionLock.IsLocked &&
            InteractionFocusService.HasFocus(this, _playerTransform.position);

        if (hasFocus)
            ShowHint();
        else
            HideHint();

        if (!hasFocus) return;
        if (!GameplayInputUtil.InteractPressedThisFrame()) return;

        SwapRope();
    }

    private void OnTriggerEnter2D(Collider2D other) => UpdatePlayerRange(other, true);
    private void OnTriggerStay2D(Collider2D other) => UpdatePlayerRange(other, true);
    private void OnTriggerExit2D(Collider2D other) => UpdatePlayerRange(other, false);

    private void UpdatePlayerRange(Collider2D other, bool inRange)
    {
        if (other == null || !other.CompareTag("Player")) return;
        if (!IsInCurrentScene()) return;
        if (_hasSwapped) return;

        _playerInRange = inRange;

        if (inRange)
        {
            _playerTransform = other.transform;
        }
        else
        {
            HideHint();
        }
    }

    private void RefreshInitialState()
    {
        bool alreadySwapped =
            _swapOnlyOnce &&
            GameManager.Instance != null &&
            GameManager.Instance.GetFlag(_swapDoneFlag);

        if (alreadySwapped)
        {
            ApplySwappedState();
            return;
        }

        _hasSwapped = false;

        if (_forceRope1ActiveOnStart && _rope1 != null)
            _rope1.SetActive(true);

        if (_forceRope2InactiveOnStart && _rope2 != null)
            _rope2.SetActive(false);

        if (_disableTriggerAfterSwap && !gameObject.activeSelf)
            gameObject.SetActive(true);
    }

    private void SwapRope()
    {
        if (_hasSwapped) return;

        _hasSwapped = true;

        if (_rope1 != null)
            _rope1.SetActive(false);

        if (_rope2 != null)
            _rope2.SetActive(true);

        HideHint();

        if (_swapOnlyOnce && GameManager.Instance != null && !string.IsNullOrWhiteSpace(_swapDoneFlag))
            GameManager.Instance.SetFlag(_swapDoneFlag, true);

        if (_disableTriggerAfterSwap)
            gameObject.SetActive(false);
    }

    private void ApplySwappedState()
    {
        _hasSwapped = true;

        if (_rope1 != null)
            _rope1.SetActive(false);

        if (_rope2 != null)
            _rope2.SetActive(true);

        HideHint();

        if (_disableTriggerAfterSwap && gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    private bool IsInCurrentScene()
    {
        return string.IsNullOrWhiteSpace(_currentScene) ||
               SceneManager.GetActiveScene().name == _currentScene;
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
