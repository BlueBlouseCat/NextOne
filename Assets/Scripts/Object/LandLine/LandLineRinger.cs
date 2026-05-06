using System;
using System.Collections.Generic;
using Spine;
using Spine.Unity;
using UnityEngine;

public class LandLineRinger : MonoBehaviour
{
    private static readonly List<LandLineRinger> _instances = new List<LandLineRinger>();

    [Header("Spine")]
    [SerializeField] private SkeletonAnimation _skeletonAnimation;
    [SpineAnimation, SerializeField] private string _ringAnimation = "ring";

    [Header("Optional")]
    [SerializeField] private bool _restartIfAlreadyPlaying = true;
    [SerializeField] private bool _returnToSetupPoseWhenFinished = true;

    private string _currentAnimation;
    private TrackEntry _ringTrack;
    private bool _isRinging;

    public event Action<LandLineRinger> RingCompleted;

    private void Awake()
    {
        if (_skeletonAnimation == null)
            _skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();
    }

    private void OnEnable()
    {
        if (!_instances.Contains(this))
            _instances.Add(this);
    }

    private void OnDisable()
    {
        _instances.Remove(this);
        ClearRingTrackCallback();
        RestoreSetupPose();
    }

    public static List<LandLineRinger> TriggerAllRings()
    {
        List<LandLineRinger> started = new List<LandLineRinger>();

        for (int i = 0; i < _instances.Count; i++)
        {
            if (_instances[i] == null) continue;

            if (_instances[i].PlayRing())
                started.Add(_instances[i]);
        }

        return started;
    }

    public bool PlayRing()
    {
        if (_skeletonAnimation == null || _skeletonAnimation.AnimationState == null) return false;
        if (string.IsNullOrWhiteSpace(_ringAnimation)) return false;

        if (_isRinging && !_restartIfAlreadyPlaying)
            return false;

        ClearRingTrackCallback();

        _ringTrack = PlayAnimation(_ringAnimation, false, true);
        _isRinging = _ringTrack != null;

        if (_ringTrack != null)
            _ringTrack.Complete += OnRingComplete;

        return _ringTrack != null;
    }

    public void StopRing()
    {
        ClearRingTrackCallback();
        RestoreSetupPose();
    }

    private void OnRingComplete(TrackEntry entry)
    {
        if (_ringTrack != entry) return;

        ClearRingTrackCallback();
        RestoreSetupPose();
        RingCompleted?.Invoke(this);
    }

    private void ClearRingTrackCallback()
    {
        if (_ringTrack != null)
        {
            _ringTrack.Complete -= OnRingComplete;
            _ringTrack = null;
        }

        _isRinging = false;
        _currentAnimation = string.Empty;
    }

    private void RestoreSetupPose()
    {
        if (_skeletonAnimation == null || _skeletonAnimation.AnimationState == null) return;

        _skeletonAnimation.AnimationState.ClearTrack(0);

        if (_returnToSetupPoseWhenFinished)
        {
            _skeletonAnimation.Skeleton.SetToSetupPose();
            _skeletonAnimation.AnimationState.Apply(_skeletonAnimation.Skeleton);
        }
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
