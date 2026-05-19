using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class ToggleInteractionTrigger : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string _currentScene = "MouseHole7";

    [Header("Target")]
    [SerializeField] private GameObject _targetRoot;

    [Header("Focus")]
    [SerializeField] private Transform _focusPoint;
    [SerializeField] private int _interactionPriority = 20;

    [Header("Options")]
    [SerializeField] private bool _forceHideTargetOnStart = true;
    [SerializeField] private bool _autoCloseOnExit = false;

    private bool _playerInRange;
    private bool _hintShownByThisScript;
    private bool _holdsInteractionLock;
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
        ApplyInitialState();
    }

    private void Start()
    {
        ApplyInitialState();
    }

    private void OnDisable()
    {
        HideHint();
        ReleaseInteractionLock();
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

        InteractionFocusService.SetCandidate(this, _focusPoint, _playerInRange, _interactionPriority);

        bool interactionAvailable = !GlobalInteractionLock.IsLocked || _holdsInteractionLock;

        bool hasFocus =
            _playerInRange &&
            _playerTransform != null &&
            interactionAvailable &&
            InteractionFocusService.HasFocus(this, _playerTransform.position);

        if (hasFocus)
            ShowHint(IsTargetActive() ? ProjectInteractionHints.Close : ProjectInteractionHints.Interact);
        else
            HideHint();

        if (!hasFocus) return;

        if (!IsTargetActive())
        {
            if (GameplayInputUtil.InteractPressedThisFrame())
                SetTargetActive(true);

            return;
        }

        if (GameplayInputUtil.CancelPressedThisFrame() && GameplayInputUtil.ConsumeCancelThisFrame())
            SetTargetActive(false);
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
        }
        else
        {
            HideHint();

            if (_autoCloseOnExit)
                SetTargetActive(false);
        }
    }

    private void ApplyInitialState()
    {
        if (_forceHideTargetOnStart)
            SetTargetActive(false);
    }

    private bool IsTargetActive()
    {
        return _targetRoot != null && _targetRoot.activeSelf;
    }

    private void SetTargetActive(bool active)
    {
        if (_targetRoot != null)
            _targetRoot.SetActive(active);

        if (active)
            AcquireInteractionLock();
        else
            ReleaseInteractionLock();
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
