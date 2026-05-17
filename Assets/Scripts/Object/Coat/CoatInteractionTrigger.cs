using UnityEngine;
using UnityEngine.InputSystem;

public class CoatInteractionTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject _coat2;
    [SerializeField] private GameObject _coat2CanvasRoot;
    [SerializeField] private CoatInfoPopupUI _popupUI;
    [SerializeField] private CoatClickablePoint[] _clickablePoints;
    [SerializeField] private Transform _focusPoint;

    [Header("Options")]
    [SerializeField] private bool _forceHideCoat2OnStart = true;
    [SerializeField] private bool _resetViewedStateEveryOpen = true;
    [SerializeField] private LayerMask _clickableLayerMask = ~0;
    [SerializeField] private int _interactionPriority = 10;

    private bool _playerInRange;
    private bool _hintShownByThisScript;
    private bool _ignoreMouseWorldClickUntilRelease;
    private PlayerMovement _playerMovement;
    private Transform _playerTransform;

    private void Awake()
    {
        if (_focusPoint == null)
            _focusPoint = transform;
    }

    private void OnEnable()
    {
        if (_popupUI != null)
        {
            _popupUI.Opened += HandlePopupOpened;
            _popupUI.Closed += HandlePopupClosed;
        }

        InteractionFocusService.SetCandidate(this, _focusPoint, false, _interactionPriority);
    }

    private void Start()
    {
        if (_forceHideCoat2OnStart && _coat2 != null)
            _coat2.SetActive(false);

        ResetClickablePoints();
        UpdateCoatCanvasVisibility();
        HideHint();
    }

    private void OnDisable()
    {
        if (_popupUI != null)
        {
            _popupUI.Opened -= HandlePopupOpened;
            _popupUI.Closed -= HandlePopupClosed;
        }

        InteractionFocusService.RemoveCandidate(this);

        HideHint();
        UnlockPlayer();
        _playerInRange = false;
    }

    private void Update()
    {
        InteractionFocusService.SetCandidate(this, _focusPoint, _playerInRange, _interactionPriority);

        bool hasFocus =
            _playerInRange &&
            _playerTransform != null &&
            !GlobalInteractionLock.IsLocked &&
            InteractionFocusService.HasFocus(this, _playerTransform.position);

        RefreshHint(hasFocus);
        UpdateIgnoreMouseState();

        HandleFInput(hasFocus);
        HandleMouseClickInput(hasFocus);
    }

    private void HandleFInput(bool hasFocus)
    {
        if (!hasFocus) return;
        if (!GameplayInputUtil.InteractPressedThisFrame()) return;
        if (IsPopupOpen()) return;

        if (!IsCoat2Active())
        {
            OpenCoat2();
            return;
        }

        if (CanCloseCoat2())
            CloseCoat2();
    }

    private void HandleMouseClickInput(bool hasFocus)
    {
        if (!hasFocus) return;
        if (!IsCoat2Active()) return;
        if (IsPopupOpen()) return;
        if (_ignoreMouseWorldClickUntilRelease) return;
        if (Mouse.current == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;
        if (Camera.main == null) return;

        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        Vector2 point = new Vector2(mouseWorldPos.x, mouseWorldPos.y);

        Collider2D[] hits = Physics2D.OverlapPointAll(point, _clickableLayerMask);
        if (hits == null || hits.Length == 0) return;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null) continue;

            CoatClickablePoint clickablePoint = hit.GetComponentInParent<CoatClickablePoint>();
            if (clickablePoint == null) continue;
            if (!IsManagedClickablePoint(clickablePoint)) continue;

            clickablePoint.MarkViewed();
            UpdateCoatCanvasVisibility();

            if (_popupUI != null)
                _popupUI.Open(clickablePoint.Title, clickablePoint.Description);

            break;
        }
    }

    private void UpdateIgnoreMouseState()
    {
        if (!_ignoreMouseWorldClickUntilRelease) return;
        if (Mouse.current == null) return;

        if (!Mouse.current.leftButton.isPressed)
            _ignoreMouseWorldClickUntilRelease = false;
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

        if (inRange)
        {
            _playerInRange = true;
            _playerTransform = other.transform;

            if (_playerMovement == null)
            {
                _playerMovement = other.GetComponent<PlayerMovement>();
                if (_playerMovement == null)
                    _playerMovement = other.GetComponentInParent<PlayerMovement>();
            }
        }
        else
        {
            _playerInRange = false;
            HideHint();

            if (!IsPopupOpen())
                SetCoat2Active(false);
        }
    }

    private void HandlePopupOpened()
    {
        HideHint();
        LockPlayer();
        _ignoreMouseWorldClickUntilRelease = true;
    }

    private void HandlePopupClosed()
    {
        UnlockPlayer();
        _ignoreMouseWorldClickUntilRelease = true;

        if (!_playerInRange)
            SetCoat2Active(false);
    }

    private void RefreshHint(bool hasFocus)
    {
        bool shouldShow = false;

        if (hasFocus && !IsPopupOpen())
        {
            if (!IsCoat2Active())
                shouldShow = true;
            else if (CanCloseCoat2())
                shouldShow = true;
        }

        if (shouldShow)
            ShowHint();
        else
            HideHint();
    }

    private void OpenCoat2()
    {
        if (IsCoat2Active()) return;

        if (_resetViewedStateEveryOpen)
            ResetClickablePoints();

        SetCoat2Active(true);
        UpdateCoatCanvasVisibility();
        HideHint();
    }

    private void CloseCoat2()
    {
        if (!IsCoat2Active()) return;

        SetCoat2Active(false);
    }

    private bool CanCloseCoat2()
    {
        if (!IsCoat2Active()) return false;
        if (IsPopupOpen()) return false;
        if (GlobalInteractionLock.IsLocked) return false;
        return AreAllClickablePointsViewed();
    }

    private bool AreAllClickablePointsViewed()
    {
        if (_clickablePoints == null || _clickablePoints.Length == 0)
            return false;

        for (int i = 0; i < _clickablePoints.Length; i++)
        {
            CoatClickablePoint point = _clickablePoints[i];
            if (point == null) return false;
            if (!point.HasBeenViewed) return false;
        }

        return true;
    }

    private void ResetClickablePoints()
    {
        if (_clickablePoints == null) return;

        for (int i = 0; i < _clickablePoints.Length; i++)
        {
            if (_clickablePoints[i] != null)
                _clickablePoints[i].ResetViewedState();
        }
    }

    private bool IsManagedClickablePoint(CoatClickablePoint point)
    {
        if (point == null || _clickablePoints == null) return false;

        for (int i = 0; i < _clickablePoints.Length; i++)
        {
            if (_clickablePoints[i] == point)
                return true;
        }

        return false;
    }

    private bool IsPopupOpen()
    {
        return _popupUI != null && _popupUI.IsOpen;
    }

    private bool IsCoat2Active()
    {
        return _coat2 != null && _coat2.activeSelf;
    }

    private void SetCoat2Active(bool active)
    {
        if (_coat2 != null)
            _coat2.SetActive(active);

        UpdateCoatCanvasVisibility();
    }

    private void UpdateCoatCanvasVisibility()
    {
        if (_coat2CanvasRoot == null) return;

        bool shouldShowCanvas = IsCoat2Active() && !AreAllClickablePointsViewed();
        _coat2CanvasRoot.SetActive(shouldShowCanvas);
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
}
