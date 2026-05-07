using System;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainUiButtons : MonoBehaviour
{
    [Header("Button Animation")]
    [SerializeField] private float _animStrength = -0.2f;
    [SerializeField] private float _animDuration = 0.2f;

    [Header("Button Refs")]
    [SerializeField] private Transform _newGameBtn;
    [SerializeField] private Transform _loadGameBtn;
    [SerializeField] private Transform _settingsBtn;
    [SerializeField] private Transform _exitBtn;

    public void NewGame()
    {
        AnimateButton(_newGameBtn,onFinish: () =>
        {
            SceneManager.LoadScene("Scene0");
        });
        // SceneManager.LoadScene(SceneName.Scene0);
    }

    public void LoadGame()
    {
        AnimateButton(_loadGameBtn);
    }

    public void OpenSettings()
    {
        AnimateButton(_settingsBtn);
    }

    public void ExitGame()
    {
        AnimateButton(_exitBtn);
        Application.Quit();
    }

    private void AnimateButton(Transform btn,Action onFinish = null)
    {
        if (btn == null) return;
        btn.DOPunchScale(new Vector3(_animStrength, _animStrength, 0f), _animDuration, 2, 0.5f) .
            OnComplete(() => onFinish?.Invoke());      
          
    }
}
