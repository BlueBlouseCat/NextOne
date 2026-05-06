using UnityEngine;
using UnityEngine.UI;

public class ItemPreviewViewerUI : MonoBehaviour
{
    public static ItemPreviewViewerUI Instance { get; private set; }

    [Header("Optional Existing UI")]
    [SerializeField] private GameObject _root;
    [SerializeField] private Image _previewImage;

    [Header("Runtime UI")]
    [SerializeField] private Vector2 _maxPreviewSize = new Vector2(1100f, 700f);
    [SerializeField] private Color _backgroundColor = new Color(0f, 0f, 0f, 0.82f);
    [SerializeField] private int _sortingOrder = 500;

    [Header("Optional")]
    [SerializeField] private bool _lockPlayerWhileOpen = true;

    private PlayerMovement _playerMovement;

    public bool IsOpen => _root != null && _root.activeSelf;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureUI();
        CloseImmediate();
    }

    private void OnDisable()
    {
        UnlockPlayer();
    }

    public bool TryOpen(ItemDefinition item)
    {
        if (item == null) return false;
        if (item.previewSprite == null) return false;

        EnsureUI();

        if (_root == null || _previewImage == null)
            return false;

        _previewImage.sprite = item.previewSprite;
        ResizePreview(item.previewSprite);

        _root.SetActive(true);
        InventoryUI.Instance?.ShowInteractHint(false);

        LockPlayer();
        return true;
    }

    public void Close()
    {
        if (_root != null)
            _root.SetActive(false);

        UnlockPlayer();
    }

    public void CloseImmediate()
    {
        if (_root != null)
            _root.SetActive(false);
    }

    private void LockPlayer()
    {
        if (!_lockPlayerWhileOpen) return;

        if (_playerMovement == null)
            _playerMovement = FindObjectOfType<PlayerMovement>();

        if (_playerMovement != null)
            _playerMovement.SetExternalInputLocked(true);
    }

    private void UnlockPlayer()
    {
        if (!_lockPlayerWhileOpen) return;

        if (_playerMovement == null)
            _playerMovement = FindObjectOfType<PlayerMovement>();

        if (_playerMovement != null)
            _playerMovement.SetExternalInputLocked(false);
    }

    private void EnsureUI()
    {
        if (_root != null && _previewImage != null)
            return;

        CreateRuntimeUI();
    }

    private void CreateRuntimeUI()
    {
        GameObject canvasGO = new GameObject("ItemPreviewCanvas");
        canvasGO.transform.SetParent(transform, false);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = _sortingOrder;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject rootGO = new GameObject("PreviewRoot");
        rootGO.transform.SetParent(canvasGO.transform, false);

        RectTransform rootRect = rootGO.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image bgImage = rootGO.AddComponent<Image>();
        bgImage.color = _backgroundColor;
        bgImage.raycastTarget = true;

        GameObject previewGO = new GameObject("PreviewImage");
        previewGO.transform.SetParent(rootGO.transform, false);

        RectTransform previewRect = previewGO.AddComponent<RectTransform>();
        previewRect.anchorMin = new Vector2(0.5f, 0.5f);
        previewRect.anchorMax = new Vector2(0.5f, 0.5f);
        previewRect.pivot = new Vector2(0.5f, 0.5f);
        previewRect.anchoredPosition = Vector2.zero;
        previewRect.sizeDelta = _maxPreviewSize;

        Image previewImage = previewGO.AddComponent<Image>();
        previewImage.preserveAspect = true;
        previewImage.raycastTarget = false;

        _root = rootGO;
        _previewImage = previewImage;
    }

    private void ResizePreview(Sprite sprite)
    {
        if (sprite == null || _previewImage == null) return;

        RectTransform rect = _previewImage.rectTransform;
        if (rect == null) return;

        Vector2 spriteSize = sprite.rect.size;
        if (spriteSize.x <= 0f || spriteSize.y <= 0f)
        {
            rect.sizeDelta = _maxPreviewSize;
            return;
        }

        float scale = Mathf.Min(
            _maxPreviewSize.x / spriteSize.x,
            _maxPreviewSize.y / spriteSize.y,
            1f
        );

        rect.sizeDelta = spriteSize * scale;
    }
}
