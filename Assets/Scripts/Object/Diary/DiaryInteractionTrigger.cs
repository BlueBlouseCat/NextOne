using UnityEngine;
using UnityEngine.InputSystem;

public class DiaryInteractionTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject _diary2;
    [SerializeField] private Transform _focusPoint;

    [Header("Options")]
    [SerializeField] private bool _forceHideDiary2OnStart = true;
    [SerializeField] private bool _autoCloseDiary2OnExit = true;
    [SerializeField] private int _interactionPriority = 10;

    private bool _playerInRange;
    private bool _hintShownByThisScript;
    private bool _holdsInteractionLock;
    private Transform _playerTransform;
    private PlayerItemController _playerItemController;

    private void Awake()
    {
        if (_focusPoint == null)
            _focusPoint = transform;
    }

    private void Start()
    {
        if (_forceHideDiary2OnStart && _diary2 != null)
            _diary2.SetActive(false);

        HideHint();
    }

    private void OnEnable()
    {
        HideHint();
        InteractionFocusService.SetCandidate(this, _focusPoint, false, _interactionPriority);
    }

    private void OnDisable()
    {
        _playerInRange = false;
        HideHint();
        ReleaseInteractionLock();
        InteractionFocusService.RemoveCandidate(this);
    }

    private void Update()
    {
        InteractionFocusService.SetCandidate(this, _focusPoint, _playerInRange, _interactionPriority);

        bool blockingPopupOpen = HasBlockingPopupOpen();
        bool interactionAvailable = !GlobalInteractionLock.IsLocked || _holdsInteractionLock;

        bool hasFocus =
            _playerInRange &&
            _playerTransform != null &&
            !blockingPopupOpen &&
            interactionAvailable &&
            InteractionFocusService.HasFocus(this, _playerTransform.position);

        if (hasFocus)
            ShowHint(IsDiary2Active() ? ProjectInteractionHints.Close : ProjectInteractionHints.Interact);
        else
            HideHint();

        if (!hasFocus) return;

        if (!IsDiary2Active())
        {
            if (GameplayInputUtil.InteractPressedThisFrame())
                SetDiary2Active(true);

            return;
        }

        if (GameplayInputUtil.CancelPressedThisFrame() && GameplayInputUtil.ConsumeCancelThisFrame())
            SetDiary2Active(false);
    }

    private bool HasBlockingPopupOpen()
    {
        if (IsDiary2Active())
            return false;

        if (InventoryUI.Instance != null && InventoryUI.Instance.IsPopupOpen)
            return true;

        if (ItemPreviewViewerUI.Instance != null && ItemPreviewViewerUI.Instance.IsOpen)
            return true;

        return false;
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

        _playerInRange = inRange;

        if (inRange)
        {
            _playerTransform = other.transform;

            if (_playerItemController == null)
            {
                _playerItemController = other.GetComponent<PlayerItemController>();
                if (_playerItemController == null)
                    _playerItemController = other.GetComponentInParent<PlayerItemController>();
            }
        }
        else
        {
            HideHint();

            if (_autoCloseDiary2OnExit)
                SetDiary2Active(false);
        }
    }

    private bool IsDiary2Active()
    {
        return _diary2 != null && _diary2.activeSelf;
    }

    private void SetDiary2Active(bool active)
    {
        if (_diary2 != null)
            _diary2.SetActive(active);

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
        _playerItemController?.ClearFocusedTargets();
        InventoryUI.Instance?.ShowInteractHint(false);
    }

    private void ReleaseInteractionLock()
    {
        if (!_holdsInteractionLock) return;

        GlobalInteractionLock.Release();
        _holdsInteractionLock = false;
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
