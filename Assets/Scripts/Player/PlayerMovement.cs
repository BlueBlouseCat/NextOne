using Spine;
using Spine.Unity;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D _rb;
    [SerializeField] private SkeletonAnimation _sa;

    [Header("Animation")]
    [SpineAnimation] public string idleAnimation;
    [SpineAnimation] public string walkAnimation;
    [SpineAnimation] public string climbAnimation;
    [SpineAnimation] public string jumpAnimation;
    [SpineAnimation] public string wakeUpAAnimation;
    private string _currentAnimation;

    [Header("Move")]
    [SerializeField] private float _moveSpeed = 5f;
    private Vector2 _moveInput;
    private bool _isWalking;
    private float _currentFaceDir = 1f;

    [Header("Climb")]
    [SerializeField] private float _climbSpeed = 3f;
    [SerializeField] private float _headOffset = 0.05f;
    [SerializeField] private float _footOffset = 0.05f;
    [SerializeField] private bool _snapXEveryFrame = true;
    [SerializeField, Range(0f, 0.2f)] private float _verticalDeadZone = 0.1f;
    private bool _isClimbing;
    private bool _canClimb;
    private float _originalGravity;
    private Collider2D _currentZone;
    private float _zoneCenterX;
    private float _zoneTopY;
    private float _zoneBottomY;
    private float _topEnterTolerance = 0.4f;
    private float _bottomExitTolerance = 0.08f;
    public bool IsClimbing => _isClimbing;

    [Header("MouseHole7 Top Exit")]
    [SerializeField] private bool _enableTopHorizontalExit = true;
    [SerializeField] private string _topHorizontalExitScene = "MouseHole7";
    [SerializeField, Range(0.01f, 0.5f)] private float _topHorizontalExitTolerance = 0.15f;

    [Header("Jump")]
    [SerializeField] private float _climbJumpHorizontalSpeed = 4f;
    [SerializeField] private float _climbJumpVerticalSpeed = 7f;
    [SerializeField] private float _jumpForce = 1f;
    [SerializeField] private float _jumpHorizontalSpeed = 4f;
    [SerializeField] private Transform _groundCheck;
    [SerializeField] private float _groundCheckRadius = 0.1f;
    [SerializeField] private LayerMask _groundLayer;
    private bool _isJumping;
    private bool _isGrounded;
    private bool _wasGroundedLastFrame;

    [Header("Launch")]
    private bool _isLaunched;
    private float _launchLockTimer;

    [Header("Wake Up")]
    private bool _isWakingUp;
    private bool _hasLaunchedThisAirborne;

    [Header("External Lock")]
    private bool _externalInputLocked;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _originalGravity = _rb.gravityScale;

        if (GameManager.Instance != null)
            GameManager.Instance.RegisterPlayer(transform);
    }

    private void Start()
    {
        PlayAnimation(idleAnimation, true);
    }

    private void FixedUpdate()
    {
        _isGrounded = Physics2D.OverlapCircle(
            _groundCheck.position,
            _groundCheckRadius,
            _groundLayer
        );

        bool justLanded = !_wasGroundedLastFrame && _isGrounded;
        _wasGroundedLastFrame = _isGrounded;

        if (_isLaunched)
        {
            _launchLockTimer -= Time.fixedDeltaTime;
            if (_launchLockTimer <= 0f)
                _launchLockTimer = 0f;
        }

        if (justLanded)
        {
            if (_isLaunched)
            {
                _isLaunched = false;
                PlayWakeUpA();
                return;
            }

            if (!_isClimbing && !_isWakingUp)
            {
                _isJumping = false;
                UpdateMovement();
            }
        }

        if (_isWakingUp) return;

        if (_externalInputLocked)
        {
            _rb.velocity = new Vector2(0f, _rb.velocity.y);
            PlayAnimation(idleAnimation, true);
            return;
        }

        UpdateClimb();

        if (_isClimbing) return;
        if (_isLaunched) return;

        UpdateMovement();
        _rb.velocity = new Vector2(_moveInput.x * _moveSpeed, _rb.velocity.y);
    }

    public TrackEntry PlayAnimation(string animName, bool isLoop, bool restart = false)
    {
        if (_sa == null || _sa.AnimationState == null) return null;
        if (string.IsNullOrEmpty(animName)) return null;

        if (!restart && _currentAnimation == animName)
            return _sa.AnimationState.GetCurrent(0);

        _currentAnimation = animName;
        TrackEntry entry = _sa.AnimationState.SetAnimation(0, animName, isLoop);
        entry.TimeScale = 1f;
        return entry;
    }

    public void Move(InputAction.CallbackContext context)
    {
        if (_externalInputLocked) return;

        _moveInput = context.ReadValue<Vector2>();

        if (context.canceled)
            _moveInput = Vector2.zero;

        if (_isWakingUp || _isLaunched) return;

        if (_isClimbing || _isJumping)
            SetFacing(_moveInput.x);

        if (!_isClimbing && !_isJumping)
            UpdateMovement();
    }

    private void UpdateMovement()
    {
        if (_isClimbing || _isJumping) return;

        if (Mathf.Abs(_moveInput.x) > 0.01f)
        {
            _isWalking = true;
            PlayAnimation(walkAnimation, true);
            SetFacing(_moveInput.x);
        }
        else
        {
            _isWalking = false;
            PlayAnimation(idleAnimation, true);
        }
    }

    private void UpdateClimb()
    {
        if (!_canClimb) return;

        float v = _moveInput.y;
        float h = _moveInput.x;

        float bottomY = _zoneBottomY + _footOffset;
        float topY = _zoneTopY - _headOffset;

        bool pressUp = v > _verticalDeadZone;
        bool pressDown = v < -_verticalDeadZone;
        bool pressHorizontal = Mathf.Abs(h) > 0.01f;

        bool nearTop = _rb.position.y >= topY - _topEnterTolerance;
        bool nearBottom = _rb.position.y <= bottomY + _bottomExitTolerance;

        if (!_isClimbing)
        {
            if (pressUp)
            {
                StartClimb();
            }
            else if (pressDown && nearTop)
            {
                StartClimb();
            }
        }

        if (!_isClimbing) return;

        bool allowTopHorizontalExit =
            _enableTopHorizontalExit &&
            SceneManager.GetActiveScene().name == _topHorizontalExitScene;

        bool nearTopForHorizontalExit =
            _rb.position.y >= topY - _topHorizontalExitTolerance;

        if (allowTopHorizontalExit && nearTopForHorizontalExit && pressHorizontal)
        {
            ExitClimbToTopWalk();
            return;
        }

        if (pressDown && nearBottom)
        {
            StopClimb();
            return;
        }

        float targetY = _rb.position.y + v * _climbSpeed * Time.fixedDeltaTime;
        float y = Mathf.Clamp(targetY, bottomY, topY);
        float x = _snapXEveryFrame ? _zoneCenterX : _rb.position.x;

        _rb.MovePosition(new Vector2(x, y));
        _rb.velocity = Vector2.zero;

        TrackEntry track = PlayAnimation(climbAnimation, true);
        if (track != null)
            track.TimeScale = Mathf.Abs(v) > _verticalDeadZone ? 1f : 0f;
    }

    private void StartClimb()
    {
        if (_isClimbing) return;

        _isClimbing = true;
        _isJumping = false;

        _rb.gravityScale = 0f;
        _rb.velocity = Vector2.zero;
        _rb.position = new Vector2(_zoneCenterX, _rb.position.y);

        TrackEntry track = PlayAnimation(climbAnimation, true, true);
        if (track != null)
            track.TimeScale = 1f;
    }

    private void StopClimb()
    {
        if (!_isClimbing) return;

        _isClimbing = false;
        _rb.gravityScale = _originalGravity;
        _rb.velocity = Vector2.zero;

        UpdateMovement();
    }

    private void ExitClimbToTopWalk()
    {
        float horizontal = _moveInput.x;

        StopClimb();

        if (Mathf.Abs(horizontal) > 0.01f)
        {
            SetFacing(horizontal);
            _rb.velocity = new Vector2(horizontal * _moveSpeed, _rb.velocity.y);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.isTrigger) return;
        if (!other.CompareTag("Climbable")) return;

        _currentZone = other;
        Bounds b = other.bounds;
        _zoneCenterX = b.center.x;
        _zoneTopY = b.max.y;
        _zoneBottomY = b.min.y;
        _canClimb = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other != _currentZone) return;

        _canClimb = false;
        if (_isClimbing) StopClimb();
        _currentZone = null;
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (_externalInputLocked) return;
        if (!context.performed) return;
        if (_isWakingUp || _isLaunched) return;

        if (_isClimbing)
        {
            JumpFromClimb();
            return;
        }

        NormalJump();
    }

    private void JumpFromClimb()
    {
        _isClimbing = false;
        _rb.gravityScale = _originalGravity;

        float jumpDir = Mathf.Abs(_moveInput.x) > 0.01f ? Mathf.Sign(_moveInput.x) : _currentFaceDir;
        SetFacing(jumpDir);

        _rb.velocity = new Vector2(jumpDir * _climbJumpHorizontalSpeed, _climbJumpVerticalSpeed);

        _isJumping = true;
        PlayAnimation(jumpAnimation, false, true);
    }

    private void NormalJump()
    {
        if (!_isGrounded) return;
        if (_isJumping) return;

        _isJumping = true;

        float jumpDir = Mathf.Abs(_moveInput.x) > 0.01f ? Mathf.Sign(_moveInput.x) : _currentFaceDir;
        SetFacing(jumpDir);

        _rb.velocity = new Vector2(jumpDir * _jumpHorizontalSpeed, _jumpForce);

        TrackEntry jumpTrack = PlayAnimation(jumpAnimation, false, true);
        if (jumpTrack != null)
            jumpTrack.Complete += OnJumpAnimationComplete;
    }

    private void OnJumpAnimationComplete(TrackEntry trackEntry)
    {
        if (_currentAnimation != jumpAnimation) return;
        UpdateMovement();
    }

    public void Launch(Vector2 launchVelocity, float controlLockTime)
    {
        if (_isLaunched || _isWakingUp) return;

        _isClimbing = false;
        _canClimb = false;

        _rb.gravityScale = _originalGravity;
        _rb.velocity = launchVelocity;

        _isJumping = true;
        _isLaunched = true;
        _hasLaunchedThisAirborne = true;
        _launchLockTimer = controlLockTime;

        SetFacing(launchVelocity.x);

        _currentAnimation = null;
        _sa.AnimationState.ClearTrack(0);
    }

    private void PlayWakeUpA()
    {
        _isWakingUp = true;
        _isJumping = false;

        TrackEntry wakeTrack = PlayAnimation(wakeUpAAnimation, false, true);
        if (wakeTrack != null)
            wakeTrack.Complete += OnWakeUpAComplete;
    }

    private void OnWakeUpAComplete(TrackEntry trackEntry)
    {
        if (_currentAnimation != wakeUpAAnimation) return;

        _isWakingUp = false;
        _hasLaunchedThisAirborne = false;
        UpdateMovement();
    }

    private void SetFacing(float horizontalInput)
    {
        if (Mathf.Abs(horizontalInput) <= 0.01f) return;

        float targetDir = horizontalInput > 0 ? 1f : -1f;
        if (_currentFaceDir == targetDir) return;

        _currentFaceDir = targetDir;

        if (_sa != null && _sa.skeleton != null)
            _sa.skeleton.ScaleX = _currentFaceDir;
    }

    public void SetExternalInputLocked(bool locked)
    {
        _externalInputLocked = locked;

        if (locked)
        {
            _moveInput = Vector2.zero;
            _rb.velocity = new Vector2(0f, _rb.velocity.y);
        }
    }
}
