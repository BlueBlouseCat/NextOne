using System;
using Spine;
using Spine.Unity;
using UnityEngine;

public class LiangyilouSpineController : MonoBehaviour
{
    [Header("Spine")]
    [SerializeField] private SkeletonAnimation _skeletonAnimation;

    [Header("Animations")]
    [SpineAnimation, SerializeField] private string _idleLouAnimation = "idle-lou";
    [SpineAnimation, SerializeField] private string _idleCatAnimation = "idle-cat";
    [SpineAnimation, SerializeField] private string _appearAnimation = "appear";
    [SpineAnimation, SerializeField] private string _talkAnimation = "talk";
    [SpineAnimation, SerializeField] private string _disappearAnimation = "disappear";

    private string _currentAnimation;
    private bool _isAppearing;
    private bool _isDisappearing;

    private TrackEntry _appearTrack;
    private TrackEntry _disappearTrack;

    private Action _appearCompleteCallback;
    private Action _disappearCompleteCallback;

    public bool IsAppearing => _isAppearing;
    public bool IsDisappearing => _isDisappearing;

    private void Awake()
    {
        if (_skeletonAnimation == null)
            _skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();
    }

    private void Start()
    {
        ResetToIdleLou();
    }

    private void OnDisable()
    {
        ClearCallbacks();
    }

    public void ResetToIdleLou()
    {
        ClearCallbacks();
        _isAppearing = false;
        _isDisappearing = false;
        PlayAnimation(_idleLouAnimation, true, true);
    }

    public void PlayIdleLou()
    {
        if (_isDisappearing) return;
        PlayAnimation(_idleLouAnimation, true, true);
    }

    public void PlayIdleCat()
    {
        if (_isDisappearing) return;
        PlayAnimation(_idleCatAnimation, true, true);
    }

    public void PlayTalk()
    {
        if (_isDisappearing) return;
        PlayAnimation(_talkAnimation, true, true);
    }

    public void PlayAppearThenIdleCat(Action onComplete = null)
    {
        if (_isAppearing || _isDisappearing)
            return;

        ClearAppearCallback();

        _isAppearing = true;
        _appearCompleteCallback = onComplete;

        _appearTrack = PlayAnimation(_appearAnimation, false, true);
        if (_appearTrack == null)
        {
            FinishAppear();
            return;
        }

        _appearTrack.Complete += OnAppearComplete;
    }

    public void PlayDisappearThenIdleLou(Action onComplete = null)
    {
        if (_isDisappearing)
            return;

        ClearDisappearCallback();

        _isAppearing = false;
        _isDisappearing = true;
        _disappearCompleteCallback = onComplete;

        _disappearTrack = PlayAnimation(_disappearAnimation, false, true, true);
        if (_disappearTrack == null)
        {
            FinishDisappear();
            return;
        }

        _disappearTrack.Reverse = true;
        _disappearTrack.Complete += OnDisappearComplete;
    }

    private void OnAppearComplete(TrackEntry entry)
    {
        if (_appearTrack != entry)
            return;

        entry.Complete -= OnAppearComplete;
        _appearTrack = null;

        FinishAppear();
    }

    private void FinishAppear()
    {
        _isAppearing = false;
        PlayIdleCat();

        Action callback = _appearCompleteCallback;
        _appearCompleteCallback = null;
        callback?.Invoke();
    }

    private void OnDisappearComplete(TrackEntry entry)
    {
        if (_disappearTrack != entry)
            return;

        entry.Complete -= OnDisappearComplete;
        _disappearTrack = null;

        FinishDisappear();
    }

    private void FinishDisappear()
    {
        _isDisappearing = false;
        PlayIdleLou();

        Action callback = _disappearCompleteCallback;
        _disappearCompleteCallback = null;
        callback?.Invoke();
    }

    private void ClearCallbacks()
    {
        ClearAppearCallback();
        ClearDisappearCallback();
    }

    private void ClearAppearCallback()
    {
        if (_appearTrack != null)
        {
            _appearTrack.Complete -= OnAppearComplete;
            _appearTrack = null;
        }

        _appearCompleteCallback = null;
    }

    private void ClearDisappearCallback()
    {
        if (_disappearTrack != null)
        {
            _disappearTrack.Complete -= OnDisappearComplete;
            _disappearTrack = null;
        }

        _disappearCompleteCallback = null;
    }

    private TrackEntry PlayAnimation(string animName, bool loop, bool restart = false, bool clearReverse = false)
    {
        if (_skeletonAnimation == null || _skeletonAnimation.AnimationState == null)
            return null;

        if (string.IsNullOrWhiteSpace(animName))
            return null;

        if (!restart && _currentAnimation == animName)
            return _skeletonAnimation.AnimationState.GetCurrent(0);

        _currentAnimation = animName;

        TrackEntry entry = _skeletonAnimation.AnimationState.SetAnimation(0, animName, loop);
        if (entry != null && clearReverse)
            entry.Reverse = false;

        return entry;
    }
}
