using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Letter2ToggleTrigger : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string _currentScene = "House";

    [Header("References")]
    [SerializeField] private GameObject _letter2Root;
    [SerializeField] private Transform _focusPoint;

    [Header("Options")]
    [SerializeField] private bool _forceLetter2HiddenOnStart = true;
    [SerializeField] private bool _autoCloseOnExit = true;
    [SerializeField] private int _interactionPriority = 10;

    private bool _playerInRange;
    private bool _hintShownByThisScript;
    private bool _isLetter2Open;
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

        if (_forceLetter2HiddenOnStart && _letter2Root != null)
            _letter2Root.SetActive(false);

        _isLetter2Open = false;
    }

    private void OnDisable()
    {
        HideHint();
        InteractionFocusService.RemoveCandidate(this);

        _playerInRange = false;
        _playerTransform = null;
        _isLetter2Open = false;

        if (_letter2Root != null)
            _letter2Root.SetActive(false);
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

        ToggleLetter2();
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

            if (_autoCloseOnExit && _isLetter2Open)
                SetLetter2Open(false);
        }
    }

    private void ToggleLetter2()
    {
        SetLetter2Open(!_isLetter2Open);
    }

    private void SetLetter2Open(bool open)
    {
        _isLetter2Open = open;

        if (_letter2Root != null)
            _letter2Root.SetActive(open);
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
