using Spine.Unity;
using UnityEngine;

public class MouseIdleController : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string _currentScene = "MouseHole7";

    [Header("Spine")]
    [SerializeField] private SkeletonAnimation _skeletonAnimation;

    [Header("Animation")]
    [SpineAnimation, SerializeField] private string _idleAnimation = "idle";

    private string _currentAnimation;

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
        if (_skeletonAnimation == null || _skeletonAnimation.AnimationState == null) return;
        if (string.IsNullOrWhiteSpace(_idleAnimation)) return;

        if (_currentAnimation == _idleAnimation)
            return;

        _currentAnimation = _idleAnimation;
        _skeletonAnimation.AnimationState.SetAnimation(0, _idleAnimation, true);
    }
}
