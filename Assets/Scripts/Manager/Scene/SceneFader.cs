using System.Collections;
using UnityEngine;

public class SceneFader : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _fadeDuration = 0.35f;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
    }

    public IEnumerator FadeOut()
    {
        _canvasGroup.blocksRaycasts = true;
        yield return Fade(0f, 1f);
    }

    public IEnumerator FadeIn()
    {
        yield return Fade(1f, 0f);
        _canvasGroup.blocksRaycasts = false;
    }

    private IEnumerator Fade(float from, float to)
    {
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
