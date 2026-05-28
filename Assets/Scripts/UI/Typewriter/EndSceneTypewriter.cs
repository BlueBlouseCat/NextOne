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

    [Header("Doctor Reveal")]
    [SerializeField] private float _doctorBlackHoldDuration = 1f;
    [SerializeField] private bool _clearTextBeforeDoctorReveal = true;
    [SerializeField] private Page[] _doctorRevealPages;
    [SerializeField] private GameObject _doctorEnvRoot;
    [SerializeField] private string _doctorEnvObjectName = "DoctorEnv";
    [SerializeField] private bool _activateDoctorEnvAfterReveal = true;
    [SerializeField] private bool _clearTextAfterDoctorReveal = true;
    [SerializeField] private float _doctorEnvRevealDuration = 1f;
    [SerializeField] private bool _waitForDoctorEnvInteraction = true;
    [SerializeField] private string _doctorBgName = "BG";
    [SerializeField] private string _doctorUnopenedName = "UNOpen";
    [SerializeField] private string _doctorDetailRootName = "detailLooking";
    [SerializeField] private string _doctorPhotoName = "photo";
    [SerializeField] private string _doctorTextName = "text";
    [SerializeField] private string _doctorTextVariantName = "text (1)";
    [SerializeField] private string[] _doctorOpenedStateNames = { "Opened", "Opened (1)", "Opened (2)", "Opened (3)" };

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

    [Header("Final Return")]
    [SerializeField] private bool _waitForReturnInputOnBlack = true;
    [SerializeField] private bool _allowFinalReturnByPointer = true;
    [SerializeField] private bool _allowFinalReturnByEscape = true;

    private Coroutine _playRoutine;
    private bool _isTyping;
    private int _lastConsumedAdvanceFrame = -1;
    private DoctorEnvInteractionStep _doctorEnvInteractionStep = DoctorEnvInteractionStep.Inactive;
    private bool _doctorEnvInteractionActive;
    private bool _doctorEnvInteractionCompleted;
    private Transform _doctorEnvRootTransform;
    private GameObject _doctorEnvBgObject;
    private GameObject _doctorEnvUnopenedObject;
    private GameObject _doctorEnvDetailRootObject;
    private GameObject _doctorEnvPhotoObject;
    private GameObject _doctorEnvTextObject;
    private GameObject _doctorEnvTextVariantObject;
    private GameObject[] _doctorEnvOpenedObjects;

    public bool IsPlaying => _playRoutine != null;
    public bool IsTyping => _isTyping;

    private enum DoctorEnvInteractionStep
    {
        Inactive,
        WaitingToOpen,
        RevealPhoto,
        RevealText,
        RevealTextVariant,
        Completed
    }

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

        if (ShouldPlayDoctorRevealSequence())
            yield return PlayDoctorRevealSequence();

        if (_waitForFinalConfirm)
        {
            SetContinueHintVisible(true);
            yield return WaitForAdvance();
            SetContinueHintVisible(false);
        }

        if (_fadeOutToBlackBeforeLoad)
            yield return PlayFinalFadeOut();

        if (_waitForReturnInputOnBlack)
            yield return WaitForFinalReturnInput();

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
        yield return FadeOverlayTo(Mathf.Clamp01(_blackoutTargetAlpha), _blackoutDuration, _allowSkipBlackout);

        if (_clearTextAfterBlackout && _targetText != null)
        {
            _targetText.text = string.Empty;
            _targetText.maxVisibleCharacters = 0;
        }
    }

    private IEnumerator PlayFinalFadeOut()
    {
        yield return FadeOverlayTo(1f, _finalFadeOutDuration, _allowSkipFinalFade);

        if (_holdBlackBeforeLoad > 0f)
            yield return CreateWaitInstruction(_holdBlackBeforeLoad);
    }

    private IEnumerator PlayDoctorRevealSequence()
    {
        SetContinueHintVisible(false);

        yield return FadeOverlayTo(1f, 0f, false);

        if (_clearTextBeforeDoctorReveal && _targetText != null)
        {
            _targetText.text = string.Empty;
            _targetText.maxVisibleCharacters = 0;
        }

        if (_doctorBlackHoldDuration > 0f)
            yield return CreateWaitInstruction(_doctorBlackHoldDuration);

        if (_doctorRevealPages != null && _doctorRevealPages.Length > 0)
            yield return PlayPages(_doctorRevealPages);

        ActivateDoctorEnv();

        bool doctorEnvInteractionStarted = BeginDoctorEnvInteraction();

        if (_clearTextAfterDoctorReveal && _targetText != null)
        {
            _targetText.text = string.Empty;
            _targetText.maxVisibleCharacters = 0;
        }

        yield return FadeOverlayTo(0f, _doctorEnvRevealDuration, false);

        if (_waitForDoctorEnvInteraction && doctorEnvInteractionStarted)
            yield return WaitForDoctorEnvInteractionCompletion();
    }

    private IEnumerator FadeOverlayTo(float targetAlpha, float duration, bool allowSkip)
    {
        SetContinueHintVisible(false);

        if (_blackOverlay == null)
            yield break;

        float fromAlpha = _blackOverlay.color.a;
        float toAlpha = Mathf.Clamp01(targetAlpha);

        if (duration <= 0f)
        {
            SetOverlayAlpha(toAlpha);
            yield break;
        }

        float timer = 0f;

        while (timer < duration)
        {
            if (allowSkip && TryConsumeAdvancePressedThisFrame())
            {
                SetOverlayAlpha(toAlpha);
                yield break;
            }

            timer += GetDeltaTime();
            float t = Mathf.Clamp01(timer / duration);
            SetOverlayAlpha(Mathf.Lerp(fromAlpha, toAlpha, t));
            yield return null;
        }

        SetOverlayAlpha(toAlpha);
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

    private bool ShouldPlayDoctorRevealSequence()
    {
        return (_doctorRevealPages != null && _doctorRevealPages.Length > 0) || _activateDoctorEnvAfterReveal;
    }

    private void ActivateDoctorEnv()
    {
        if (!_activateDoctorEnvAfterReveal)
            return;

        GameObject doctorEnv = ResolveDoctorEnvRoot();
        if (doctorEnv == null)
            return;

        if (!doctorEnv.activeSelf)
            doctorEnv.SetActive(true);
    }

    private GameObject ResolveDoctorEnvRoot()
    {
        if (_doctorEnvRoot != null)
            return _doctorEnvRoot;

        if (string.IsNullOrWhiteSpace(_doctorEnvObjectName))
            return null;

        Scene activeScene = SceneManager.GetActiveScene();
        GameObject[] candidates = Resources.FindObjectsOfTypeAll<GameObject>();

        for (int i = 0; i < candidates.Length; i++)
        {
            GameObject candidate = candidates[i];
            if (candidate == null || candidate.name != _doctorEnvObjectName)
                continue;

            if (!candidate.scene.IsValid() || candidate.scene != activeScene)
                continue;

            _doctorEnvRoot = candidate;
            return _doctorEnvRoot;
        }

        return null;
    }

    private bool BeginDoctorEnvInteraction()
    {
        _doctorEnvInteractionActive = false;
        _doctorEnvInteractionCompleted = false;
        _doctorEnvInteractionStep = DoctorEnvInteractionStep.Inactive;

        _doctorEnvRootTransform = ResolveDoctorEnvRootTransform();
        if (_doctorEnvRootTransform == null)
            return false;

        _doctorEnvBgObject = ResolveDirectChildObject(_doctorEnvRootTransform, _doctorBgName);
        _doctorEnvUnopenedObject = ResolveDirectChildObject(_doctorEnvRootTransform, _doctorUnopenedName);
        _doctorEnvOpenedObjects = ResolveDirectChildObjects(_doctorEnvRootTransform, _doctorOpenedStateNames);
        _doctorEnvDetailRootObject = ResolveDirectChildObject(_doctorEnvRootTransform, _doctorDetailRootName);

        Transform detailRootTransform = _doctorEnvDetailRootObject != null
            ? _doctorEnvDetailRootObject.transform
            : null;

        _doctorEnvPhotoObject = ResolveDirectChildObject(detailRootTransform, _doctorPhotoName);
        _doctorEnvTextObject = ResolveDirectChildObject(detailRootTransform, _doctorTextName);
        _doctorEnvTextVariantObject = ResolveDirectChildObject(detailRootTransform, _doctorTextVariantName);

        if (_doctorEnvBgObject == null && _doctorEnvUnopenedObject == null && (_doctorEnvOpenedObjects == null || _doctorEnvOpenedObjects.Length == 0))
            return false;

        SetActiveIfPresent(_doctorEnvBgObject, true);
        SetActiveIfPresent(_doctorEnvUnopenedObject, true);
        SetActiveIfPresent(_doctorEnvDetailRootObject, false);
        SetActiveIfPresent(_doctorEnvPhotoObject, false);
        SetActiveIfPresent(_doctorEnvTextObject, false);
        SetActiveIfPresent(_doctorEnvTextVariantObject, false);

        if (_doctorEnvOpenedObjects != null)
        {
            for (int i = 0; i < _doctorEnvOpenedObjects.Length; i++)
                SetActiveIfPresent(_doctorEnvOpenedObjects[i], false);
        }

        _doctorEnvInteractionStep = DoctorEnvInteractionStep.WaitingToOpen;
        _doctorEnvInteractionActive = true;
        return true;
    }

    private IEnumerator WaitForDoctorEnvInteractionCompletion()
    {
        while (_doctorEnvInteractionActive && !_doctorEnvInteractionCompleted)
        {
            if (ReadDoctorEnvPointerPressedThisFrame())
                AdvanceDoctorEnvInteraction();

            yield return null;
        }
    }

    private void AdvanceDoctorEnvInteraction()
    {
        switch (_doctorEnvInteractionStep)
        {
            case DoctorEnvInteractionStep.WaitingToOpen:
                SetActiveIfPresent(_doctorEnvUnopenedObject, false);
                if (_doctorEnvOpenedObjects != null)
                {
                    for (int i = 0; i < _doctorEnvOpenedObjects.Length; i++)
                        SetActiveIfPresent(_doctorEnvOpenedObjects[i], true);
                }

                _doctorEnvInteractionStep = DoctorEnvInteractionStep.RevealPhoto;
                break;

            case DoctorEnvInteractionStep.RevealPhoto:
                SetActiveIfPresent(_doctorEnvDetailRootObject, true);
                SetActiveIfPresent(_doctorEnvPhotoObject, true);
                _doctorEnvInteractionStep = DoctorEnvInteractionStep.RevealText;
                break;

            case DoctorEnvInteractionStep.RevealText:
                SetActiveIfPresent(_doctorEnvTextObject, true);
                _doctorEnvInteractionStep = DoctorEnvInteractionStep.RevealTextVariant;
                break;

            case DoctorEnvInteractionStep.RevealTextVariant:
                SetActiveIfPresent(_doctorEnvTextVariantObject, true);
                _doctorEnvInteractionStep = DoctorEnvInteractionStep.Completed;
                _doctorEnvInteractionCompleted = true;
                _doctorEnvInteractionActive = false;
                break;
        }
    }

    private Transform ResolveDoctorEnvRootTransform()
    {
        if (_doctorEnvRoot != null)
            return _doctorEnvRoot.transform;

        GameObject root = ResolveDoctorEnvRoot();
        return root != null ? root.transform : null;
    }

    private static GameObject ResolveDirectChildObject(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrWhiteSpace(childName))
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child != null && child.name == childName)
                return child.gameObject;
        }

        return null;
    }

    private static GameObject[] ResolveDirectChildObjects(Transform parent, string[] childNames)
    {
        if (childNames == null || childNames.Length == 0)
            return System.Array.Empty<GameObject>();

        GameObject[] results = new GameObject[childNames.Length];
        for (int i = 0; i < childNames.Length; i++)
            results[i] = ResolveDirectChildObject(parent, childNames[i]);

        return results;
    }

    private static void SetActiveIfPresent(GameObject target, bool active)
    {
        if (target == null) return;

        if (target.activeSelf != active)
            target.SetActive(active);
    }

    private static bool ReadDoctorEnvPointerPressedThisFrame()
    {
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            return true;

        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen != null && touchscreen.primaryTouch.press.wasPressedThisFrame)
            return true;

        return false;
    }

    private IEnumerator WaitForFinalReturnInput()
    {
        SetContinueHintVisible(false);

        while (!ReadFinalReturnPressedThisFrame())
            yield return null;
    }

    private bool ReadFinalReturnPressedThisFrame()
    {
        bool pointerPressed = _allowFinalReturnByPointer && ReadDoctorEnvPointerPressedThisFrame();

        bool escapePressed =
            _allowFinalReturnByEscape &&
            Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame;

        return pointerPressed || escapePressed;
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
