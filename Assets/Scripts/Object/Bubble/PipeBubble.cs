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

    private Coroutine _sequenceCoroutine;
    private bool _wasClimbingLastFrame;

    private void Awake()
    {
        HideAllBubbles();
    }

    private void OnEnable()
    {
        HideAllBubbles();
        _wasClimbingLastFrame = false;
        TryResolvePlayer();
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
