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
    [SpineAnimation, SerializeField] private string _disappearAnimation = "disappear";

    [Header("State Flag")]
    [SerializeField] private string _fedFlag = "mousehole2_bread_delivered";
    [SerializeField] private string _disappearedFlag = "mousehole2_mouse_disappeared";

    [Header("Disappear")]
    [SerializeField] private GameObject _hideTarget;
    [SerializeField] private bool _disableCollidersOnDisappear = true;
    [SerializeField] private Collider2D[] _collidersToDisable;

    private string _currentAnimation;
    private bool _isFed;
    private bool _hasDisappeared;

    private TrackEntry _eatTrack;
    private Spine.AnimationState.TrackEntryDelegate _eatCompleteHandler;
    private Action _pendingEatCallback;

    private TrackEntry _disappearTrack;
    private Spine.AnimationState.TrackEntryDelegate _disappearCompleteHandler;
    private Action _pendingDisappearCallback;

    public bool HasDisappeared => _hasDisappeared;

    private void Awake()
    {
        if (_skeletonAnimation == null)
            _skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();

        if (_hideTarget == null)
            _hideTarget = gameObject;
    }

    private void Start()
    {
        RefreshStateFromFlags();

        if (_hasDisappeared)
        {
            ApplyDisappearedStateImmediate();
            return;
        }

        PlayIdle();
    }

    private void OnDisable()
    {
        DetachEatCompleteHandler();
        DetachDisappearCompleteHandler();
        _pendingEatCallback = null;
        _pendingDisappearCallback = null;
    }

    public void RefreshStateFromFlags()
    {
        _isFed = GameManager.Instance != null && GameManager.Instance.GetFlag(_fedFlag);
        _hasDisappeared = GameManager.Instance != null && GameManager.Instance.GetFlag(_disappearedFlag);
    }

    public void PlayIdle()
    {
        RefreshStateFromFlags();

        if (_hasDisappeared)
        {
            ApplyDisappearedStateImmediate();
            return;
        }

        string targetAnimation = _isFed ? _fedIdleAnimation : _idleAnimation;
        PlayAnimation(targetAnimation, true, false);
    }

    public void PlayFedIdle()
    {
        if (_hasDisappeared) return;

        _isFed = true;
        PlayAnimation(_fedIdleAnimation, true, true);
    }

    public void PlayEatThenFedIdle(Action onComplete = null)
    {
        if (_hasDisappeared)
        {
            onComplete?.Invoke();
            return;
        }

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

    public void PlayDisappear(Action onComplete = null)
    {
        RefreshStateFromFlags();

        if (_hasDisappeared)
        {
            ApplyDisappearedStateImmediate();
            onComplete?.Invoke();
            return;
        }

        _hasDisappeared = true;

        if (GameManager.Instance != null && !string.IsNullOrWhiteSpace(_disappearedFlag))
            GameManager.Instance.SetFlag(_disappearedFlag, true);

        if (_skeletonAnimation == null || _skeletonAnimation.AnimationState == null || string.IsNullOrWhiteSpace(_disappearAnimation))
        {
            CompleteDisappearImmediately(onComplete);
            return;
        }

        DetachDisappearCompleteHandler();
        _pendingDisappearCallback = onComplete;

        _disappearTrack = PlayAnimation(_disappearAnimation, false, true);
        if (_disappearTrack == null)
        {
            CompleteDisappearImmediately(onComplete);
            return;
        }

        _disappearCompleteHandler = OnDisappearComplete;
        _disappearTrack.Complete += _disappearCompleteHandler;
    }

    private void OnEatComplete(TrackEntry entry)
    {
        if (_eatTrack != entry) return;

        DetachEatCompleteHandler();
        PlayFedIdle();

        _pendingEatCallback?.Invoke();
        _pendingEatCallback = null;
    }

    private void OnDisappearComplete(TrackEntry entry)
    {
        if (_disappearTrack != entry) return;

        DetachDisappearCompleteHandler();

        Action callback = _pendingDisappearCallback;
        _pendingDisappearCallback = null;

        DisableCollidersForDisappear();
        callback?.Invoke();
        HideTargetImmediate();
    }

    private void CompleteDisappearImmediately(Action onComplete)
    {
        DisableCollidersForDisappear();
        onComplete?.Invoke();
        HideTargetImmediate();
    }

    private void ApplyDisappearedStateImmediate()
    {
        DisableCollidersForDisappear();
        HideTargetImmediate();
    }

    private void HideTargetImmediate()
    {
        if (_hideTarget != null && _hideTarget.activeSelf)
            _hideTarget.SetActive(false);
    }

    private void DisableCollidersForDisappear()
    {
        if (!_disableCollidersOnDisappear) return;

        if (_collidersToDisable != null && _collidersToDisable.Length > 0)
        {
            for (int i = 0; i < _collidersToDisable.Length; i++)
            {
                if (_collidersToDisable[i] != null)
                    _collidersToDisable[i].enabled = false;
            }

            return;
        }

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = false;
        }
    }

    private void DetachEatCompleteHandler()
    {
        if (_eatTrack != null && _eatCompleteHandler != null)
            _eatTrack.Complete -= _eatCompleteHandler;

        _eatTrack = null;
        _eatCompleteHandler = null;
    }

    private void DetachDisappearCompleteHandler()
    {
        if (_disappearTrack != null && _disappearCompleteHandler != null)
            _disappearTrack.Complete -= _disappearCompleteHandler;

        _disappearTrack = null;
        _disappearCompleteHandler = null;
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