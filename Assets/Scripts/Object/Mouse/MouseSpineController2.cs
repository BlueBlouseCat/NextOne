using System;
using Spine;
using Spine.Unity;
using UnityEngine;

public class MouseSpineController2 : MonoBehaviour
{
    [Header("Spine")]
    [SerializeField] private SkeletonAnimation _skeletonAnimation;

    [Header("Animation")]
    [SpineAnimation, SerializeField] private string _idleAnimation = "idel-e";
    [SpineAnimation, SerializeField] private string _fedIdleAnimation = "idel-bao";
    [SpineAnimation, SerializeField] private string _eatAnimation = "eat";

    [Header("State Flag")]
    [SerializeField] private string _fedFlag = "mousehole2_bread_delivered";

    private string _currentAnimation;
    private bool _isFed;
    private TrackEntry _eatTrack;
    private Spine.AnimationState.TrackEntryDelegate _eatCompleteHandler;
    private Action _pendingEatCallback;

    private void Awake()
    {
        if (_skeletonAnimation == null)
            _skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();
    }

    private void Start()
    {
        RefreshFedStateFromFlag();
        PlayIdle();
    }

    private void OnDisable()
    {
        DetachEatCompleteHandler();
        _pendingEatCallback = null;
    }

    public void RefreshFedStateFromFlag()
    {
        _isFed = GameManager.Instance != null && GameManager.Instance.GetFlag(_fedFlag);
    }

    public void PlayIdle()
    {
        RefreshFedStateFromFlag();

        string targetAnimation = _isFed ? _fedIdleAnimation : _idleAnimation;
        PlayAnimation(targetAnimation, true, false);
    }

    public void PlayFedIdle()
    {
        _isFed = true;
        PlayAnimation(_fedIdleAnimation, true, true);
    }

    public void PlayEatThenFedIdle(Action onComplete = null)
    {
        _isFed = true;

        if (GameManager.Instance != null && !string.IsNullOrWhiteSpace(_fedFlag))
            GameManager.Instance.SetFlag(_fedFlag, true);

        if (_skeletonAnimation == null || _skeletonAnimation.AnimationState == null || string.IsNullOrWhiteSpace(_eatAnimation))
        {
            PlayFedIdle();
            onComplete?.Invoke();
            return;
        }

        DetachEatCompleteHandler();
        _pendingEatCallback = onComplete;

        _eatTrack = PlayAnimation(_eatAnimation, false, true);
        if (_eatTrack == null)
        {
            PlayFedIdle();
            _pendingEatCallback?.Invoke();
            _pendingEatCallback = null;
            return;
        }

        _eatCompleteHandler = OnEatComplete;
        _eatTrack.Complete += _eatCompleteHandler;
    }

    private void OnEatComplete(TrackEntry entry)
    {
        if (_eatTrack != entry) return;

        DetachEatCompleteHandler();
        PlayFedIdle();

        _pendingEatCallback?.Invoke();
        _pendingEatCallback = null;
    }

    private void DetachEatCompleteHandler()
    {
        if (_eatTrack != null && _eatCompleteHandler != null)
            _eatTrack.Complete -= _eatCompleteHandler;

        _eatTrack = null;
        _eatCompleteHandler = null;
    }

    private TrackEntry PlayAnimation(string animationName, bool loop, bool restart)
    {
        if (_skeletonAnimation == null || _skeletonAnimation.AnimationState == null) return null;
        if (string.IsNullOrWhiteSpace(animationName)) return null;

        if (!restart && _currentAnimation == animationName)
            return _skeletonAnimation.AnimationState.GetCurrent(0);

        _currentAnimation = animationName;
        return _skeletonAnimation.AnimationState.SetAnimation(0, animationName, loop);
    }
}
