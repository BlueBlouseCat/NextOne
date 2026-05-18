using System;
using System.Collections;
using Spine;
using Spine.Unity;
using UnityEngine;

public class CrowSpineController : MonoBehaviour
{
    [Header("Spine")]
    [SerializeField] private SkeletonAnimation _skeletonAnimation;

    [Header("Animations")]
    [SpineAnimation, SerializeField] private string _idleAnimation = "idel";
    [SpineAnimation, SerializeField] private string _speakAnimation = "speak";
    [SpineAnimation, SerializeField] private string _flyAnimation = "fly";
    [SpineAnimation, SerializeField] private string _downToGroundAnimation = "down to ground";

    [Header("Optional")]
    [SerializeField] private bool _disableRootOnLandComplete = false;
    [SerializeField] private float _landStopDistance = 0.02f;

    private string _currentAnimation;
    private bool _hasFlownAway;
    private Coroutine _moveRoutine;
    private Action _pendingMoveComplete;
    private TrackEntry _pendingLandEntry;
    private bool _disableRootAfterCurrentLand;
    private bool _playIdleAfterCurrentLand;

    private void Awake()
    {
        if (_skeletonAnimation == null)
            _skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();
    }

    private void Start()
    {
        PlayIdle();
    }

    public void PlayIdle()
    {
        if (_hasFlownAway) return;
        PlayAnimation(_idleAnimation, true, true);
    }

    public void PlaySpeak()
    {
        if (_hasFlownAway) return;
        PlayAnimation(_speakAnimation, true, true);
    }

    public void PlayFly()
    {
        _hasFlownAway = true;
        PlayAnimation(_flyAnimation, true, true);
    }

    public void FlyTo(Vector2 targetWorldPosition, float moveSpeed, Action onComplete = null)
    {
        StartMove(targetWorldPosition, moveSpeed, onComplete, false, false, false);
    }

    public void FlyToAndLand(Vector2 targetWorldPosition, float moveSpeed)
    {
        StartMove(targetWorldPosition, moveSpeed, null, true, _disableRootOnLandComplete, false);
    }

    public void FlyToAndLand(Vector2 targetWorldPosition, float moveSpeed, Action onComplete, bool playLandAnimation)
    {
        StartMove(targetWorldPosition, moveSpeed, onComplete, playLandAnimation, _disableRootOnLandComplete, false);
    }

    public void FlyToAndLand(
        Vector2 targetWorldPosition,
        float moveSpeed,
        Action onComplete,
        bool playLandAnimation,
        bool playIdleAfterLand)
    {
        StartMove(targetWorldPosition, moveSpeed, onComplete, playLandAnimation, _disableRootOnLandComplete, playIdleAfterLand);
    }

    public bool HasFlownAway()
    {
        return _hasFlownAway;
    }

    private void StartMove(
        Vector2 targetWorldPosition,
        float moveSpeed,
        Action onComplete,
        bool playLandAnimation,
        bool disableRootAfterLand,
        bool playIdleAfterLand)
    {
        if (_moveRoutine != null)
            StopCoroutine(_moveRoutine);

        ClearPendingCallbacks();

        _hasFlownAway = true;
        _pendingMoveComplete = onComplete;
        _disableRootAfterCurrentLand = disableRootAfterLand;
        _playIdleAfterCurrentLand = playIdleAfterLand;
        _moveRoutine = StartCoroutine(MoveRoutine(targetWorldPosition, Mathf.Max(0.01f, moveSpeed), playLandAnimation));
    }

    private IEnumerator MoveRoutine(Vector2 targetWorldPosition, float moveSpeed, bool playLandAnimation)
    {
        PlayAnimation(_flyAnimation, true, true);

        while (((Vector2)transform.position - targetWorldPosition).sqrMagnitude > _landStopDistance * _landStopDistance)
        {
            Vector2 nextPos = Vector2.MoveTowards(transform.position, targetWorldPosition, moveSpeed * Time.deltaTime);
            transform.position = new Vector3(nextPos.x, nextPos.y, transform.position.z);
            yield return null;
        }

        transform.position = new Vector3(targetWorldPosition.x, targetWorldPosition.y, transform.position.z);
        _moveRoutine = null;

        if (playLandAnimation && !string.IsNullOrEmpty(_downToGroundAnimation))
        {
            TrackEntry landEntry = PlayAnimation(_downToGroundAnimation, false, true);
            if (landEntry != null)
            {
                _pendingLandEntry = landEntry;
                landEntry.Complete += OnLandComplete;
                yield break;
            }
        }

        CompleteMove();
    }

    private void OnLandComplete(TrackEntry entry)
    {
        if (_pendingLandEntry != entry)
            return;

        entry.Complete -= OnLandComplete;
        _pendingLandEntry = null;

        CompleteMove();
    }

    private void CompleteMove()
    {
        if (_playIdleAfterCurrentLand && !_disableRootAfterCurrentLand)
        {
            _hasFlownAway = false;
            PlayIdle();
        }

        Action callback = _pendingMoveComplete;
        _pendingMoveComplete = null;

        if (_disableRootAfterCurrentLand)
            gameObject.SetActive(false);

        callback?.Invoke();
    }

    private void ClearPendingCallbacks()
    {
        if (_pendingLandEntry != null)
        {
            _pendingLandEntry.Complete -= OnLandComplete;
            _pendingLandEntry = null;
        }

        _pendingMoveComplete = null;
        _disableRootAfterCurrentLand = false;
        _playIdleAfterCurrentLand = false;
    }

    private TrackEntry PlayAnimation(string animName, bool loop, bool restart = false)
    {
        if (_skeletonAnimation == null || _skeletonAnimation.AnimationState == null) return null;
        if (string.IsNullOrEmpty(animName)) return null;

        if (!restart && _currentAnimation == animName)
            return _skeletonAnimation.AnimationState.GetCurrent(0);

        _currentAnimation = animName;
        return _skeletonAnimation.AnimationState.SetAnimation(0, animName, loop);
    }
}
