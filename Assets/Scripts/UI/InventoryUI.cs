using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    [Header("Interact")]
    [SerializeField] private GameObject _interactHintRoot;
    [SerializeField] private TMP_Text _interactHintText;

    [Header("Popup")]
    [SerializeField] private GameObject _popupRoot;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _descText;
    [SerializeField] private TMP_Text _popupHintText;

    [Header("Bag")]
    [SerializeField] private Image[] _slotIcons;
    [SerializeField] private RectTransform[] _slotRects;
    [SerializeField] private float _jumpHeight = 18f;
    [SerializeField] private float _jumpSpeed = 8f;

    private PlayerItemController _pendingController;
    private WorldItemInteractable _pendingItem;
    private bool _popupCanCloseWithCancel = true;

    private int _jumpSlot = -1;
    private Vector2[] _basePositions;
    private bool _isSubscribed;

    public bool IsPopupOpen => _popupRoot != null && _popupRoot.activeSelf;
    public bool HasPendingPickup => _pendingItem != null;
    public bool CanClosePopupWithCancel => _popupCanCloseWithCancel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        CacheBasePositions();

        if (_popupRoot != null)
            _popupRoot.SetActive(false);

        ShowInteractHint(false);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        GlobalInteractionLock.StateChanged += OnGlobalInteractionLockChanged;
        TryBindInventoryEvents();
        Refresh();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        GlobalInteractionLock.StateChanged -= OnGlobalInteractionLockChanged;
        UnbindInventoryEvents();
    }

    private void Start()
    {
        Refresh();
    }

    private void Update()
    {
        if (!_isSubscribed)
            TryBindInventoryEvents();

        UpdateSlotJumpAnimation();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CacheBasePositions();
        TryBindInventoryEvents();
        Refresh();
    }

    private void OnGlobalInteractionLockChanged(bool locked)
    {
        if (locked)
            ShowInteractHint(false);
    }

    private void CacheBasePositions()
    {
        if (_slotRects == null)
        {
            _basePositions = new Vector2[0];
            return;
        }

        _basePositions = new Vector2[_slotRects.Length];
        for (int i = 0; i < _slotRects.Length; i++)
        {
            if (_slotRects[i] != null)
                _basePositions[i] = _slotRects[i].anchoredPosition;
        }
    }

    private void TryBindInventoryEvents()
    {
        if (_isSubscribed) return;
        if (InventoryManager.Instance == null) return;

        InventoryManager.Instance.OnInventoryChanged -= Refresh;
        InventoryManager.Instance.OnInventoryChanged += Refresh;
        _isSubscribed = true;
    }

    private void UnbindInventoryEvents()
    {
        if (!_isSubscribed) return;

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= Refresh;

        _isSubscribed = false;
    }

    private void UpdateSlotJumpAnimation()
    {
        if (_jumpSlot < 0 || _slotRects == null || _jumpSlot >= _slotRects.Length) return;
        if (_slotRects[_jumpSlot] == null) return;
        if (_basePositions == null || _jumpSlot >= _basePositions.Length) return;

        Vector2 pos = _basePositions[_jumpSlot];
        pos.y += Mathf.Abs(Mathf.Sin(Time.unscaledTime * _jumpSpeed)) * _jumpHeight;
        _slotRects[_jumpSlot].anchoredPosition = pos;
    }

    public void ShowInteractHint(bool show, string hintText = null)
    {
        if (_interactHintText != null && !string.IsNullOrWhiteSpace(hintText))
            _interactHintText.text = hintText;

        if (_interactHintRoot != null)
            _interactHintRoot.SetActive(show && !GlobalInteractionLock.IsLocked);
    }

    public void OpenInspectPopup(
        string title,
        string desc,
        PlayerItemController controller,
        WorldItemInteractable pendingItem = null,
        string popupHintText = null,
        bool canCloseWithCancel = true)
    {
        _pendingController = controller;
        _pendingItem = pendingItem;
        _popupCanCloseWithCancel = canCloseWithCancel;

        if (_titleText != null)
            _titleText.text = title;

        if (_descText != null)
            _descText.text = desc;

        if (_popupHintText != null)
        {
            _popupHintText.text = !string.IsNullOrWhiteSpace(popupHintText)
                ? popupHintText
                : pendingItem != null
                    ? ProjectInteractionHints.PopupPickup
                    : ProjectInteractionHints.PopupClose;
        }

        if (_popupRoot != null)
            _popupRoot.SetActive(true);

        ShowInteractHint(false);
    }

    public bool ConfirmPendingPickup()
    {
        if (_pendingController == null || _pendingItem == null)
            return false;

        _pendingController.ConfirmPickup(_pendingItem);
        return true;
    }

    public void ClosePickupPopup()
    {
        _pendingController = null;
        _pendingItem = null;
        _popupCanCloseWithCancel = true;

        if (_popupHintText != null)
            _popupHintText.text = string.Empty;

        if (_popupRoot != null)
            _popupRoot.SetActive(false);
    }

    public void OnClickClose()
    {
        _pendingController?.CancelPopup();
    }

    public void PlaySlotHint(int slotIndex)
    {
        if (GlobalInteractionLock.IsLocked)
            return;

        StopSlotHint();
        _jumpSlot = slotIndex;
    }

    public void StopSlotHint()
    {
        if (_jumpSlot >= 0 &&
            _slotRects != null &&
            _jumpSlot < _slotRects.Length &&
            _slotRects[_jumpSlot] != null &&
            _basePositions != null &&
            _jumpSlot < _basePositions.Length)
        {
            _slotRects[_jumpSlot].anchoredPosition = _basePositions[_jumpSlot];
        }

        _jumpSlot = -1;
    }

    public void Refresh()
    {
        if (_slotIcons == null) return;

        for (int i = 0; i < _slotIcons.Length; i++)
        {
            Image iconImage = _slotIcons[i];
            if (iconImage == null) continue;

            ItemDefinition item = InventoryManager.Instance != null
                ? InventoryManager.Instance.GetSlot(i)
                : null;

            bool hasItem = item != null && item.icon != null;

            iconImage.sprite = hasItem ? item.icon : null;
            iconImage.enabled = hasItem;
            iconImage.color = hasItem ? Color.white : new Color(1f, 1f, 1f, 0f);
        }
    }
}
