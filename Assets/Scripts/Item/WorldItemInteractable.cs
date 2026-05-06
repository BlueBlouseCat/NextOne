using UnityEngine;

public class WorldItemInteractable : MonoBehaviour
{
    [SerializeField] private ItemDefinition _item;
    [SerializeField] private GameObject _outlineRoot;
    [SerializeField] private Transform _focusPoint;
    [SerializeField] private int _interactionPriority = 0;

    private PlayerItemController _controller;
    private Transform _playerTransform;
    private bool _playerInRange;
    private bool _isFocusedByThisScript;

    public ItemDefinition Item => _item;

    private void Awake()
    {
        if (_focusPoint == null)
            _focusPoint = transform;
    }

    private void OnEnable()
    {
        InteractionFocusService.SetCandidate(this, _focusPoint, false, _interactionPriority);
    }

    private void Start()
    {
        SetOutline(false);

        if (GameManager.Instance != null && _item != null && GameManager.Instance.GetFlag(_item.CollectedFlag))
            gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        ApplyFocus(false);
        InteractionFocusService.RemoveCandidate(this);
        SetOutline(false);

        _controller = null;
        _playerTransform = null;
        _playerInRange = false;
    }

    private void Update()
    {
        InteractionFocusService.SetCandidate(this, _focusPoint, _playerInRange, _interactionPriority);

        bool shouldFocus =
            _playerInRange &&
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

        if (_controller != null)
        {
            if (focused)
                _controller.SetFocusedItem(this);
            else
                _controller.ClearFocusedItem(this);
        }

        SetOutline(focused);
    }

    public void SetOutline(bool visible)
    {
        if (_outlineRoot != null)
            _outlineRoot.SetActive(visible);
    }

    public void Pickup()
    {
        if (GameManager.Instance != null && _item != null)
            GameManager.Instance.SetFlag(_item.CollectedFlag, true);

        ApplyFocus(false);
        SetOutline(false);
        InteractionFocusService.RemoveCandidate(this);
        gameObject.SetActive(false);
    }
}
