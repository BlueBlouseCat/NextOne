using UnityEngine;
using UnityEngine.UI;

public class ItemPreviewViewerUI : MonoBehaviour
{
    public static ItemPreviewViewerUI Instance { get; private set; }

    [Header("Optional Existing UI")]
    [SerializeField] private GameObject _root;
    [SerializeField] private Image _previewImage;
    [SerializeField] private Image _previewOverlayImage;

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
        if (_root == null || _previewImage == null) return false;

        _previewImage.sprite = item.previewSprite;
        ResizePreview(_previewImage.rectTransform, item.previewSprite, _maxPreviewSize);
        _previewImage.enabled = true;
        _previewImage.transform.SetAsLastSibling();

        if (_previewOverlayImage != null)
        {
            if (item.previewOverlaySprite != null)
            {
                _previewOverlayImage.sprite = item.previewOverlaySprite;
                _previewOverlayImage.color = new Color(1f, 1f, 1f, Mathf.Clamp01(item.previewOverlayAlpha));
                ResizePreview(_previewOverlayImage.rectTransform, item.previewOverlaySprite, _maxPreviewSize);
                _previewOverlayImage.rectTransform.anchoredPosition = item.previewOverlayOffset;
                _previewOverlayImage.rectTransform.sizeDelta = new Vector2(
                    _previewOverlayImage.rectTransform.sizeDelta.x * Mathf.Max(0.01f, item.previewOverlaySizeMultiplier.x),
                    _previewOverlayImage.rectTransform.sizeDelta.y * Mathf.Max(0.01f, item.previewOverlaySizeMultiplier.y)
                );
                _previewOverlayImage.enabled = true;
                _previewOverlayImage.transform.SetAsLastSibling();
            }
            else
            {
                _previewOverlayImage.sprite = null;
                _previewOverlayImage.enabled = false;
            }
        }

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
        if (_previewImage != null)
        {
            _previewImage.sprite = null;
            _previewImage.enabled = false;
        }

        if (_previewOverlayImage != null)
        {
            _previewOverlayImage.sprite = null;
            _previewOverlayImage.enabled = false;
        }

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
        {
            if (_previewOverlayImage == null)
                CreateOverlayImage(_root.transform);

            return;
        }

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
        CreateOverlayImage(rootGO.transform);
    }

    private void CreateOverlayImage(Transform parent)
    {
        GameObject overlayGO = new GameObject("PreviewOverlayImage");
        overlayGO.transform.SetParent(parent, false);

        RectTransform overlayRect = overlayGO.AddComponent<RectTransform>();
        overlayRect.anchorMin = new Vector2(0.5f, 0.5f);
        overlayRect.anchorMax = new Vector2(0.5f, 0.5f);
        overlayRect.pivot = new Vector2(0.5f, 0.5f);
        overlayRect.anchoredPosition = Vector2.zero;
        overlayRect.sizeDelta = _maxPreviewSize;

        Image overlayImage = overlayGO.AddComponent<Image>();
        overlayImage.preserveAspect = true;
        overlayImage.raycastTarget = false;
        overlayImage.enabled = false;

        _previewOverlayImage = overlayImage;
    }

    private void ResizePreview(RectTransform rect, Sprite sprite, Vector2 maxSize)
    {
        if (sprite == null || rect == null) return;

        Vector2 spriteSize = sprite.rect.size;
        if (spriteSize.x <= 0f || spriteSize.y <= 0f)
        {
            rect.sizeDelta = maxSize;
            return;
        }

        float scale = Mathf.Min(maxSize.x / spriteSize.x, maxSize.y / spriteSize.y, 1f);
        rect.sizeDelta = spriteSize * scale;
    }
}
