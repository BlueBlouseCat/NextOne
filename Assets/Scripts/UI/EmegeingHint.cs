using DG.Tweening;
using UnityEngine;

public class EmegeingHint : MonoBehaviour
{
    [SerializeField] private CanvasGroup _childCanvasGroup;
    [SerializeField] const float _delay = 1.5f;
    [SerializeField] private float _fadeDuration = 0.5f;
    [SerializeField] private Collider2D _collider2D;
    private Tween _fadeTween;

    private void Awake()
    {
        _collider2D = GetComponent<Collider2D>();
        _collider2D.isTrigger = true;
        if (_childCanvasGroup == null)
            _childCanvasGroup = GetComponentInChildren<CanvasGroup>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_fadeTween != null)
            return;

        if (_childCanvasGroup == null)
        {
            Destroy(gameObject);
            return;
        }

        _fadeTween = _childCanvasGroup
            .DOFade(0f, _fadeDuration)
            .SetDelay(_delay)
            .OnComplete(() => Destroy(gameObject));
    }

    private void OnDestroy()
    {
        if (_fadeTween != null)
        {
            _fadeTween.Kill();
            _fadeTween = null;
        }
    }
}
