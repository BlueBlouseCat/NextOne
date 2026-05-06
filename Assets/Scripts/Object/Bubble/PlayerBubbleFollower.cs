using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerBubbleFollower : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform _target;
    [SerializeField] private string _targetTag = "Player";

    [Header("UI")]
    [SerializeField] private Canvas _canvas;
    [SerializeField] private Camera _worldCamera;

    [Header("Offset")]
    [SerializeField] private Vector3 _worldOffset = new Vector3(0f, 2.2f, 0f);
    [SerializeField] private Vector2 _screenOffset = Vector2.zero;

    private RectTransform _selfRect;
    private RectTransform _canvasRect;

    private void Awake()
    {
        _selfRect = transform as RectTransform;
        CacheCanvasRefs();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        CacheCanvasRefs();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void LateUpdate()
    {
        if (!gameObject.activeInHierarchy) return;
        if (_selfRect == null) return;
        if (_canvas == null || _canvasRect == null) return;

        ResolveTarget();
        if (_target == null) return;

        Camera cam = ResolveCamera();
        Vector3 worldPos = _target.position + _worldOffset;
        Vector3 screenPos = cam != null ? cam.WorldToScreenPoint(worldPos) : worldPos;

        if (screenPos.z < 0f) return;

        Camera uiCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect,
                screenPos,
                uiCamera,
                out Vector2 localPoint))
        {
            _selfRect.anchoredPosition = localPoint + _screenOffset;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _target = null;
        CacheCanvasRefs();
    }

    private void CacheCanvasRefs()
    {
        if (_canvas == null)
            _canvas = GetComponentInParent<Canvas>();

        if (_canvas != null)
            _canvasRect = _canvas.transform as RectTransform;
    }

    private void ResolveTarget()
    {
        if (_target != null) return;

        if (GameManager.Instance != null && GameManager.Instance.CurrentPlayer != null)
        {
            _target = GameManager.Instance.CurrentPlayer;
            return;
        }

        if (string.IsNullOrWhiteSpace(_targetTag)) return;

        GameObject targetObject = GameObject.FindWithTag(_targetTag);
        if (targetObject != null)
            _target = targetObject.transform;
    }

    private Camera ResolveCamera()
    {
        if (_worldCamera != null)
            return _worldCamera;

        if (_canvas != null &&
            _canvas.renderMode != RenderMode.ScreenSpaceOverlay &&
            _canvas.worldCamera != null)
        {
            return _canvas.worldCamera;
        }

        return Camera.main;
    }
}
