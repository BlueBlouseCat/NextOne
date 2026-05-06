using UnityEngine;

public class InspectableTrigger : MonoBehaviour
{
    [SerializeField] private WorldInspectable _target;
    [SerializeField] private Transform _focusPoint;
    [SerializeField] private int _interactionPriority = 0;

    private PlayerItemController _controller;
    private Transform _playerTransform;
    private bool _playerInRange;
    private bool _isFocusedByThisScript;

    private void Awake()
    {
        if (_focusPoint == null)
            _focusPoint = transform;
    }

    private void OnEnable()
    {
        InteractionFocusService.SetCandidate(this, _focusPoint, false, _interactionPriority);
    }

    private void OnDisable()
    {
        ApplyFocus(false);
        InteractionFocusService.RemoveCandidate(this);

        _controller = null;
        _playerTransform = null;
        _playerInRange = false;
    }

    private void Update()
    {
        InteractionFocusService.SetCandidate(this, _focusPoint, _playerInRange, _interactionPriority);

        bool shouldFocus =
            _playerInRange &&
            _target != null &&
            _controller != null &&
            _playerTransform != null &&
            !GlobalInteractionLock.IsLocked &&
            InteractionFocusService.HasFocus(this, _playerTransform.position);

        ApplyFocus(shouldFocus);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        UpdatePlayerState(other, true);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        UpdatePlayerState(other, true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        UpdatePlayerState(other, false);
    }

    private void UpdatePlayerState(Collider2D other, bool inRange)
    {
        if (other == null || !other.CompareTag("Player")) return;

        if (inRange)
        {
            _playerInRange = true;
            _playerTransform = other.transform;

            _controller = other.GetComponent<PlayerItemController>();
            if (_controller == null)
                _controller = other.GetComponentInParent<PlayerItemController>();
        }
        else
        {
            _playerInRange = false;
            ApplyFocus(false);
        }
    }

    private void ApplyFocus(bool focused)
    {
        if (_isFocusedByThisScript == focused) return;

        _isFocusedByThisScript = focused;

        if (_controller == null || _target == null) return;

        if (focused)
            _controller.SetFocusedInspectable(_target);
        else
            _controller.ClearFocusedInspectable(_target);
    }
}
