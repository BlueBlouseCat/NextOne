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

    [Header("Door Tween")]
    [SerializeField] private float _doorOpenScaleX = 0.2f;
    [SerializeField] private float _doorClosedScaleX = 1f;
    [SerializeField] private float _doorOpenDuration = 0.45f;
    [SerializeField] private float _doorCloseDuration = 0.25f;
    [SerializeField] private Ease _doorOpenEase = Ease.OutCubic;
    [SerializeField] private Ease _doorCloseEase = Ease.OutCubic;

    [Header("Stand Check")]
    [SerializeField] private float _topContactTolerance = 0.2f;
    [SerializeField] private float _minHorizontalOverlap = 0.05f;

    [Header("Ride")]
    [SerializeField] private float _elevatorMoveSpeed = 1.2f;
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

    private void Awake()
    {
        if (_skeletonAnimation == null)
            _skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();

        BoxCollider2D bellTrigger = GetComponent<BoxCollider2D>();
        if (bellTrigger != null)
            bellTrigger.isTrigger = true;

        if (_elevatorRoot == null)
            _elevatorRoot = transform.parent;

        if (_doorTransform == null && _elevatorRoot != null)
        {
            Transform door = _elevatorRoot.Find("sense_laoshudong5_diantimen_0");
            if (door != null)
                _doorTransform = door;
        }

        if (_platformCollider == null && _elevatorRoot != null)
        {
            Transform platform = _elevatorRoot.Find("sense_laoshudong5_dianti1");
            if (platform != null)
                _platformCollider = platform.GetComponent<Collider2D>();
        }

        if (_backBlockCollider == null && _elevatorRoot != null)
        {
            Transform backBlock = _elevatorRoot.Find("sense_laoshudong5_dianti2(back)");
            if (backBlock != null)
                _backBlockCollider = backBlock.GetComponent<Collider2D>();
        }
    }

    private void Update()
    {
        ResolvePlayerReferences();

        if (!_doorOpened) return;
        if (_ridePreparing || _rideStarted || _sceneLoadRequested) return;

        if (IsPlayerStandingOnTopOfPlatform())
            CloseDoorThenStartRide();
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

    private void CloseDoorThenStartRide()
    {
        if (_ridePreparing)
            return;

        _ridePreparing = true;

        if (_doorTransform == null || _doorCloseDuration <= 0f)
        {
            SetDoorScaleX(_doorClosedScaleX);
            BeginRide();
            return;
        }

        KillDoorTween();

        _doorTween = _doorTransform
            .DOScaleX(_doorClosedScaleX, _doorCloseDuration)
            .SetEase(_doorCloseEase)
            .OnComplete(() =>
            {
                _doorTween = null;
                BeginRide();
            });
    }

    private void BeginRide()
    {
        ResolvePlayerReferences();

        if (_player == null)
        {
            _ridePreparing = false;
            return;
        }

        LockPlayerForRide();
        _doorOpened = false;
        _ridePreparing = false;
        _rideStarted = true;
    }

    private void MoveElevatorAndPlayerUp()
    {
        if (_elevatorRoot == null || _player == null)
        {
            FinishRide();
            return;
        }

        float remainingY = _stopPlayerY - _player.position.y;
        if (remainingY <= 0f)
        {
            FinishRide();
            return;
        }

        float stepY = Mathf.Min(_elevatorMoveSpeed * Time.fixedDeltaTime, remainingY);
        Vector3 delta = new Vector3(0f, stepY, 0f);

        _elevatorRoot.position += delta;

        if (_playerRigidbody != null)
        {
            _playerRigidbody.velocity = Vector2.zero;
            _playerRigidbody.angularVelocity = 0f;
            _playerRigidbody.MovePosition(_playerRigidbody.position + new Vector2(0f, stepY));
        }
        else
        {
            _player.position += delta;
        }

        if (_player.position.y >= _stopPlayerY - 0.001f)
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

        if (string.IsNullOrWhiteSpace(_targetScene)) return;

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

    private bool IsPlayerStandingOnTopOfPlatform()
    {
        if (_platformCollider == null || _playerCollider == null)
            return false;

        if (!_platformCollider.IsTouching(_playerCollider))
            return false;

        Bounds platformBounds = _platformCollider.bounds;
        Bounds playerBounds = _playerCollider.bounds;

        float verticalGap = Mathf.Abs(playerBounds.min.y - platformBounds.max.y);
        bool centerAbovePlatform = playerBounds.center.y >= platformBounds.center.y;
        bool closeToTopSurface = verticalGap <= _topContactTolerance;

        float overlapMinX = Mathf.Max(playerBounds.min.x, platformBounds.min.x);
        float overlapMaxX = Mathf.Min(playerBounds.max.x, platformBounds.max.x);
        bool hasHorizontalOverlap = (overlapMaxX - overlapMinX) > _minHorizontalOverlap;

        return centerAbovePlatform && closeToTopSurface && hasHorizontalOverlap;
    }

    private void ResolvePlayerReferences()
    {
        if (_player == null && GameManager.Instance != null)
            _player = GameManager.Instance.CurrentPlayer;

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
            _playerMovement.SetExternalInputLocked(true);

        if (_playerRigidbody != null)
        {
            _cachedGravityScale = _playerRigidbody.gravityScale;
            _cachedGravityValid = true;
            _playerRigidbody.velocity = Vector2.zero;
            _playerRigidbody.angularVelocity = 0f;
            _playerRigidbody.gravityScale = 0f;
        }
    }

    private void RestorePlayerState()
    {
        if (_playerMovement != null)
            _playerMovement.SetExternalInputLocked(false);

        if (_playerRigidbody != null)
        {
            _playerRigidbody.velocity = Vector2.zero;
            _playerRigidbody.angularVelocity = 0f;

            if (_cachedGravityValid)
                _playerRigidbody.gravityScale = _cachedGravityScale;
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
