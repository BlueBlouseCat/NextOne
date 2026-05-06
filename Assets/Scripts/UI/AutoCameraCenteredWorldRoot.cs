using UnityEngine;

public class AutoCameraCenteredWorldRoot : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform _target;

    [Header("Center Offset")]
    [SerializeField] private Vector2 _centerOffset = Vector2.zero;

    [Header("Options")]
    [SerializeField] private bool _followWhileVisible = true;
    [SerializeField] private bool _keepOriginalZ = true;
    [SerializeField] private bool _restoreRotationOnHide = true;
    [SerializeField] private bool _restoreScaleOnHide = true;
    [SerializeField] private bool _centerIfAlreadyVisibleOnStart = false;

    private bool _hasSnapshot;
    private bool _wasVisibleLastFrame;

    private Vector3 _originalWorldPosition;
    private Quaternion _originalWorldRotation;
    private Vector3 _originalLocalScale;

    private void Awake()
    {
        if (_target == null)
            _target = transform;

        CacheCurrentTransform();

        _wasVisibleLastFrame = IsTargetVisible();

        if (_centerIfAlreadyVisibleOnStart && _wasVisibleLastFrame)
            MoveTargetToCameraCenter();
    }

    private void LateUpdate()
    {
        if (_target == null)
            return;

        bool isVisible = IsTargetVisible();

        if (isVisible && !_wasVisibleLastFrame)
        {
            CacheCurrentTransform();
            MoveTargetToCameraCenter();
        }
        else if (!isVisible && _wasVisibleLastFrame)
        {
            RestoreTargetTransform();
        }

        if (isVisible && _followWhileVisible)
            MoveTargetToCameraCenter();

        _wasVisibleLastFrame = isVisible;
    }

    public void ForceCenterNow()
    {
        if (_target == null)
            return;

        if (!IsTargetVisible())
            return;

        CacheCurrentTransform();
        MoveTargetToCameraCenter();
        _wasVisibleLastFrame = true;
    }

    public void RestoreNow()
    {
        RestoreTargetTransform();
        _wasVisibleLastFrame = IsTargetVisible();
    }

    private bool IsTargetVisible()
    {
        return _target != null &&
               _target.gameObject.activeInHierarchy;
    }

    private void CacheCurrentTransform()
    {
        if (_target == null)
            return;

        _originalWorldPosition = _target.position;
        _originalWorldRotation = _target.rotation;
        _originalLocalScale = _target.localScale;
        _hasSnapshot = true;
    }

    private void MoveTargetToCameraCenter()
    {
        Camera cam = Camera.main;
        if (cam == null || _target == null)
            return;

        Vector3 camPos = cam.transform.position;
        float z = _keepOriginalZ ? _originalWorldPosition.z : _target.position.z;

        _target.position = new Vector3(
            camPos.x + _centerOffset.x,
            camPos.y + _centerOffset.y,
            z
        );
    }

    private void RestoreTargetTransform()
    {
        if (!_hasSnapshot || _target == null)
            return;

        _target.position = _originalWorldPosition;

        if (_restoreRotationOnHide)
            _target.rotation = _originalWorldRotation;

        if (_restoreScaleOnHide)
            _target.localScale = _originalLocalScale;
    }
}
