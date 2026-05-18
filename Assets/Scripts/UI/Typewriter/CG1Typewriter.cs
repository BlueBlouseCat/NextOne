using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class CG1Typewriter : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TMP_Text _targetText;
    [SerializeField] private TMP_Text _continueHintText;
    [SerializeField, TextArea(3, 12)] private string _contentOverride;
    [SerializeField] private bool _readTextFromTargetOnStart = true;
    [SerializeField] private float _startDelay = 0.35f;
    [SerializeField] private float _charInterval = 0.05f;
    [SerializeField] private float _segmentGap = 0.6f;
    [SerializeField] private bool _splitByBlankLine = true;
    [SerializeField] private bool _useUnscaledTime = true;

    [Header("Hint")]
    [SerializeField] private string _continueHintTextFormat = "按F键继续";
    [SerializeField] private bool _showHintOnStart = true;

    [Header("Input")]
    [SerializeField] private bool _allowSkipTyping = true;
    [SerializeField] private bool _allowSkipSegmentGap = true;
    [SerializeField] private bool _allowMouseClickSkip = false;
    [SerializeField] private bool _allowSpaceSkip = false;
    [SerializeField] private bool _allowEnterSkip = false;

    [Header("Final")]
    [SerializeField] private bool _waitForFinalConfirm = false;
    [SerializeField] private float _delayBeforeLoadNextScene = 0.3f;

    [Header("Next Scene")]
    [SerializeField] private string _nextSceneName = "House";
    [SerializeField] private bool _useFade = true;
    [SerializeField] private string _nextSpawnPointId = "";

    private Coroutine _playRoutine;
    private string _cachedContent;
    private bool _hasCachedContent;
    private int _lastConsumedAdvanceFrame = -1;

    private void Awake()
    {
        if (_targetText == null)
            _targetText = FindObjectOfType<TMP_Text>();

        CacheContent();
        ClearVisibleText();
        SetHintVisible(_showHintOnStart);
    }

    private void OnEnable()
    {
        Play();
    }

    private void OnDisable()
    {
        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }

        SetHintVisible(false);
    }

    public void Play()
    {
        if (_playRoutine != null)
            StopCoroutine(_playRoutine);

        CacheContent();
        ClearVisibleText();
        SetHintVisible(_showHintOnStart);
        _playRoutine = StartCoroutine(PlayRoutine());
    }

    private void CacheContent()
    {
        if (!string.IsNullOrWhiteSpace(_contentOverride))
        {
            _cachedContent = _contentOverride;
            _hasCachedContent = true;
            return;
        }

        if (_hasCachedContent)
            return;

        if (_readTextFromTargetOnStart && _targetText != null)
        {
            _cachedContent = _targetText.text;
            _hasCachedContent = true;
            return;
        }

        _cachedContent = string.Empty;
        _hasCachedContent = true;
    }

    private void ClearVisibleText()
    {
        if (_targetText == null) return;

        _targetText.text = string.Empty;
        _targetText.maxVisibleCharacters = 0;
    }

    private IEnumerator PlayRoutine()
    {
        if (_targetText == null)
        {
            Debug.LogWarning("CG1Typewriter: Target Text is not assigned.", this);
            _playRoutine = null;
            yield break;
        }

        if (_startDelay > 0f)
            yield return CreateWaitInstruction(_startDelay);

        List<string> segments = BuildSegments(_cachedContent);
        if (segments.Count == 0)
        {
            SetHintVisible(false);
            _playRoutine = null;
            yield break;
        }

        string accumulatedText = string.Empty;

        for (int i = 0; i < segments.Count; i++)
        {
            string nextFullText = string.IsNullOrEmpty(accumulatedText)
                ? segments[i]
                : accumulatedText + "\n\n" + segments[i];

            yield return TypeAppendedText(accumulatedText, nextFullText);
            accumulatedText = nextFullText;

            if (i < segments.Count - 1 && _segmentGap > 0f)
            {
                if (_allowSkipSegmentGap)
                    yield return WaitForDelayOrAdvance(_segmentGap);
                else
                    yield return CreateWaitInstruction(_segmentGap);
            }
        }

        if (_waitForFinalConfirm)
        {
            yield return WaitForAdvance();
        }
        else if (_delayBeforeLoadNextScene > 0f)
        {
            yield return WaitForDelayOrAdvance(_delayBeforeLoadNextScene);
        }

        SetHintVisible(false);
        LoadNextScene();
        _playRoutine = null;
    }

    private List<string> BuildSegments(string content)
    {
        List<string> segments = new List<string>();

        if (string.IsNullOrWhiteSpace(content))
            return segments;

        string normalized = content.Replace("\r\n", "\n").Replace('\r', '\n');

        if (!_splitByBlankLine)
        {
            segments.Add(normalized);
            return segments;
        }

        string[] rawSegments = normalized.Split(new string[] { "\n\n" }, System.StringSplitOptions.None);

        for (int i = 0; i < rawSegments.Length; i++)
        {
            string segment = rawSegments[i].Trim('\n');
            if (!string.IsNullOrWhiteSpace(segment))
                segments.Add(segment);
        }

        if (segments.Count == 0)
            segments.Add(normalized.Trim('\n'));

        return segments;
    }

    private IEnumerator TypeAppendedText(string previousText, string fullText)
    {
        if (fullText == null)
            fullText = string.Empty;

        int previousVisibleCharacters = 0;

        if (!string.IsNullOrEmpty(previousText))
        {
            _targetText.text = previousText;
            _targetText.maxVisibleCharacters = int.MaxValue;
            _targetText.ForceMeshUpdate();
            previousVisibleCharacters = _targetText.textInfo.characterCount;
        }

        _targetText.text = fullText;
        _targetText.ForceMeshUpdate();

        int totalVisibleCharacters = _targetText.textInfo.characterCount;
        _targetText.maxVisibleCharacters = previousVisibleCharacters;

        if (totalVisibleCharacters <= previousVisibleCharacters)
            yield break;

        if (_charInterval <= 0f)
        {
            _targetText.maxVisibleCharacters = totalVisibleCharacters;
            yield break;
        }

        int currentVisibleCharacters = previousVisibleCharacters;
        float timer = 0f;

        while (currentVisibleCharacters < totalVisibleCharacters)
        {
            if (_allowSkipTyping && TryConsumeAdvancePressedThisFrame())
            {
                _targetText.maxVisibleCharacters = totalVisibleCharacters;
                yield break;
            }

            timer += GetDeltaTime();

            while (timer >= _charInterval && currentVisibleCharacters < totalVisibleCharacters)
            {
                timer -= _charInterval;
                currentVisibleCharacters++;
                _targetText.maxVisibleCharacters = currentVisibleCharacters;
            }

            yield return null;
        }

        _targetText.maxVisibleCharacters = totalVisibleCharacters;
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

    private void SetHintVisible(bool visible)
    {
        if (_continueHintText == null) return;

        _continueHintText.gameObject.SetActive(visible);
        _continueHintText.text = visible ? _continueHintTextFormat : string.Empty;
    }

    private void LoadNextScene()
    {
        if (string.IsNullOrWhiteSpace(_nextSceneName))
            return;

        if (GameManager.Instance != null)
        {
            if (_useFade)
            {
                if (string.IsNullOrWhiteSpace(_nextSpawnPointId))
                    GameManager.Instance.LoadSceneWithFade(_nextSceneName);
                else
                    GameManager.Instance.LoadSceneWithFade(_nextSceneName, _nextSpawnPointId);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(_nextSpawnPointId))
                    GameManager.Instance.LoadScene(_nextSceneName);
                else
                    GameManager.Instance.LoadScene(_nextSceneName, _nextSpawnPointId);
            }

            return;
        }

        SceneManager.LoadScene(_nextSceneName);
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