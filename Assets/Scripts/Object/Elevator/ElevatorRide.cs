using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ElevatorRide : MonoBehaviour
{
    [Header("Scene Limit")]
    [SerializeField] private string _currentScene = "MouseHole6";

    [Header("References")]
    [SerializeField] private Transform _elevatorRoot;
    [SerializeField] private Collider2D _platformCollider;
    [SerializeField] private string _playerTag = "Player";

    [Header("Start Condition")]
    [SerializeField] private bool _requirePlayerStandingOnPlatform = true;
    [SerializeField] private float _topContactTolerance = 0.2f;
    [SerializeField] private float _minHorizontalOverlap = 0.05f;

    [Header("Ride")]
    [SerializeField] private float _moveSpeed = 1.2f;
    [SerializeField] private float _stopPlayerY = 4.96f;

    [Header("Target Scene")]
    [SerializeField] private string _targetScene = "House";
    [SerializeField] private string _targetSpawnPointId = "hole6_to_house";
    [SerializeField] private bool _useFade = false;

    private Transform _player;
    private Collider2D _playerCollider;
    private Rigidbody2D _playerRigidbody;
    private PlayerMovement _playerMovement;

    private bool _isInitialized;
    private bool _isRiding;
    private bool _sceneLoadRequested;

    private float _cachedGravityScale;
    private bool _cachedGravityValid;

    private void Awake()
    {
        if (_elevatorRoot == null)
            _elevatorRoot = transform;

        if (_platformCollider == null)
        {
            Transform platform = _elevatorRoot.Find("sense_laoshudong5_dianti1");
            if (platform != null)
                _platformCollider = platform.GetComponent<Collider2D>();
        }
    }

    private void OnEnable()
    {
        StartCoroutine(InitializeRoutine());
    }

    private void FixedUpdate()
    {
        if (!_isRiding) return;
        if (_sceneLoadRequested) return;

        MoveElevatorAndPlayerUp();
    }

    private void OnDisable()
    {
        RestorePlayerState();
    }

    private IEnumerator InitializeRoutine()
    {
        if (SceneManager.GetActiveScene().name != _currentScene)
            yield break;

        while (GameManager.Instance == null || GameManager.Instance.CurrentPlayer == null)
            yield return null;

        ResolvePlayerReferences();
        _isInitialized = _player != null;

        if (!_isInitialized)
            yield break;

        if (_requirePlayerStandingOnPlatform)
        {
            while (_player != null && !IsPlayerStandingOnPlatform())
                yield return null;
        }

        StartRide();
    }

    private void StartRide()
    {
        if (_isRiding) return;
        if (_sceneLoadRequested) return;

        ResolvePlayerReferences();
        if (_player == null) return;

        LockPlayerForRide();
        _isRiding = true;
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

        float stepY = Mathf.Min(_moveSpeed * Time.fixedDeltaTime, remainingY);
        Vector3 delta = new Vector3(0f, stepY, 0f);

        _elevatorRoot.position += delta;

        if (_playerRigidbody != null)
        {
            Vector2 currentVelocity = _playerRigidbody.velocity;
            _playerRigidbody.velocity = new Vector2(currentVelocity.x, 0f);
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
        _isRiding = false;
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

    private bool IsPlayerStandingOnPlatform()
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
            _playerMovement.SetJumpInputLocked(true);

        if (_playerRigidbody != null)
        {
            _cachedGravityScale = _playerRigidbody.gravityScale;
            _cachedGravityValid = true;

            _playerRigidbody.angularVelocity = 0f;
            _playerRigidbody.gravityScale = 0f;
        }
    }

    private void RestorePlayerState()
    {
        if (_playerMovement != null)
            _playerMovement.SetJumpInputLocked(false);

        if (_playerRigidbody != null)
        {
            _playerRigidbody.velocity = new Vector2(_playerRigidbody.velocity.x, 0f);
            _playerRigidbody.angularVelocity = 0f;

            if (_cachedGravityValid)
                _playerRigidbody.gravityScale = _cachedGravityScale;
        }
    }
}
