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

    [Header("Startup")]
    [SerializeField] private bool _setDefaultAnimationOnAwake = true;

    [Header("Disappear")]
    [SerializeField] private bool _reverseDisappearTrack = true;

    [Header("Mix")]
    [SerializeField, Min(0f)] private float _defaultMix = 0.06f;
    [SerializeField, Min(0f)] private float _appearToIdleCatMix = 0.04f;
    [SerializeField, Min(0f)] private float _idleCatToTalkMix = 0.03f;
    [SerializeField, Min(0f)] private float _talkToDisappearMix = 0.03f;
    [SerializeField, Min(0f)] private float _disappearToIdleLouMix = 0.05f;

    private string _currentAnimation;
    private bool _isAppearing;
    private bool _isDisappearing;

    private TrackEntry _appearTrack;
    private TrackEntry _appearIdleTrack;
    private TrackEntry _disappearTrack;
    private TrackEntry _disappearIdleTrack;

    private Action _appearCompleteCallback;
    private Action _disappearCompleteCallback;

    public bool IsAppearing => _isAppearing;
    public bool IsDisappearing => _isDisappearing;

    private void Awake()
    {
        if (_skeletonAnimation == null)
            _skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();

        if (_setDefaultAnimationOnAwake)
            InitializeDefaultAnimation();
    }

    private void Start()
    {
        ApplyMixSettings();
        ResetToIdleLou();
    }

    private void OnDisable()
    {
        ClearCallbacks();
    }

    private void InitializeDefaultAnimation()
    {
        if (_skeletonAnimation == null)
            return;

        if (string.IsNullOrWhiteSpace(_idleLouAnimation))
            return;

        _skeletonAnimation.AnimationName = _idleLouAnimation;
        _skeletonAnimation.loop = true;
    }

    private void ApplyMixSettings()
    {
        if (_skeletonAnimation == null || _skeletonAnimation.AnimationState == null)
            return;

        AnimationStateData data = _skeletonAnimation.AnimationState.Data;
        if (data == null)
            return;

        data.DefaultMix = _defaultMix;

        SetMixIfValid(data, _appearAnimation, _idleCatAnimation, _appearToIdleCatMix);
        SetMixIfValid(data, _idleCatAnimation, _talkAnimation, _idleCatToTalkMix);
        SetMixIfValid(data, _talkAnimation, _disappearAnimation, _talkToDisappearMix);
        SetMixIfValid(data, _disappearAnimation, _idleLouAnimation, _disappearToIdleLouMix);
    }

    private void SetMixIfValid(AnimationStateData data, string from, string to, float duration)
    {
        if (data == null) return;
        if (string.IsNullOrWhiteSpace(from)) return;
        if (string.IsNullOrWhiteSpace(to)) return;

        data.SetMix(from, to, Mathf.Max(0f, duration));
    }

    public void ResetToIdleLou()
    {
        ClearCallbacks();
        ApplyMixSettings();

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

        if (_skeletonAnimation == null || _skeletonAnimation.AnimationState == null)
        {
            FinishAppearFallback(onComplete);
            return;
        }

        ClearAppearCallback();
        ApplyMixSettings();

        _isAppearing = true;
        _appearCompleteCallback = onComplete;

        Spine.AnimationState state = _skeletonAnimation.AnimationState;

        _currentAnimation = _appearAnimation;
        _appearTrack = state.SetAnimation(0, _appearAnimation, false);

        if (_appearTrack == null)
        {
            FinishAppearFallback(onComplete);
            return;
        }

        _appearTrack.Reverse = false;

        _appearIdleTrack = state.AddAnimation(0, _idleCatAnimation, true, 0f);
        if (_appearIdleTrack == null)
        {
            FinishAppearFallback(onComplete);
            return;
        }

        _appearIdleTrack.Start += OnAppearIdleStarted;
    }

    public void PlayDisappearThenIdleLou(Action onComplete = null)
    {
        if (_isDisappearing)
            return;

        if (_skeletonAnimation == null || _skeletonAnimation.AnimationState == null)
        {
            FinishDisappearFallback(onComplete);
            return;
        }

        ClearDisappearCallback();
        ApplyMixSettings();

        _isAppearing = false;
        _isDisappearing = true;
        _disappearCompleteCallback = onComplete;

        Spine.AnimationState state = _skeletonAnimation.AnimationState;

        _currentAnimation = _disappearAnimation;
        _disappearTrack = state.SetAnimation(0, _disappearAnimation, false);

        if (_disappearTrack == null)
        {
            FinishDisappearFallback(onComplete);
            return;
        }

        _disappearTrack.Reverse = _reverseDisappearTrack;

        _disappearIdleTrack = state.AddAnimation(0, _idleLouAnimation, true, 0f);
        if (_disappearIdleTrack == null)
        {
            FinishDisappearFallback(onComplete);
            return;
        }

        _disappearIdleTrack.Start += OnDisappearIdleStarted;
    }

    private void OnAppearIdleStarted(TrackEntry entry)
    {
        if (_appearIdleTrack != entry)
            return;

        entry.Start -= OnAppearIdleStarted;
        _appearIdleTrack = null;
        _appearTrack = null;

        _isAppearing = false;
        _currentAnimation = _idleCatAnimation;

        Action callback = _appearCompleteCallback;
        _appearCompleteCallback = null;
        callback?.Invoke();
    }

    private void OnDisappearIdleStarted(TrackEntry entry)
    {
        if (_disappearIdleTrack != entry)
            return;

        entry.Start -= OnDisappearIdleStarted;
        _disappearIdleTrack = null;
        _disappearTrack = null;

        _isDisappearing = false;
        _currentAnimation = _idleLouAnimation;

        Action callback = _disappearCompleteCallback;
        _disappearCompleteCallback = null;
        callback?.Invoke();
    }

    private void FinishAppearFallback(Action onComplete)
    {
        _isAppearing = false;
        PlayIdleCat();
        onComplete?.Invoke();
    }

    private void FinishDisappearFallback(Action onComplete)
    {
        _isDisappearing = false;
        PlayIdleLou();
        onComplete?.Invoke();
    }

    private void ClearCallbacks()
    {
        ClearAppearCallback();
        ClearDisappearCallback();
    }

    private void ClearAppearCallback()
    {
        if (_appearIdleTrack != null)
        {
            _appearIdleTrack.Start -= OnAppearIdleStarted;
            _appearIdleTrack = null;
        }

        _appearTrack = null;
        _appearCompleteCallback = null;
    }

    private void ClearDisappearCallback()
    {
        if (_disappearIdleTrack != null)
        {
            _disappearIdleTrack.Start -= OnDisappearIdleStarted;
            _disappearIdleTrack = null;
        }

        _disappearTrack = null;
        _disappearCompleteCallback = null;
    }

    private TrackEntry PlayAnimation(string animName, bool loop, bool restart = false)
    {
        if (_skeletonAnimation == null || _skeletonAnimation.AnimationState == null)
            return null;

        if (string.IsNullOrWhiteSpace(animName))
            return null;

        if (!restart && _currentAnimation == animName)
            return _skeletonAnimation.AnimationState.GetCurrent(0);

        _currentAnimation = animName;
        return _skeletonAnimation.AnimationState.SetAnimation(0, animName, loop);
    }
}