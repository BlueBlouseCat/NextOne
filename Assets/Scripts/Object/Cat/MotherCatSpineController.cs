using Spine;
using Spine.Unity;
using UnityEngine;

public class MotherCatSpineController : MonoBehaviour
{
    [Header("Spine")]
    [SerializeField] private SkeletonAnimation _skeletonAnimation;

    [Header("Animations")]
    [SpineAnimation, SerializeField] private string _idleAnimation = "idle";
    [SpineAnimation, SerializeField] private string _moveTailAnimation = "move tail";
    [SpineAnimation, SerializeField] private string _tailMovedIdleAnimation = "move tail-idel";
    [SpineAnimation, SerializeField] private string _talkAnimation = "talk";

    [Header("Tail Blocker")]
    [SerializeField] private Collider2D _tailBlocker;
    [SerializeField] private bool _disableBlockerOnMoveStart = true;

    [Header("Optional Spine Event")]
    [SerializeField] private string _tailOpenedEventName = "tail_open";

    private string _currentAnimation;
    private bool _tailOpened;
    private bool _isOpeningTail;

    private void Awake()
    {
        if (_skeletonAnimation == null)
            _skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();
    }

    private void OnEnable()
    {
        if (_skeletonAnimation != null && _skeletonAnimation.AnimationState != null)
            _skeletonAnimation.AnimationState.Event += OnSpineEvent;
    }

    private void OnDisable()
    {
        if (_skeletonAnimation != null && _skeletonAnimation.AnimationState != null)
            _skeletonAnimation.AnimationState.Event -= OnSpineEvent;
    }

    private void Start()
    {
        PlayBlockedIdle();
    }

    /// <summary>
    /// 尾巴状态
    /// </summary>
    public void PlayBlockedIdle()
    {
        _tailOpened = false;
        _isOpeningTail = false;

        SetTailBlocker(true);
        PlayAnimation(_idleAnimation, true, true);
    }

    public void PlayClosedIdle()
    {
        if (_tailOpened)
        {
            PlayAnimation(_tailMovedIdleAnimation, true, true);
            return;
        }

        PlayAnimation(_idleAnimation, true, true);
    }

    public void PlayTalk()
    {
        PlayAnimation(_talkAnimation, true, true);
    }

    public void PlayOpenIdle()
    {
        if (!_tailOpened)
        {
            OpenTail();
            return;
        }

        PlayAnimation(_tailMovedIdleAnimation, true, true);
    }

    public void OpenTail()
    {
        if (_tailOpened)
        {
            PlayAnimation(_tailMovedIdleAnimation, true, true);
            return;
        }

        if (_isOpeningTail) return;

        _isOpeningTail = true;

        if (_disableBlockerOnMoveStart)
            SetTailBlocker(false);

        TrackEntry entry = PlayAnimation(_moveTailAnimation, false, true);
        if (entry != null)
            entry.Complete += OnMoveTailComplete;
    }

    private void OnMoveTailComplete(TrackEntry entry)
    {
        entry.Complete -= OnMoveTailComplete;

        _isOpeningTail = false;
        _tailOpened = true;

        SetTailBlocker(false);
        PlayAnimation(_tailMovedIdleAnimation, true, true);
    }

    private void OnSpineEvent(TrackEntry entry, Spine.Event e)
    {
        if (e == null || e.Data == null) return;
        if (string.IsNullOrEmpty(_tailOpenedEventName)) return;

        if (e.Data.Name == _tailOpenedEventName)
            SetTailBlocker(false);
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

    private void SetTailBlocker(bool enabled)
    {
        if (_tailBlocker != null)
            _tailBlocker.enabled = enabled;
    }
}
