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
    private Transform _playerTransform;

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
        InteractionFocusService.RemoveCandidate(this);
    }

    private void Update()
    {
        InteractionFocusService.SetCandidate(this, _focusPoint, _playerInRange, _interactionPriority);

        bool hasFocus =
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

        ToggleDiary2();
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
        }
        else
        {
            HideHint();

            if (_autoCloseDiary2OnExit)
                SetDiary2Active(false);
        }
    }

    private void ToggleDiary2()
    {
        SetDiary2Active(!IsDiary2Active());
    }

    private bool IsDiary2Active()
    {
        return _diary2 != null && _diary2.activeSelf;
    }

    private void SetDiary2Active(bool active)
    {
        if (_diary2 != null)
            _diary2.SetActive(active);
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
