using System.Collections;
using UnityEngine;

public class PipeBubble : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private PlayerMovement _playerMovement;

    [Header("Bubbles")]
    [SerializeField] private GameObject _bubble1;
    [SerializeField] private GameObject _bubble2;

    [Header("Timing")]
    [SerializeField] private float _firstBubbleDuration = 1.5f;

    [Header("Optional")]
    [SerializeField] private bool _playOnlyOnce = true;
    [SerializeField] private string _playedFlag = "outside_pipe_bubble_played";

    private Coroutine _sequenceCoroutine;
    private bool _wasClimbingLastFrame;
    private bool _hasPlayedThisSession;

    private void Awake()
    {
        HideAllBubbles();
    }

    private void OnEnable()
    {
        HideAllBubbles();
        _wasClimbingLastFrame = false;
        TryResolvePlayer();
        SyncPlayedStateFromSave();
    }

    private void OnDisable()
    {
        StopSequence();
        HideAllBubbles();
        _wasClimbingLastFrame = false;
    }

    private void Update()
    {
        TryResolvePlayer();

        bool isClimbing = _playerMovement != null && _playerMovement.IsClimbing;

        if (!_wasClimbingLastFrame && isClimbing)
        {
            StartSequence();
        }
        else if (_wasClimbingLastFrame && !isClimbing)
        {
            ResetSequence();
        }

        _wasClimbingLastFrame = isClimbing;
    }

    private void TryResolvePlayer()
    {
        if (_playerMovement != null) return;
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentPlayer == null) return;

        _playerMovement = GameManager.Instance.CurrentPlayer.GetComponent<PlayerMovement>();
    }

    private void StartSequence()
    {
        if (HasAlreadyPlayed())
            return;

        MarkPlayed();
        StopSequence();
        HideAllBubbles();
        _sequenceCoroutine = StartCoroutine(PlayBubbleSequence());
    }

    private IEnumerator PlayBubbleSequence()
    {
        SetBubble1(true);
        SetBubble2(false);

        yield return new WaitForSeconds(_firstBubbleDuration);

        if (_playerMovement == null || !_playerMovement.IsClimbing)
        {
            HideAllBubbles();
            _sequenceCoroutine = null;
            yield break;
        }

        SetBubble1(false);
        SetBubble2(true);

        _sequenceCoroutine = null;
    }

    private void ResetSequence()
    {
        StopSequence();
        HideAllBubbles();
    }

    private void StopSequence()
    {
        if (_sequenceCoroutine == null) return;

        StopCoroutine(_sequenceCoroutine);
        _sequenceCoroutine = null;
    }

    private void HideAllBubbles()
    {
        SetBubble1(false);
        SetBubble2(false);
    }

    private void SyncPlayedStateFromSave()
    {
        if (!_playOnlyOnce) return;
        if (GameManager.Instance == null) return;
        if (string.IsNullOrWhiteSpace(_playedFlag)) return;

        _hasPlayedThisSession = GameManager.Instance.GetFlag(_playedFlag);
    }

    private bool HasAlreadyPlayed()
    {
        if (!_playOnlyOnce)
            return false;

        if (_hasPlayedThisSession)
            return true;

        if (GameManager.Instance == null || string.IsNullOrWhiteSpace(_playedFlag))
            return false;

        bool played = GameManager.Instance.GetFlag(_playedFlag);
        if (played)
            _hasPlayedThisSession = true;

        return played;
    }

    private void MarkPlayed()
    {
        if (!_playOnlyOnce)
            return;

        _hasPlayedThisSession = true;

        if (GameManager.Instance == null || string.IsNullOrWhiteSpace(_playedFlag))
            return;

        GameManager.Instance.SetFlag(_playedFlag, true);
    }

    private void SetBubble1(bool visible)
    {
        if (_bubble1 != null)
            _bubble1.SetActive(visible);
    }

    private void SetBubble2(bool visible)
    {
        if (_bubble2 != null)
            _bubble2.SetActive(visible);
    }
}
