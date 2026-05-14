using System.Collections;
using DG.Tweening;
using Spine;
using Spine.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BellTouchTrigger : MonoBehaviour
{
    [Header("Bell Trigger")]
    [SerializeField] private string _playerTag = "Player";
    [SerializeField] private bool _playOnlyOnce = true;
    [SerializeField] private bool _restartIfAlreadyPlaying = true;

    [Header("Bell Spine")]
    [SerializeField] private SkeletonAnimation _skeletonAnimation;
    [SpineAnimation, SerializeField] private string _touchAnimation = "touch";
    [SerializeField] private bool _returnToSetupPoseWhenFinished = true;

    [Header("Elevator References")]
    [SerializeField] private Transform _elevatorRoot;
    [SerializeField] private Transform _doorTransform;
    [SerializeField] private Collider2D _platformCollider;
    [SerializeField] private Collider2D _backBlockCollider;

    [Header("Safe Boarding")]
    [SerializeField] private Collider2D _rideSafeZoneTrigger;
    [SerializeField] private Transform _boardingStandPoint;
    [SerializeField] private float _alignSpeed = 6f;
    [SerializeField] private float _alignStopDistance = 0.02f;
    [SerializeField] private bool _lockPlayerXAfterSnap = true;

    [Header("Door Tween")]
    [SerializeField] private float _doorOpenScaleX = 0.2f;
    [SerializeField] private float _doorClosedScaleX = 1f;
    [SerializeField] private float _doorOpenDuration = 0.45f;
    [SerializeField] private float _doorCloseDuration = 0.25f;
    [SerializeField] private Ease _doorOpenEase = Ease.OutCubic;
    [SerializeField] private Ease _doorCloseEase = Ease.OutCubic;

    [Header("Ride")]
    [SerializeField] private float _elevatorMoveSpeed = 2f;
    [SerializeField] private float _stopPlayerY = 5.4f;

    [Header("Scene")]
    [SerializeField] private string _targetScene = "MouseHole6";
    [SerializeField] private string _targetSpawnPointId = "";
    [SerializeField] private bool _useFade = false;

    private TrackEntry _touchTrack;
    private Tween _doorTween;

    private bool _hasPlayed;
    private bool _doorOpened;
    private bool _ridePreparing;
    private bool _rideStarted;
    private bool _sceneLoadRequested;

    private Transform _player;
    private Collider2D _playerCollider;
    private Rigidbody2D _playerRigidbody;
    private PlayerMovement _playerMovement;

    private float _cachedGravityScale;
    private bool _cachedGravityValid;

    private RigidbodyConstraints2D _cachedConstraints;
    private bool _cachedConstraintsValid;
    private bool _playerXFrozen;

    private void Awake()
    {
        if (_skeletonAnimation == null)
            _skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();

        BoxCollider2D bellTrigger = GetComponent<BoxCollider2D>();
        if (bellTrigger != null)
            bellTrigger.isTrigger = true;

        if (_elevatorRoot == null)
            _elevatorRoot = transform.parent;
    }

    private void Update()
    {
        ResolvePlayerReferences();

        if (!_doorOpened) return;
        if (_ridePreparing || _rideStarted || _sceneLoadRequested) return;

        if (CanStartRide())
            StartCoroutine(BoardAndRideRoutine());
    }

    private void FixedUpdate()
    {
        if (!_rideStarted) return;
        if (_sceneLoadRequested) return;

        MoveElevatorAndPlayerUp();
    }

    private void OnDisable()
    {
        KillDoorTween();
        ClearTrackCallback();
        RestoreSetupPose();
        RestorePlayerState();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null || !other.CompareTag(_playerTag))
            return;

        if (_doorOpened || _ridePreparing || _rideStarted || _sceneLoadRequested)
            return;

        PlayTouch();
    }

    public bool PlayTouch()
    {
        if (_playOnlyOnce && _hasPlayed)
            return false;

        _hasPlayed = true;

        if (_skeletonAnimation == null || _skeletonAnimation.AnimationState == null || string.IsNullOrWhiteSpace(_touchAnimation))
        {
            HandleTouchFinished();
            return true;
        }

        if (_touchTrack != null && !_restartIfAlreadyPlaying)
            return false;

        ClearTrackCallback();

        _touchTrack = _skeletonAnimation.AnimationState.SetAnimation(0, _touchAnimation, false);
        if (_touchTrack == null)
        {
            HandleTouchFinished();
            return true;
        }

        _touchTrack.Complete += OnTouchComplete;
        return true;
    }

    private void OnTouchComplete(TrackEntry entry)
    {
        if (_touchTrack != entry)
            return;

        ClearTrackCallback();
        RestoreSetupPose();
        HandleTouchFinished();
    }

    private void HandleTouchFinished()
    {
        DisableBackBlockCollider();
        PlayDoorOpenTween();
    }

    private void DisableBackBlockCollider()
    {
        if (_backBlockCollider != null)
            _backBlockCollider.enabled = false;
    }

    private void PlayDoorOpenTween()
    {
        if (_doorTransform == null)
        {
            _doorOpened = true;
            return;
        }

        KillDoorTween();

        _doorTween = _doorTransform
            .DOScaleX(_doorOpenScaleX, _doorOpenDuration)
            .SetEase(_doorOpenEase)
            .OnComplete(() =>
            {
                _doorTween = null;
                _doorOpened = true;
            });
    }

    private bool CanStartRide()
    {
        if (!IsPlayerTouchingPlatform())
            return false;

        if (!IsPlayerInsideRideSafeZone())
            return false;

        return true;
    }

    private bool IsPlayerTouchingPlatform()
    {
        if (_platformCollider == null || _playerCollider == null)
            return false;

        return _platformCollider.IsTouching(_playerCollider);
    }

    private bool IsPlayerInsideRideSafeZone()
    {
        if (_rideSafeZoneTrigger == null || _playerCollider == null)
            return false;

        Bounds safe = _rideSafeZoneTrigger.bounds;
        Bounds player = _playerCollider.bounds;

        bool overlapX = safe.min.x <= player.max.x && safe.max.x >= player.min.x;
        bool overlapY = safe.min.y <= player.max.y && safe.max.y >= player.min.y;

        return overlapX && overlapY;
    }


    private IEnumerator BoardAndRideRoutine()
    {
        _ridePreparing = true;

        ResolvePlayerReferences();
        if (_player == null)
        {
            _ridePreparing = false;
            yield break;
        }

        LockPlayerForRide();

        yield return AlignPlayerToStandPoint();

        if (_player == null)
        {
            _ridePreparing = false;
            RestorePlayerState();
            yield break;
        }

        SnapPlayerToStandPointImmediate();

        if (_lockPlayerXAfterSnap)
            FreezePlayerX();

        yield return CloseDoorRoutine();

        _doorOpened = false;
        _ridePreparing = false;
        _rideStarted = true;
    }

    private IEnumerator AlignPlayerToStandPoint()
    {
        if (_boardingStandPoint == null || _player == null)
            yield break;

        while (_player != null)
        {
            Vector2 current = GetPlayerPosition();
            Vector2 target = _boardingStandPoint.position;
            float distance = Vector2.Distance(current, target);

            if (distance <= _alignStopDistance)
                break;

            Vector2 next = Vector2.MoveTowards(current, target, _alignSpeed * Time.fixedDeltaTime);
            SetPlayerPosition(next);

            yield return new WaitForFixedUpdate();
        }
    }

    private void SnapPlayerToStandPointImmediate()
    {
        if (_player == null || _boardingStandPoint == null)
            return;

        SetPlayerPosition(_boardingStandPoint.position);
    }

    private IEnumerator CloseDoorRoutine()
    {
        if (_doorTransform == null || _doorCloseDuration <= 0f)
        {
            SetDoorScaleX(_doorClosedScaleX);
            yield break;
        }

        bool completed = false;

        KillDoorTween();

        _doorTween = _doorTransform
            .DOScaleX(_doorClosedScaleX, _doorCloseDuration)
            .SetEase(_doorCloseEase)
            .OnComplete(() =>
            {
                _doorTween = null;
                completed = true;
            });

        while (!completed)
            yield return null;
    }

    private void MoveElevatorAndPlayerUp()
    {
        if (_elevatorRoot == null || _player == null)
        {
            FinishRide();
            return;
        }

        if (GetPlayerY() >= _stopPlayerY)
        {
            FinishRide();
            return;
        }

        float remainingY = _stopPlayerY - GetPlayerY();
        float stepY = Mathf.Min(_elevatorMoveSpeed * Time.fixedDeltaTime, remainingY);
        Vector3 delta = new Vector3(0f, stepY, 0f);

        _elevatorRoot.position += delta;

        if (_boardingStandPoint != null)
            SetPlayerPosition(_boardingStandPoint.position);
        else
            SetPlayerPosition(GetPlayerPosition() + new Vector2(0f, stepY));

        if (GetPlayerY() >= _stopPlayerY - 0.001f)
            FinishRide();
    }

    private void FinishRide()
    {
        _rideStarted = false;
        RestorePlayerState();
        LoadTargetScene();
    }

    private void LoadTargetScene()
    {
        if (_sceneLoadRequested) return;
        _sceneLoadRequested = true;

        if (string.IsNullOrWhiteSpace(_targetScene))
            return;

        if (GameManager.Instance != null)
        {
            if (_useFade)
            {
                if (string.IsNullOrWhiteSpace(_targetSpawnPointId))
                    GameManager.Instance.LoadSceneWithFade(_targetScene);
                else
                    GameManager.Instance.LoadSceneWithFade(_targetScene, _targetSpawnPointId);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(_targetSpawnPointId))
                    GameManager.Instance.LoadScene(_targetScene);
                else
                    GameManager.Instance.LoadScene(_targetScene, _targetSpawnPointId);
            }

            return;
        }

        SceneManager.LoadScene(_targetScene);
    }

    private void ResolvePlayerReferences()
    {
        if (_player == null)
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentPlayer != null)
                _player = GameManager.Instance.CurrentPlayer;
            else
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag(_playerTag);
                if (playerObject != null)
                    _player = playerObject.transform;
            }
        }

        if (_player == null)
            return;

        if (_playerCollider == null)
        {
            _playerCollider = _player.GetComponent<Collider2D>();
            if (_playerCollider == null)
                _playerCollider = _player.GetComponentInChildren<Collider2D>();
        }

        if (_playerRigidbody == null)
        {
            _playerRigidbody = _player.GetComponent<Rigidbody2D>();
            if (_playerRigidbody == null)
                _playerRigidbody = _player.GetComponentInChildren<Rigidbody2D>();
        }

        if (_playerMovement == null)
        {
            _playerMovement = _player.GetComponent<PlayerMovement>();
            if (_playerMovement == null)
                _playerMovement = _player.GetComponentInChildren<PlayerMovement>();
        }
    }

    private void LockPlayerForRide()
    {
        if (_playerMovement != null)
        {
            _playerMovement.SetExternalInputLocked(true);
            _playerMovement.SetJumpInputLocked(true);
        }

        if (_playerRigidbody != null)
        {
            _cachedGravityScale = _playerRigidbody.gravityScale;
            _cachedGravityValid = true;

            _playerRigidbody.velocity = Vector2.zero;
            _playerRigidbody.angularVelocity = 0f;
            _playerRigidbody.gravityScale = 0f;
        }
    }

    private void FreezePlayerX()
    {
        if (_playerRigidbody == null || _playerXFrozen)
            return;

        _cachedConstraints = _playerRigidbody.constraints;
        _cachedConstraintsValid = true;

        _playerRigidbody.constraints = _cachedConstraints | RigidbodyConstraints2D.FreezePositionX;
        _playerXFrozen = true;
    }

    private void RestorePlayerState()
    {
        if (_playerMovement != null)
        {
            _playerMovement.SetExternalInputLocked(false);
            _playerMovement.SetJumpInputLocked(false);
        }

        if (_playerRigidbody != null)
        {
            _playerRigidbody.velocity = Vector2.zero;
            _playerRigidbody.angularVelocity = 0f;

            if (_cachedGravityValid)
                _playerRigidbody.gravityScale = _cachedGravityScale;

            if (_playerXFrozen && _cachedConstraintsValid)
                _playerRigidbody.constraints = _cachedConstraints;
        }

        _playerXFrozen = false;
        _cachedConstraintsValid = false;
    }

    private Vector2 GetPlayerPosition()
    {
        if (_playerRigidbody != null)
            return _playerRigidbody.position;

        return _player != null
            ? (Vector2)_player.position
            : Vector2.zero;
    }

    private float GetPlayerY()
    {
        return _playerRigidbody != null ? _playerRigidbody.position.y : _player.position.y;
    }

    private void SetPlayerPosition(Vector2 targetPosition)
    {
        if (_playerRigidbody != null)
        {
            _playerRigidbody.velocity = Vector2.zero;
            _playerRigidbody.angularVelocity = 0f;
            _playerRigidbody.MovePosition(targetPosition);
        }
        else if (_player != null)
        {
            _player.position = new Vector3(targetPosition.x, targetPosition.y, _player.position.z);
        }
    }

    private void SetDoorScaleX(float targetX)
    {
        if (_doorTransform == null) return;

        Vector3 scale = _doorTransform.localScale;
        scale.x = targetX;
        _doorTransform.localScale = scale;
    }

    private void KillDoorTween()
    {
        if (_doorTween != null)
        {
            _doorTween.Kill();
            _doorTween = null;
        }
    }

    private void ClearTrackCallback()
    {
        if (_touchTrack != null)
        {
            _touchTrack.Complete -= OnTouchComplete;
            _touchTrack = null;
        }
    }

    private void RestoreSetupPose()
    {
        if (!_returnToSetupPoseWhenFinished)
            return;

        if (_skeletonAnimation == null || _skeletonAnimation.AnimationState == null)
            return;

        _skeletonAnimation.AnimationState.ClearTrack(0);
        _skeletonAnimation.Skeleton.SetToSetupPose();
        _skeletonAnimation.AnimationState.Apply(_skeletonAnimation.Skeleton);
    }

}
