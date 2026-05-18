using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class EndSceneTypewriter : MonoBehaviour
{
    private const string LegacyContinueHint = "Press F / Space";

    [System.Serializable]
    public class Page
    {
        [TextArea(3, 8)]
        public string content;

        [Min(0.001f)]
        public float charInterval = 0.06f;

        [Min(0f)]
        public float holdDuration = 1.2f;

        public bool waitForInput = true;
    }

    [Header("UI")]
    [SerializeField] private TMP_Text _targetText;
    [SerializeField] private TMP_Text _continueHintText;
    [SerializeField] private Image _blackOverlay;

    [Header("First Group")]
    [SerializeField] private Page[] _firstGroupPages;

    [Header("Blackout")]
    [SerializeField] private bool _blackoutBetweenGroups = true;
    [SerializeField] private float _blackoutDuration = 1f;
    [SerializeField, Range(0f, 1f)] private float _blackoutTargetAlpha = 1f;
    [SerializeField] private bool _clearTextAfterBlackout = true;
    [SerializeField] private float _delayAfterBlackout = 0.3f;

    [Header("Second Group")]
    [SerializeField] private Page[] _secondGroupPages;

    [Header("Playback")]
    [SerializeField] private bool _playOnStart = true;
    [SerializeField] private float _startDelay = 0.5f;
    [SerializeField] private bool _useUnscaledTime = true;
    [SerializeField] private bool _clearTextOnStart = true;

    [Header("Input")]
    [SerializeField] private bool _allowSkipTyping = true;
    [SerializeField] private bool _allowAdvanceByInput = true;
    [SerializeField] private string _continueHint = "Press F / Space";

    private Coroutine _playRoutine;
    private Tween _typingTween;
    private Tween _blackoutTween;

    private bool _isTyping;
    private bool _advancePressedThisFrame;

    public bool IsPlaying => _playRoutine != null;
    public bool IsTyping => _isTyping;

    private void Awake()
    {
        if (_clearTextOnStart && _targetText != null)
            _targetText.text = string.Empty;

        SetContinueHintVisible(false);
    }

    private void OnEnable()
    {
        if (_playOnStart)
            Play();
    }

    private void OnDisable()
    {
        StopPlayback();
    }

    private void Update()
    {
        _advancePressedThisFrame = ReadAdvancePressedThisFrame();
    }

    public void Play()
    {
        StopPlayback();
        _playRoutine = StartCoroutine(PlayRoutine());
    }

    public void StopPlayback()
    {
        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }

        KillTypingTween();
        KillBlackoutTween();

        _isTyping = false;
        SetContinueHintVisible(false);
    }

    private IEnumerator PlayRoutine()
    {
        if (_targetText == null)
        {
            Debug.LogWarning("EndSceneTypewriter: Target Text is not assigned.", this);
            _playRoutine = null;
            yield break;
        }

        if (_startDelay > 0f)
            yield return CreateWaitInstruction(_startDelay);

        if (_firstGroupPages != null && _firstGroupPages.Length > 0)
            yield return PlayPages(_firstGroupPages);

        if (_blackoutBetweenGroups && _secondGroupPages != null && _secondGroupPages.Length > 0)
            yield return PlayBlackout();

        if (_secondGroupPages != null && _secondGroupPages.Length > 0)
        {
            if (_blackoutBetweenGroups && _delayAfterBlackout > 0f)
                yield return CreateWaitInstruction(_delayAfterBlackout);

            yield return PlayPages(_secondGroupPages);
        }

        _playRoutine = null;
    }

    private IEnumerator PlayPages(Page[] pages)
    {
        for (int i = 0; i < pages.Length; i++)
        {
            Page page = pages[i];
            if (page == null) continue;

            yield return PlaySinglePage(page);

            if (page.waitForInput && _allowAdvanceByInput)
            {
                SetContinueHintVisible(true);
                yield return new WaitUntil(() => _advancePressedThisFrame);
                SetContinueHintVisible(false);
            }
            else if (page.holdDuration > 0f)
            {
                yield return CreateWaitInstruction(page.holdDuration);
            }
        }
    }

    private IEnumerator PlaySinglePage(Page page)
    {
        string content = page.content ?? string.Empty;
        float charInterval = Mathf.Max(0.001f, page.charInterval);

        _targetText.text = string.Empty;
        _isTyping = true;

        if (content.Length == 0)
        {
            _isTyping = false;
            yield break;
        }

        float duration = content.Length * charInterval;

        _typingTween = DOVirtual.Int(0, content.Length, duration, value =>
        {
            if (_targetText == null) return;

            int visibleCount = Mathf.Clamp(value, 0, content.Length);
            _targetText.text = content.Substring(0, visibleCount);
        })
        .SetEase(Ease.Linear)
        .SetUpdate(_useUnscaledTime)
        .OnComplete(() =>
        {
            if (_targetText != null)
                _targetText.text = content;
        });

        while (_typingTween != null && _typingTween.IsActive() && _typingTween.IsPlaying())
        {
            if (_allowSkipTyping && _advancePressedThisFrame)
            {
                _typingTween.Complete();
                break;
            }

            yield return null;
        }

        KillTypingTween();

        if (_targetText != null)
            _targetText.text = content;

        _isTyping = false;
    }

    private IEnumerator PlayBlackout()
    {
        SetContinueHintVisible(false);

        if (_blackOverlay == null)
            yield break;

        KillBlackoutTween();

        float currentAlpha = _blackOverlay.color.a;
        float targetAlpha = Mathf.Clamp01(_blackoutTargetAlpha);
        bool completed = false;

        _blackoutTween = DOTween.To(
            () => currentAlpha,
            value =>
            {
                currentAlpha = value;
                Color color = _blackOverlay.color;
                color.a = value;
                _blackOverlay.color = color;
            },
            targetAlpha,
            _blackoutDuration
        )
        .SetEase(Ease.Linear)
        .SetUpdate(_useUnscaledTime)
        .OnComplete(() => completed = true);

        while (!completed)
            yield return null;

        KillBlackoutTween();

        if (_clearTextAfterBlackout && _targetText != null)
            _targetText.text = string.Empty;
    }

    private void KillTypingTween()
    {
        if (_typingTween == null) return;

        if (_typingTween.IsActive())
            _typingTween.Kill();

        _typingTween = null;
    }

    private void KillBlackoutTween()
    {
        if (_blackoutTween == null) return;

        if (_blackoutTween.IsActive())
            _blackoutTween.Kill();

        _blackoutTween = null;
    }

    private void SetContinueHintVisible(bool visible)
    {
        if (_continueHintText == null) return;

        _continueHintText.gameObject.SetActive(visible);
        _continueHintText.text = visible ? GetResolvedContinueHint() : string.Empty;
    }

    private bool ReadAdvancePressedThisFrame()
    {
        if (!_allowAdvanceByInput && !_allowSkipTyping)
            return false;

        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        bool keyboardPressed =
            GameplayInputUtil.InteractPressedThisFrame() ||
            GameplayInputUtil.SubmitPressedThisFrame() ||
            (keyboard != null && keyboard.spaceKey.wasPressedThisFrame);

        bool mousePressed =
            mouse != null &&
            mouse.leftButton.wasPressedThisFrame;

        return keyboardPressed || mousePressed;
    }

    private string GetResolvedContinueHint()
    {
        if (string.IsNullOrWhiteSpace(_continueHint) || _continueHint == LegacyContinueHint)
            return $"Press {GameplayInputUtil.GetInteractDisplayName()} / Space";

        return _continueHint;
    }

    private object CreateWaitInstruction(float seconds)
    {
        if (_useUnscaledTime)
            return new WaitForSecondsRealtime(seconds);

        return new WaitForSeconds(seconds);
    }
}
