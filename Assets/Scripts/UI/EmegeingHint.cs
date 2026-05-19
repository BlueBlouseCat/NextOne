using System;
using DG.Tweening;
using UnityEngine;

public class EmegeingHint : MonoBehaviour
{
    [SerializeField] private string _hintId;
    [SerializeField] private CanvasGroup _childCanvasGroup;
    [SerializeField] private float _delay = 1.5f;
    [SerializeField] private float _fadeDuration = 0.5f;

    private Tween _fadeTween;

    private void Awake()
    {
        if (_childCanvasGroup == null)
            _childCanvasGroup = GetComponentInChildren<CanvasGroup>();

        if (WasAlreadyShown())
        {
            Destroy(gameObject);
            return;
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
    }


    private void OnEnable()
    {
        if (_childCanvasGroup == null)
            _childCanvasGroup = GetComponentInChildren<CanvasGroup>();

        if (WasAlreadyShown())
        {
            Destroy(gameObject);
            return;
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
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

        MarkAsShown();

        _fadeTween = _childCanvasGroup
            .DOFade(0f, _fadeDuration)
            .SetDelay(_delay)
            .OnComplete(() =>
            {
                GameManager.Instance.SetFlag(_hintId, true);
                Destroy(gameObject);
            });
    }

    private bool WasAlreadyShown()
    {
        if (string.IsNullOrEmpty(_hintId))
            return false;
        if (GameManager.Instance == null)
            return false;
        return GameManager.Instance.HasFlag(_hintId);
    }

    private void MarkAsShown()
    {
        if (string.IsNullOrEmpty(_hintId))
            return;
        if (GameManager.Instance != null)
            GameManager.Instance.SetFlag(_hintId, true);
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
