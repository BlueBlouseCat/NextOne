using UnityEngine;

public class CatDialogueFollower : MonoBehaviour
{
    [Header("World Target")]
    [SerializeField] private Transform _worldAnchorTarget;
    [SerializeField] private bool _useParentAsDefaultTarget = true;
    [SerializeField] private Vector3 _worldOffset = new Vector3(0f, 2.2f, 0f);

    [Header("UI")]
    [SerializeField] private Canvas _uiCanvas;
    [SerializeField] private RectTransform _boxRect;
    [SerializeField] private Camera _worldCamera;
    [SerializeField] private Vector2 _screenOffset = Vector2.zero;

    [Header("Optional")]
    [SerializeField] private bool _hideWhenTargetBehindCamera = true;
    [SerializeField] private bool _hideWhenOffScreen = false;
    [SerializeField] private GameObject _visibilityTarget;

    private RectTransform _canvasRect;

    private void Awake()
    {
        CacheReferences();
        ResolveDefaultTarget();
        ApplyPosition();
    }

    private void OnEnable()
    {
        CacheReferences();
        ResolveDefaultTarget();
        ApplyPosition();
    }

    private void LateUpdate()
    {
        ApplyPosition();
    }

    private void CacheReferences()
    {
        if (_uiCanvas == null)
            _uiCanvas = GetComponentInChildren<Canvas>(true);

        if (_uiCanvas != null)
            _canvasRect = _uiCanvas.transform as RectTransform;

        if (_boxRect == null && _uiCanvas != null)
        {
            Transform canvasTransform = _uiCanvas.transform;
            if (canvasTransform.childCount > 0)
                _boxRect = canvasTransform.GetChild(0) as RectTransform;
        }

        if (_visibilityTarget == null && _boxRect != null)
            _visibilityTarget = _boxRect.gameObject;
    }

    private void ResolveDefaultTarget()
    {
        if (_worldAnchorTarget != null)
            return;

        if (!_useParentAsDefaultTarget)
            return;

        Transform parent = transform.parent;
        if (parent != null)
            _worldAnchorTarget = parent;
    }

    private void ApplyPosition()
    {
        if (_worldAnchorTarget == null) return;
        if (_uiCanvas == null || _canvasRect == null) return;
        if (_boxRect == null) return;

        Camera cam = ResolveWorldCamera();
        if (cam == null) return;

        Vector3 worldPos = _worldAnchorTarget.position + _worldOffset;
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

        if (_hideWhenTargetBehindCamera && screenPos.z < 0f)
        {
            SetVisible(false);
            return;
        }

        bool offScreen =
            screenPos.x < 0f ||
            screenPos.x > Screen.width ||
            screenPos.y < 0f ||
            screenPos.y > Screen.height;

        if (_hideWhenOffScreen && offScreen)
        {
            SetVisible(false);
            return;
        }

        Camera uiCamera = _uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : _uiCanvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect,
                screenPos,
                uiCamera,
                out Vector2 localPoint))
        {
            _boxRect.anchoredPosition = localPoint + _screenOffset;
            SetVisible(true);
        }
    }

    private Camera ResolveWorldCamera()
    {
        if (_worldCamera != null)
            return _worldCamera;

        return Camera.main;
    }

    private void SetVisible(bool visible)
    {
        if (_visibilityTarget == null)
            return;

        if (_visibilityTarget.activeSelf != visible)
            _visibilityTarget.SetActive(visible);
    }
}