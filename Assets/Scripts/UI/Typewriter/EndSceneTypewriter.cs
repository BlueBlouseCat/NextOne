using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndSceneTypewriter : MonoBehaviour
{
    [System.Serializable]
    public class Page
    {
        [TextArea(3, 8)]
        public string content;

        [Min(0f)]
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

    [Header("Blackout Between Groups")]
    [SerializeField] private bool _blackoutBetweenGroups = true;
    [SerializeField] private float _blackoutDuration = 1f;
    [SerializeField, Range(0f, 1f)] private float _blackoutTargetAlpha = 1f;
    [SerializeField] private bool _clearTextAfterBlackout = true;
    [SerializeField] private float _delayAfterBlackout = 0.3f;

    [Header("Playback")]
    [SerializeField] private bool _playOnStart = true;
    [SerializeField] private float _startDelay = 0.5f;
    [SerializeField] private bool _useUnscaledTime = true;
    [SerializeField] private bool _clearTextOnStart = true;

    [Header("Second Group")]
    [SerializeField] private Page[] _secondGroupPages;

    [Header("Input")]
    [SerializeField] private bool _allowAdvanceByInput = true;
    [SerializeField] private bool _allowSkipTyping = true;
    [SerializeField] private bool _allowSkipHoldDuration = true;
    [SerializeField] private bool _allowSkipBlackout = true;
    [SerializeField] private bool _allowMouseClickSkip = false;
    [SerializeField] private bool _allowSpaceSkip = false;
    [SerializeField] private bool _allowEnterSkip = false;
    [SerializeField] private string _continueHint = "按F键继续";

    [Header("Finish")]
    [SerializeField] private bool _waitForFinalConfirm = true;
    [SerializeField] private string _finalSceneName = "MainStart";
    [SerializeField] private float _delayBeforeFinalSceneLoad = 0f;

    [Header("Final Fade Out")]
    [SerializeField] private bool _fadeOutToBlackBeforeLoad = true;
    [SerializeField] private float _finalFadeOutDuration = 1f;
    [SerializeField] private float _holdBlackBeforeLoad = 0.2f;
    [SerializeField] private bool _allowSkipFinalFade = false;

    private Coroutine _playRoutine;
    private bool _isTyping;
    private int _lastConsumedAdvanceFrame = -1;

    public bool IsPlaying => _playRoutine != null;
    public bool IsTyping => _isTyping;

    private void Awake()
    {
        if (_clearTextOnStart && _targetText != null)
        {
            _targetText.text = string.Empty;
            _targetText.maxVisibleCharacters = 0;
        }

        if (_blackOverlay != null)
            SetOverlayAlpha(_blackOverlay.color.a);

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

    public void Play()
    {
        StopPlayback();

        if (_clearTextOnStart && _targetText != null)
        {
            _targetText.text = string.Empty;
            _targetText.maxVisibleCharacters = 0;
        }

        SetContinueHintVisible(false);
        _playRoutine = StartCoroutine(PlayRoutine());
    }

    public void StopPlayback()
    {
        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }

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
            yield return PlayBlackoutBetweenGroups();

        if (_secondGroupPages != null && _secondGroupPages.Length > 0)
        {
            if (_blackoutBetweenGroups && _delayAfterBlackout > 0f)
            {
                if (_allowSkipBlackout)
                    yield return WaitForDelayOrAdvance(_delayAfterBlackout);
                else
                    yield return CreateWaitInstruction(_delayAfterBlackout);
            }

            yield return PlayPages(_secondGroupPages);
        }

        if (_waitForFinalConfirm)
        {
            SetContinueHintVisible(true);
            yield return WaitForAdvance();
            SetContinueHintVisible(false);
        }

        if (_fadeOutToBlackBeforeLoad)
            yield return PlayFinalFadeOut();

        if (_delayBeforeFinalSceneLoad > 0f)
            yield return CreateWaitInstruction(_delayBeforeFinalSceneLoad);

        LoadFinalScene();
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
                yield return WaitForAdvance();
                SetContinueHintVisible(false);
            }
            else if (page.holdDuration > 0f)
            {
                if (_allowSkipHoldDuration)
                    yield return WaitForDelayOrAdvance(page.holdDuration);
                else
                    yield return CreateWaitInstruction(page.holdDuration);
            }
        }
    }

    private IEnumerator PlaySinglePage(Page page)
    {
        string content = page.content ?? string.Empty;
        float charInterval = Mathf.Max(0f, page.charInterval);

        _targetText.text = content;
        _targetText.maxVisibleCharacters = 0;
        _targetText.ForceMeshUpdate();

        int totalVisibleCharacters = _targetText.textInfo.characterCount;
        _isTyping = true;

        if (totalVisibleCharacters <= 0)
        {
            _isTyping = false;
            yield break;
        }

        if (charInterval <= 0f)
        {
            _targetText.maxVisibleCharacters = totalVisibleCharacters;
            _isTyping = false;
            yield break;
        }

        int currentVisibleCharacters = 0;
        float timer = 0f;

        while (currentVisibleCharacters < totalVisibleCharacters)
        {
            if (_allowSkipTyping && TryConsumeAdvancePressedThisFrame())
            {
                _targetText.maxVisibleCharacters = totalVisibleCharacters;
                _isTyping = false;
                yield break;
            }

            timer += GetDeltaTime();

            while (timer >= charInterval && currentVisibleCharacters < totalVisibleCharacters)
            {
                timer -= charInterval;
                currentVisibleCharacters++;
                _targetText.maxVisibleCharacters = currentVisibleCharacters;
            }

            yield return null;
        }

        _targetText.maxVisibleCharacters = totalVisibleCharacters;
        _isTyping = false;
    }

    private IEnumerator PlayBlackoutBetweenGroups()
    {
        SetContinueHintVisible(false);

        if (_blackOverlay == null)
            yield break;

        float fromAlpha = _blackOverlay.color.a;
        float toAlpha = Mathf.Clamp01(_blackoutTargetAlpha);

        if (_blackoutDuration <= 0f)
        {
            SetOverlayAlpha(toAlpha);
        }
        else
        {
            float timer = 0f;

            while (timer < _blackoutDuration)
            {
                if (_allowSkipBlackout && TryConsumeAdvancePressedThisFrame())
                {
                    SetOverlayAlpha(toAlpha);
                    break;
                }

                timer += GetDeltaTime();
                float t = Mathf.Clamp01(timer / _blackoutDuration);
                SetOverlayAlpha(Mathf.Lerp(fromAlpha, toAlpha, t));
                yield return null;
            }

            SetOverlayAlpha(toAlpha);
        }

        if (_clearTextAfterBlackout && _targetText != null)
        {
            _targetText.text = string.Empty;
            _targetText.maxVisibleCharacters = 0;
        }
    }

    private IEnumerator PlayFinalFadeOut()
    {
        SetContinueHintVisible(false);

        if (_blackOverlay == null)
            yield break;

        float fromAlpha = _blackOverlay.color.a;
        float toAlpha = 1f;

        if (_finalFadeOutDuration <= 0f)
        {
            SetOverlayAlpha(toAlpha);
        }
        else
        {
            float timer = 0f;

            while (timer < _finalFadeOutDuration)
            {
                if (_allowSkipFinalFade && TryConsumeAdvancePressedThisFrame())
                {
                    SetOverlayAlpha(toAlpha);
                    break;
                }

                timer += GetDeltaTime();
                float t = Mathf.Clamp01(timer / _finalFadeOutDuration);
                SetOverlayAlpha(Mathf.Lerp(fromAlpha, toAlpha, t));
                yield return null;
            }

            SetOverlayAlpha(toAlpha);
        }

        if (_holdBlackBeforeLoad > 0f)
            yield return CreateWaitInstruction(_holdBlackBeforeLoad);
    }

    private void SetOverlayAlpha(float alpha)
    {
        if (_blackOverlay == null) return;

        Color color = _blackOverlay.color;
        color.a = Mathf.Clamp01(alpha);
        _blackOverlay.color = color;
    }

    private IEnumerator WaitForAdvance()
    {
        while (!TryConsumeAdvancePressedThisFrame())
            yield return null;
    }

    private IEnumerator WaitForDelayOrAdvance(float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            if (TryConsumeAdvancePressedThisFrame())
                yield break;

            timer += GetDeltaTime();
            yield return null;
        }
    }

    private bool TryConsumeAdvancePressedThisFrame()
    {
        if (!ReadAdvancePressedThisFrame())
            return false;

        if (_lastConsumedAdvanceFrame == Time.frameCount)
            return false;

        _lastConsumedAdvanceFrame = Time.frameCount;
        return true;
    }

    private bool ReadAdvancePressedThisFrame()
    {
        if (!_allowAdvanceByInput && !_allowSkipTyping)
            return false;

        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        bool fPressed =
            keyboard != null &&
            keyboard.fKey.wasPressedThisFrame;

        bool spacePressed =
            _allowSpaceSkip &&
            keyboard != null &&
            keyboard.spaceKey.wasPressedThisFrame;

        bool enterPressed =
            _allowEnterSkip &&
            keyboard != null &&
            (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame);

        bool mousePressed =
            _allowMouseClickSkip &&
            mouse != null &&
            mouse.leftButton.wasPressedThisFrame;

        return fPressed || spacePressed || enterPressed || mousePressed;
    }

    private void SetContinueHintVisible(bool visible)
    {
        if (_continueHintText == null) return;

        _continueHintText.gameObject.SetActive(visible);
        _continueHintText.text = visible ? _continueHint : string.Empty;
    }

    private void LoadFinalScene()
    {
        if (string.IsNullOrWhiteSpace(_finalSceneName))
            return;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadScene(_finalSceneName);
            return;
        }

        SceneManager.LoadScene(_finalSceneName);
    }

    private float GetDeltaTime()
    {
        return _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    private object CreateWaitInstruction(float seconds)
    {
        if (_useUnscaledTime)
            return new WaitForSecondsRealtime(seconds);

        return new WaitForSeconds(seconds);
    }
}