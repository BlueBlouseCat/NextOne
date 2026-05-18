using System.Collections;
using TMPro;
using UnityEngine;

public class SceneFader : MonoBehaviour
{
    [Header("Fade")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _fadeDuration = 0.8f;

    [Header("Message")]
    [SerializeField] private GameObject _messageRoot;
    [SerializeField] private TMP_Text _messageText;
    [SerializeField] private CanvasGroup _messageCanvasGroup;
    [SerializeField] private float _messageHoldBeforeLoad = 0.8f;
    [SerializeField] private float _messageHoldAfterLoad = 0.5f;

    private string _currentMessage = string.Empty;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
        }

        SetMessageImmediate(string.Empty);
    }

    public IEnumerator FadeOut()
    {
        yield return FadeOut(string.Empty);
    }

    public IEnumerator FadeOut(string message)
    {
        SetMessageImmediate(message);

        if (_canvasGroup != null)
            _canvasGroup.blocksRaycasts = true;

        yield return Fade(0f, 1f);
    }

    public IEnumerator FadeIn()
    {
        yield return FadeIn(true);
    }

    public IEnumerator FadeIn(bool clearMessageAfterFade)
    {
        yield return Fade(1f, 0f);

        if (_canvasGroup != null)
            _canvasGroup.blocksRaycasts = false;

        if (clearMessageAfterFade)
            SetMessageImmediate(string.Empty);
    }

    public IEnumerator HoldBeforeSceneLoad()
    {
        if (_messageHoldBeforeLoad > 0f)
            yield return new WaitForSeconds(_messageHoldBeforeLoad);
    }

    public IEnumerator HoldAfterSceneLoad()
    {
        if (_messageHoldAfterLoad > 0f)
            yield return new WaitForSeconds(_messageHoldAfterLoad);
    }

    public void SetMessageImmediate(string message)
    {
        _currentMessage = message ?? string.Empty;

        bool hasMessage =
            !string.IsNullOrWhiteSpace(_currentMessage) &&
            _messageRoot != null &&
            _messageText != null;

        if (_messageText != null)
            _messageText.text = _currentMessage;

        if (_messageRoot != null)
            _messageRoot.SetActive(hasMessage);

        if (_messageCanvasGroup != null)
            _messageCanvasGroup.alpha = hasMessage ? 1f : 0f;
    }

    private IEnumerator Fade(float from, float to)
    {
        if (_canvasGroup == null)
            yield break;

        float time = 0f;
        _canvasGroup.alpha = from;

        while (time < _fadeDuration)
        {
            time += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(from, to, time / _fadeDuration);
            yield return null;
        }

        _canvasGroup.alpha = to;
    }
}
