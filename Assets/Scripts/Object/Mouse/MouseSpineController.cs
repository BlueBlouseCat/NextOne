using System;
using Spine;
using Spine.Unity;
using UnityEngine;

public class MouseSpineController : MonoBehaviour
{
    [Header("Spine")]
    [SerializeField] private SkeletonAnimation _skeletonAnimation;

    [Header("Animations")]
    [SpineAnimation, SerializeField] private string _idleAnimation = "main sense";
    [SpineAnimation, SerializeField] private string _disappearAnimation = "disapper";
    [SpineAnimation, SerializeField] private string _hungryAnimation = "idel-e";

    [Header("Optional")]
    [SerializeField] private bool _disableRootAfterDisappear = true;
    [SerializeField] private string _disappearedFlag = "house_mouse_disappeared";

    private string _currentAnimation;
    private bool _hasDisappeared;
    private TrackEntry _disappearTrack;
    private Spine.AnimationState.TrackEntryDelegate _disappearCompleteHandler;
    private Action _pendingDisappearCallback;

    public bool HasDisappeared => _hasDisappeared;

    private void Awake()
    {
        if (_skeletonAnimation == null)
            _skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();
    }

    private void Start()
    {
        bool alreadyDisappeared = GameManager.Instance != null && GameManager.Instance.GetFlag(_disappearedFlag);

        if (alreadyDisappeared)
        {
            _hasDisappeared = true;

            if (_disableRootAfterDisappear)
                gameObject.SetActive(false);

            return;
        }

        PlayIdle();
    }

    public void PlayIdle()
    {
        if (_hasDisappeared) return;
        PlayAnimation(_idleAnimation, true, true);
    }

    public void PlayIdle(string animationName)
    {
        if (_hasDisappeared) return;

        if (string.IsNullOrWhiteSpace(animationName))
        {
            PlayIdle();
            return;
        }

        PlayAnimation(animationName, true, true);
    }

    public void PlayDisappear(Action onComplete = null)
    {
        if (_hasDisappeared)
        {
            onComplete?.Invoke();
            return;
        }

        _hasDisappeared = true;

        if (GameManager.Instance != null && !string.IsNullOrWhiteSpace(_disappearedFlag))
            GameManager.Instance.SetFlag(_disappearedFlag, true);

        _pendingDisappearCallback = onComplete;
        _disappearTrack = PlayAnimation(_disappearAnimation, false, true);

        if (_disappearTrack == null)
        {
            _pendingDisappearCallback?.Invoke();
            _pendingDisappearCallback = null;

            if (_disableRootAfterDisappear)
                gameObject.SetActive(false);

            return;
        }

        _disappearCompleteHandler = OnDisappearComplete;
        _disappearTrack.Complete += _disappearCompleteHandler;
    }

    private void OnDisappearComplete(TrackEntry entry)
    {
        if (_disappearTrack != entry) return;

        if (_disappearTrack != null && _disappearCompleteHandler != null)
            _disappearTrack.Complete -= _disappearCompleteHandler;

        _disappearCompleteHandler = null;
        _disappearTrack = null;

        _pendingDisappearCallback?.Invoke();
        _pendingDisappearCallback = null;

        if (_disableRootAfterDisappear)
            gameObject.SetActive(false);
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
